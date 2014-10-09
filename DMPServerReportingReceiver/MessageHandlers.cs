using System;
using MessageStream;

namespace DMPServerReportingReceiver
{
    public class MessageHandlers
    {
        public static void HandleHeartbeat(ClientObject client, byte[] messageData)
        {
            //Don't care - these only keep the connection alive
        }

        public static void HandleReportingVersion1(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData, false))
            {
                bool cheats = mr.Read<bool>();
                int gameMode = mr.Read<int>();
                int modControl = mr.Read<int>();
                int warpMode = mr.Read<int>();
                int maxPlayers = mr.Read<int>();
                int playerCount = mr.Read<int>();
                string players = mr.Read<string>();
                long lastPlayerActivity = mr.Read<long>();
                int gamePort = mr.Read<int>();
                int httpPort = mr.Read<int>();
                int protocolVersion = mr.Read<int>();
                string programVersion = mr.Read<string>();
                string serverName = mr.Read<string>();
                long universeSize = mr.Read<long>();
                Console.WriteLine("Received report from " + client.address + ", Protocol " + protocolVersion + ", Program Version: " + programVersion);

            }
        }
    }
}

