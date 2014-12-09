using System;
using System.Collections.Generic;
using System.Diagnostics;
using MessageStream;
using MessageStream2;

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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            using (MessageStream.MessageReader mr = new MessageStream.MessageReader(messageData, false))
            {
                string serverHash = mr.Read<string>();
                string serverName = mr.Read<string>();
                string description = mr.Read<string>();
                int gamePort = mr.Read<int>();
                string gameAddress = mr.Read<string>();
                int protocolVersion = mr.Read<int>();
                string programVersion = mr.Read<string>();
                int maxPlayers = mr.Read<int>();
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
                hashParameters["@hash"] = serverHash;

                //Initialize if needed
                if (!client.initialized)
                {
                    MainClass.DisconnectClientsWithHash(serverHash);
                    client.initialized = true;
                    //string sqlQuery = "CALL gameserverinit(@serverhash, @namex, @descriptionx, @gameportx, @gameaddressx, @protocolx, @programversion, @maxplayersx, @modcontrolx, @modcontrolshax, @gamemodex, @cheatsx, @warpmodex, @universex, @bannerx, @homepagex, @httpportx, @adminx, @teamx, @locationx, @fixedipx);";
                    string sqlQuery = "INSERT INTO " + databaseConnection.settings.database + " (`serverHash`, `serverName`, `description`, `gamePort`, `gameAddress`, `protocolVersion`, `programVersion`, `maxPlayers`, `modControl`, `modControlSha`, `gameMode`, `cheats`, `warpMode`, `universeSize`, `banner`, `homepage`, `httpPort`, `admin`, `team`, `location`, `fixedIP`) VALUES (@serverhash, @namex, @descriptionx, @gameportx, @gameaddressx, @protocolx, @programversion, @maxplayersx, @modcontrolx, @modcontrolshax, @gamemodex, @cheatsx, @warpmodex, @universex, @bannerx, @homepagex, @httpportx, @adminx, @teamx, @locationx, @fixedipx)";
                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    parameters["@serverhash"] = serverHash;
                    parameters["@namex"] = serverName;
                    if (serverName.Length > 255)
                    {
                        serverName = serverName.Substring(0, 255);
                    }
                    parameters["@descriptionx"] = description;
                    parameters["@gameportx"] = gamePort;
                    parameters["@gameaddressx"] = gameAddress;
                    parameters["@protocolx"] = protocolVersion;
                    parameters["@programversion"] = programVersion;
                    parameters["@maxplayersx"] = maxPlayers;
                    parameters["@modcontrolx"] = modControl;
                    parameters["@modcontrolshax"] = modControlSha;
                    parameters["@gamemodex"] = gameMode;
                    parameters["@cheatsx"] = cheats;
                    parameters["@warpmodex"] = warpMode;
                    parameters["@universex"] = universeSize;
                    parameters["@bannerx"] = banner;
                    parameters["@homepagex"] = homepage;
                    parameters["@httpportx"] = httpPort;
                    parameters["@adminx"] = admin;
                    parameters["@teamx"] = team;
                    parameters["@locationx"] = location;
                    parameters["@fixedipx"] = fixedIP;
                    Console.WriteLine("Server " + serverHash + " is online!");
                    databaseConnection.ExecuteNonReader(sqlQuery, parameters);
                }

                foreach (string player in players)
                {
                    Console.WriteLine(serverHash + ": " + player);
                }
                string playerSqlCommand = "UPDATE DMPServerList SET players=@players WHERE `serverHash`=@serverHash";
                Dictionary<string, object> playerParameters = new Dictionary<string, object>();
                playerParameters.Add("@serverHash", serverHash);
                string currentPlayers = String.Join("\n", players) + "\n";
                playerParameters.Add("@players", currentPlayers);
                databaseConnection.ExecuteNonReader(playerSqlCommand, playerParameters);
                sw.Stop();
                Console.WriteLine("Handled report from " + serverName + " (" + client.address + "), Protocol " + protocolVersion + ", Program Version: " + programVersion + ", Time: " + sw.ElapsedMilliseconds);
            }
        }

        //Exactly the same as above, but with MessageReader2
        public static void HandleReportingVersion2(ClientObject client, byte[] messageData)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            using (MessageStream2.MessageReader mr = new MessageStream2.MessageReader(messageData))
            {
                string serverHash = mr.Read<string>();
                string serverName = mr.Read<string>();
                string description = mr.Read<string>();
                int gamePort = mr.Read<int>();
                string gameAddress = mr.Read<string>();
                int protocolVersion = mr.Read<int>();
                string programVersion = mr.Read<string>();
                int maxPlayers = mr.Read<int>();
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
                hashParameters["@hash"] = serverHash;

                //Initialize if needed
                if (!client.initialized)
                {
                    MainClass.DisconnectClientsWithHash(serverHash);
                    client.initialized = true;
                    //string sqlQuery = "CALL gameserverinit(@serverhash, @namex, @descriptionx, @gameportx, @gameaddressx, @protocolx, @programversion, @maxplayersx, @modcontrolx, @modcontrolshax, @gamemodex, @cheatsx, @warpmodex, @universex, @bannerx, @homepagex, @httpportx, @adminx, @teamx, @locationx, @fixedipx);";
                    string sqlQuery = "INSERT INTO " + databaseConnection.settings.database + " (`serverHash`, `serverName`, `description`, `gamePort`, `gameAddress`, `protocolVersion`, `programVersion`, `maxPlayers`, `modControl`, `modControlSha`, `gameMode`, `cheats`, `warpMode`, `universeSize`, `banner`, `homepage`, `httpPort`, `admin`, `team`, `location`, `fixedIP`) VALUES (@serverhash, @namex, @descriptionx, @gameportx, @gameaddressx, @protocolx, @programversion, @maxplayersx, @modcontrolx, @modcontrolshax, @gamemodex, @cheatsx, @warpmodex, @universex, @bannerx, @homepagex, @httpportx, @adminx, @teamx, @locationx, @fixedipx)";
                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    parameters["@serverhash"] = serverHash;
                    parameters["@namex"] = serverName;
                    if (serverName.Length > 255)
                    {
                        serverName = serverName.Substring(0, 255);
                    }
                    parameters["@descriptionx"] = description;
                    parameters["@gameportx"] = gamePort;
                    parameters["@gameaddressx"] = gameAddress;
                    parameters["@protocolx"] = protocolVersion;
                    parameters["@programversion"] = programVersion;
                    parameters["@maxplayersx"] = maxPlayers;
                    parameters["@modcontrolx"] = modControl;
                    parameters["@modcontrolshax"] = modControlSha;
                    parameters["@gamemodex"] = gameMode;
                    parameters["@cheatsx"] = cheats;
                    parameters["@warpmodex"] = warpMode;
                    parameters["@universex"] = universeSize;
                    parameters["@bannerx"] = banner;
                    parameters["@homepagex"] = homepage;
                    parameters["@httpportx"] = httpPort;
                    parameters["@adminx"] = admin;
                    parameters["@teamx"] = team;
                    parameters["@locationx"] = location;
                    parameters["@fixedipx"] = fixedIP;
                    Console.WriteLine("Server " + serverHash + " is online!");
                    databaseConnection.ExecuteNonReader(sqlQuery, parameters);
                }

                foreach (string player in players)
                {
                    Console.WriteLine(serverHash + ": " + player);
                }
                string playerSqlCommand = "UPDATE DMPServerList SET players=@players WHERE `serverHash`=@serverHash";
                Dictionary<string, object> playerParameters = new Dictionary<string, object>();
                playerParameters.Add("@serverHash", serverHash);
                string currentPlayers = String.Join("\n", players) + "\n";
                playerParameters.Add("@players", currentPlayers);
                databaseConnection.ExecuteNonReader(playerSqlCommand, playerParameters);
                sw.Stop();
                Console.WriteLine("Handled report from " + serverName + " (" + client.address + "), Protocol " + protocolVersion + ", Program Version: " + programVersion + ", Time: " + sw.ElapsedMilliseconds);
            }
        }
    }
}

