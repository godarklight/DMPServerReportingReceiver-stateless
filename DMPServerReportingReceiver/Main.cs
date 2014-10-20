using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DMPServerReportingReceiver
{
    public delegate void ConnectionCallback(ClientObject client,byte[] messageData);
    public enum MessageTypes
    {
        HEARTBEAT,
        REPORTING_VERSION_1,
    }

    public class MainClass
    {
        //Server socket
        private static TcpListener serverListener;
        //State tracking
        private static int connectedClients = 0;
        private static ConcurrentQueue<ClientObject> addClients = new ConcurrentQueue<ClientObject>();
        private static List<ClientObject> clients = new List<ClientObject>();
        private static ConcurrentQueue<ClientObject> deleteClients = new ConcurrentQueue<ClientObject>();
        private static Stopwatch programClock = new Stopwatch();
        //Server disappears after 60 seconds
        private const int CONNECTION_TIMEOUT = 30000;
        //5MB max message size
        private const int MAX_PAYLOAD_SIZE = 5000 * 1024;
        //Message handlers
        private static Dictionary<int, ConnectionCallback> registeredHandlers = new Dictionary<int, ConnectionCallback>();
        public static DatabaseConnection databaseConnection = new DatabaseConnection();

        public static void Main()
        {
            programClock.Start();
            //Connect to database
            databaseConnection.Connect();
            //Register handlers
            registeredHandlers.Add((int)MessageTypes.HEARTBEAT, MessageHandlers.HandleHeartbeat);
            registeredHandlers.Add((int)MessageTypes.REPORTING_VERSION_1, MessageHandlers.HandleReportingVersion1);
            StartServer();
            ExpireAllOnlineServers();
            while (true)
            {
                //Add client
                ClientObject addClient;
                while (addClients.TryDequeue(out addClient))
                {
                    //Treat the clients list as immuteable - Prevents throws while iterating the list.
                    List<ClientObject> newClientsList = new List<ClientObject>(clients);
                    newClientsList.Add(addClient);
                    clients = newClientsList;
                    connectedClients = clients.Count;
                }
                //Delete client
                ClientObject deleteClient;
                while (deleteClients.TryDequeue(out deleteClient))
                {
                    //Treat the clients list as immuteable - Prevents throws while iterating the list.
                    List<ClientObject> newClientsList = new List<ClientObject>(clients);
                    if (newClientsList.Contains(deleteClient))
                    {
                        CallServerOffline(deleteClient.serverHash);
                        newClientsList.Remove(deleteClient);
                        clients = newClientsList;
                        connectedClients = clients.Count;
                        Console.WriteLine("Dropped connection from " + deleteClient.address.ToString() + ", connected: " + connectedClients);
                        try
                        {
                            if (deleteClient.clientConnection.Connected)
                            {
                                deleteClient.clientConnection.GetStream().Close();
                                deleteClient.clientConnection.GetStream().Dispose();
                                deleteClient.clientConnection.Close();
                            }
                        }
                        catch
                        {
                            //Don't care.
                        }
                    }
                }
                CheckTimeouts();
                Thread.Sleep(500);
            }
        }

        private static void ExpireAllOnlineServers()
        {
            object[][] result = databaseConnection.ExecuteReader("SELECT hash FROM server_statusnow");
            foreach (object[] entry in result)
            {
                string hash = (string)entry[0];
                Console.WriteLine("Taking stale server " + (string)entry[0] + " offline!");
                CallServerOffline(hash);
            }
        }

        private static void CallServerOffline(string hash)
        {
            Dictionary<string, object> offlineParams = new Dictionary<string, object>();
            offlineParams["@hash"] = hash;
            string mySql = "CALL gameserveroffline(@hash)";
            databaseConnection.ExecuteNonReader(mySql, offlineParams);
        }

        private static void CheckTimeouts()
        {
            foreach (ClientObject client in clients)
            {
                if (programClock.ElapsedMilliseconds > (client.lastReceiveTime + CONNECTION_TIMEOUT))
                {
                    deleteClients.Enqueue(client);
                }
            }
        }

        private static void StartServer()
        {
            serverListener = new TcpListener(IPAddress.Any, 9001);
            serverListener.Start();
            serverListener.BeginAcceptTcpClient(AcceptCallback, null);
            Console.WriteLine("Listening for connections!");
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            TcpClient clientConnection = serverListener.EndAcceptTcpClient(ar);
            try
            {
                if (clientConnection.Connected)
                {
                    SetupNewClient(clientConnection);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Client failed to connect, error: " + e);
            }
            serverListener.BeginAcceptSocket(AcceptCallback, null);
        }

        private static void SetupNewClient(TcpClient clientConnection)
        {
            //Create a new ClientObject for the reporting client
            ClientObject newClient = new ClientObject();
            newClient.clientConnection = clientConnection;
            newClient.incomingMessage = new NetworkMessage();
            newClient.incomingMessage.data = new byte[8];
            newClient.bytesToReceive = 8;
            newClient.lastReceiveTime = programClock.ElapsedMilliseconds;
            addClients.Enqueue(newClient);
            try
            {
                newClient.address = (IPEndPoint)newClient.clientConnection.Client.RemoteEndPoint;
                Console.WriteLine("New connection from " + newClient.address.ToString() + ", connected: " + (connectedClients + 1));
                newClient.clientConnection.GetStream().BeginRead(newClient.incomingMessage.data, newClient.incomingMessage.data.Length - newClient.bytesToReceive, newClient.bytesToReceive, ReceiveCallback, newClient);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error setting up new client, Exception: " + e);
                deleteClients.Enqueue(newClient);
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            ClientObject client = (ClientObject)ar.AsyncState;
            try
            {
                int bytesReceived = client.clientConnection.GetStream().EndRead(ar);
                client.bytesToReceive -= bytesReceived;
                if (bytesReceived > 0)
                {
                    client.lastReceiveTime = programClock.ElapsedMilliseconds;
                }
                if (client.bytesToReceive == 0)
                {
                    //We have a header or a payload
                    if (!client.isRecevingPayload)
                    {
                        //We have a header
                        client.incomingMessage.type = BitConverter.ToInt32(client.incomingMessage.data, 0);
                        int messagePayload = BitConverter.ToInt32(client.incomingMessage.data, 4);
                        if (messagePayload > MAX_PAYLOAD_SIZE || MAX_PAYLOAD_SIZE < 0)
                        {
                            Console.WriteLine("Invalid TCP message. Disconnecting client.");
                            deleteClients.Enqueue(client);
                            return;
                        }
                        if (messagePayload == 0)
                        {
                            client.incomingMessage.data = null;
                            HandleMessage(client, client.incomingMessage);
                            client.incomingMessage = new NetworkMessage();
                            client.incomingMessage.data = new byte[8];
                            client.bytesToReceive = 8;
                        }
                        else
                        {
                            client.isRecevingPayload = true;
                            client.incomingMessage.data = new byte[messagePayload];
                            client.bytesToReceive = messagePayload;
                        }
                    }
                    else
                    {
                        //We have a payload
                        HandleMessage(client, client.incomingMessage);
                        client.isRecevingPayload = false;
                        client.incomingMessage = new NetworkMessage();
                        client.incomingMessage.data = new byte[8];
                        client.bytesToReceive = 8;
                    }
                }
                client.clientConnection.GetStream().BeginRead(client.incomingMessage.data, client.incomingMessage.data.Length - client.bytesToReceive, client.bytesToReceive, ReceiveCallback, client);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading data, Exception: " + e);
                deleteClients.Enqueue(client);
            }
        }

        private static void HandleMessage(ClientObject client, NetworkMessage receivedMessage)
        {
            if (registeredHandlers.ContainsKey(receivedMessage.type))
            {
                try
                {
                    registeredHandlers[receivedMessage.type](client, receivedMessage.data);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error processing type " + receivedMessage.type + ", Exception :" + e);
                }
            }
        }
    }
}
