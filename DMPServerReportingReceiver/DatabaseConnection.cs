using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace DMPServerReportingReceiver
{
    public class DatabaseConnection : IDisposable
    {
        private DatabaseSettings settings = new DatabaseSettings();
        private MySqlConnection connection;

        public void Connect()
        {
            if ((connection == null) || (connection.State != System.Data.ConnectionState.Open))
            {
                connection = new MySqlConnection(settings.ToConnectionString());
                connection.Open();
            }
        }

        public int ExecuteNonReader(string mySql)
        {
            return ExecuteNonReader(mySql, null);
        }

        public int ExecuteNonReader(string mySql, Dictionary<string,object> parameters)
        {
            int returnValue;
            using (MySqlCommand command = new MySqlCommand(mySql, connection))
            {
                if (parameters != null)
                {
                    foreach (KeyValuePair<string,object> kvp in parameters)
                    {
                        command.Parameters.AddWithValue(kvp.Key, kvp.Value);
                    }
                }
                returnValue = command.ExecuteNonQuery();
            }
            return returnValue;
        }

        public T ExecuteScalar<T>(string mySql)
        {
            return ExecuteScalar<T>(mySql, null);
        }

        public T ExecuteScalar<T>(string mySql, Dictionary<string,object> parameters)
        {
            T returnValue;
            using (MySqlCommand command = new MySqlCommand(mySql, connection))
            {
                if (parameters != null)
                {
                    foreach (KeyValuePair<string,object> kvp in parameters)
                    {
                        command.Parameters.AddWithValue(kvp.Key, kvp.Value);
                    }
                }
                returnValue = (T)command.ExecuteScalar();
            }
            return returnValue;
        }

        public object[][] ExecuteReader(string mySql)
        {
            return ExecuteReader(mySql, null);
        }

        public object[][] ExecuteReader(string mySql, Dictionary<string,object> parameters)
        {
            object[][] returnValue;
            using (MySqlCommand command = new MySqlCommand(mySql, connection))
            {
                if (parameters != null)
                {
                    foreach (KeyValuePair<string,object> kvp in parameters)
                    {
                        command.Parameters.AddWithValue(kvp.Key, kvp.Value);
                    }
                }
                List<object[]> fieldData = new List<object[]>();
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        object[] fields = new object[reader.FieldCount];
                        reader.GetValues(fields);
                        fieldData.Add(fields);
                    }
                }
                returnValue = fieldData.ToArray();
            }
            return returnValue;
        }



        public void Dispose()
        {
            //Dispose connection stuff here.
        }
    }
}

