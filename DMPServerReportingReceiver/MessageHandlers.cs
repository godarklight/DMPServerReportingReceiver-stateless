using System;
using System.Collections.Generic;
using MessageStream;

namespace DMPServerReportingReceiver
{
    public class MessageHandlers
    {
        private static DatabaseConnection databaseConnection
        {
            get
            {
                return MainClass.databaseConnection;
            }
        }

        public static void HandleHeartbeat(ClientObject client, byte[] messageData)
        {
            //Don't care - these only keep the connection alive
        }

        public static void HandleReportingVersion1(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData, false))
            {
                string serverHash = mr.Read<string>();
                string serverName = mr.Read<string>();
                string description = mr.Read<string>();
                int gamePort = mr.Read<int>();
                string gameAddress = mr.Read<string>();
                int protocolVersion = mr.Read<int>();
                string programVersion = mr.Read<string>();
                int maxPlayers = mr.Read<int>();
                int playerCount = mr.Read<int>();
                int modControl = mr.Read<int>();
                string modControlSha = mr.Read<string>();
                int gameMode = mr.Read<int>();
                bool cheats = mr.Read<bool>();
                int warpMode = mr.Read<int>();
                long universeSize = mr.Read<long>();
                string banner = mr.Read<string>();
                string homepage = mr.Read<string>();
                int httpPort = mr.Read<int>();
                string admin = mr.Read<string>();
                string team = mr.Read<string>();
                string location = mr.Read<string>();
                bool fixedIP = mr.Read<bool>();
                string[] players = mr.Read<string[]>();
                //Check if this is a new server
                client.serverHash = serverHash;
                Dictionary<string, object> hashParameters = new Dictionary<string, object>();
                hashParameters["hash"] = serverHash;
                Console.WriteLine("Init: " + client.initialized);

                //Initialize if needed
                if (!client.initialized)
                {
                    client.initialized = true;
                    string sqlQuery = "CALL gameserverinit('?serverhash', '?namex', '?descriptionx', '?gameportx', '?gameaddressx', '?protocolx', '?programversion', '?maxplayersx', '?playercountx', '?modcontrolx', '?modcontrolshax', '?gamemodex', '?cheatsx', '?warpmodex', '?universex', '?bannerx', '?homepagex', '?httpportx', '?adminx', '?teamx', '?locationx', '?fixedipx');";
                    Console.WriteLine("===");
                    Console.WriteLine(sqlQuery);
                    Console.WriteLine("===");
                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    parameters["serverhash"] = serverHash;
                    parameters["namex"] = serverName;
                    if (serverName.Length > 255)
                    {
                        serverName.Substring(0, 255);
                    }
                    parameters["descriptionx"] = description;
                    parameters["gameportx"] = gamePort;
                    parameters["gameaddressx"] = gameAddress;
                    parameters["protocolx"] = protocolVersion;
                    parameters["programversion"] = programVersion;
                    parameters["maxplayersx"] = maxPlayers;
                    parameters["playercountx"] = playerCount;
                    parameters["modcontrolx"] = modControl;
                    parameters["modcontrolshax"] = modControlSha;
                    parameters["gamemodex"] = gameMode;
                    parameters["cheatsx"] = cheats;
                    parameters["warpmodex"] = warpMode;
                    parameters["universex"] = universeSize;
                    parameters["bannerx"] = banner;
                    parameters["homepagex"] = homepage;
                    parameters["httpportx"] = httpPort;
                    parameters["adminx"] = admin;
                    parameters["teamx"] = team;
                    parameters["locationx"] = location;
                    parameters["fixedipx"] = fixedIP;
                    databaseConnection.ExecuteNonReader(sqlQuery, parameters);
                }

                if (client.connectedPlayers == null)
                {
                    //Report connected players as connected
                    foreach (string connectedPlayer in players)
                    {
                        Dictionary<string, object> playerParams = new Dictionary<string, object>();
                        playerParams["hash"] = serverHash;
                        playerParams["player"] = connectedPlayer;
                        string sqlQuery = "CALL gameserverplayer('?hash' ,'?player', '1')";
                        databaseConnection.ExecuteNonReader(sqlQuery, playerParams);
                    }
                }
                else
                {
                    //Take all the currently connected players and remove the players that were connected already to generate a list of players to be added
                    List<string> addList = new List<string>(players);
                    foreach (string player in client.connectedPlayers)
                    {
                        if (addList.Contains(player))
                        {
                            addList.Remove(player);
                        }
                    }
                    //Take all the old players connected and remove the players that are connected already to generate a list of players to be removed
                    List<string> removeList = new List<string>(client.connectedPlayers);
                    foreach (string player in players)
                    {
                        if (addList.Contains(player))
                        {
                            addList.Remove(player);
                        }
                    }
                    //Add new players
                    foreach (string player in addList)
                    {
                        Dictionary<string, object> playerParams = new Dictionary<string, object>();
                        playerParams["hash"] = serverHash;
                        playerParams["player"] = player;
                        string sqlQuery = "CALL gameserverplayer('?hash' ,'?player', '1')";
                        databaseConnection.ExecuteNonReader(sqlQuery, playerParams);
                    }
                    //Remove old players
                    foreach (string player in removeList)
                    {
                        Dictionary<string, object> playerParams = new Dictionary<string, object>();
                        playerParams["hash"] = serverHash;
                        playerParams["player"] = player;
                        string sqlQuery = "CALL gameserverplayer('?hash' ,'?player', '0')";
                        databaseConnection.ExecuteNonReader(sqlQuery, playerParams);
                    }
                }
                //Save connected players for tracking
                client.connectedPlayers = players;


                Console.WriteLine("Received report from " + serverName + " (" + client.address + "), Protocol " + protocolVersion + ", Program Version: " + programVersion);
            }
        }
    }
}

