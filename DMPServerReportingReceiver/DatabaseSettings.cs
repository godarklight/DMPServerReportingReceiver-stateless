using System;
using System.IO;

namespace DMPServerReportingReceiver
{
    public class DatabaseSettings
    {
        public string host;
        public string database;
        public string username;
        public string password;
        private string settingsFile;
        private const string SETTINGS_FILE_NAME = "DatabaseSettings.txt";

        public DatabaseSettings()
        {
            settingsFile = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), SETTINGS_FILE_NAME);
            LoadSettings();
        }

        public void LoadSettings()
        {
            if (!File.Exists(settingsFile))
            {
                SaveSettings();
            }

            using (StreamReader sr = new StreamReader(settingsFile))
            {
                host = sr.ReadLine().Substring("host = ".Length);
                database = sr.ReadLine().Substring("database = ".Length);
                username = sr.ReadLine().Substring("username = ".Length);
                password = sr.ReadLine().Substring("password = ".Length);
            }
        }

        public void SaveSettings()
        {
            if (File.Exists(settingsFile))
            {
                File.Delete(settingsFile);
            }
            using (StreamWriter sw = new StreamWriter(settingsFile, false))
            {
                sw.WriteLine("host = " + host);
                sw.WriteLine("database = " + database);
                sw.WriteLine("username = " + username);
                sw.WriteLine("password = " + password);
            }
        }

        public string ToConnectionString()
        {
            return "Server=" + host + ";Database=" + database + ";Uid=" + username + ";Pwd=" + password + ";";
        }
    }
}

