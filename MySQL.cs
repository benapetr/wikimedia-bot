using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    public class MySQL : Database
    {
        MySql.Data.MySqlClient.MySqlConnection Connection = null;
        public override bool IsConnected
        {
            get
            {
                return connected;
            }
        }

        private bool connected = false;

        public override bool InsertRow(string table, Row row)
        {
            lock (DatabaseLock)
            {
                try
                {
                    if (!IsConnected)
                    {
                        core.DebugLog("Ignoring request to insert a row into database which is not connected");
                        return false;
                    }

                    MySqlCommand xx = Connection.CreateCommand();
                    string sql = "INSERT INTO " + table + " VALUES (";
                    foreach (Database.Row.Value value in row.Values)
                    {
                        switch (value.Type)
                        {
                            case DataType.Boolean:
                            case DataType.Integer:
                                sql += value.Data + ", ";
                                break;
                            case DataType.Varchar:
                            case DataType.Text:
                                sql += "'" + value.Data + "', ";
                                break;
                        }
                    }
                    if (sql.EndsWith(", "))
                    {
                        sql = sql.Substring(0, sql.Length - 2);
                    }
                    sql += ");";
                    xx.CommandText = sql;
                    xx.ExecuteNonQuery();
                    return true;
                }
                catch (MySqlException me)
                {
                    ErrorBuffer = me.Message;
                    core.DebugLog("Error while storing a row to DB " + me.ToString());
                    return false;
                }
            }
        }

        public override void Disconnect()
        {
            lock (DatabaseLock)
            {
                if (IsConnected)
                {
                    Connection.Close();
                    connected = false;
                }
            }
        }

        public override void Connect()
        {
            lock (DatabaseLock)
            {
                if (IsConnected)
                {
                    return;
                }
                try
                {
                    Connection = new MySql.Data.MySqlClient.MySqlConnection("Server=" + config.MysqlHost + ";" +
                                                                              "Database=" + config.Mysqldb + ";" +
                                                                              "User ID=" + config.MysqlUser + ";" +
                                                                              "Password=" + config.MysqlPw + ";" +
                                                                              "port=" + config.MysqlPort + ";" +
                                                                              "Pooling=false");
                    Connection.Open();
                    connected = true;
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    core.Log("MySQL: Unable to connect to server: " + ex.ToString(), true);
                    connected = false;
                }
            }
        }
    }
}
