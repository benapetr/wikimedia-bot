//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

using MySql.Data.MySqlClient;
using System.Xml;
using System;
using System.Xml.Serialization;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    /// <summary>
    /// Mysql
    /// </summary>
    public class WMIBMySQL : Database
    {
		[Serializable]
		public class Unwritten
		{
			public List<SerializedRow> PendingRows = new List<SerializedRow>();
		}

        [Serializable]
        public class SerializedRow
        {
            public Row row;
            public string table;

            public SerializedRow(string name, Row _row)
            {
                row = _row;
                table = name;
            }
        }

        private Thread reco = null;
        private bool Recovering = false;
		private Unwritten unwritten = new Unwritten();
        
        private MySql.Data.MySqlClient.MySqlConnection Connection = null;
        /// <summary>
        /// Return true if mysql is connected to server
        /// </summary>
        public override bool IsConnected
        {
            get
            {
                return connected;
            }
        }

        private bool connected = false;

        public WMIBMySQL()
        {
            string file = Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "unwrittensql.xml";
            Core.RecoverFile(file);
            if (File.Exists(file))
            {
                Syslog.WarningLog("There is a mysql dump file from previous run containing mysql rows that were never successfuly inserted, trying to recover them");
                XmlDocument document = new XmlDocument();
                TextReader sr = new StreamReader(file);
                document.Load(sr);
                XmlNodeReader reader = new XmlNodeReader(document.DocumentElement);
                XmlSerializer xs = new XmlSerializer(typeof(Unwritten));
                Unwritten un = (Unwritten)xs.Deserialize(reader);
                reader.Close();
                sr.Close();
                lock (unwritten.PendingRows)
                {
                    unwritten.PendingRows.AddRange(un.PendingRows);
                }
            }
            reco = new Thread(Exec);
            reco.Name = "Recovery";
			Core.ThreadManager.RegisterThread(reco);
            reco.Start();
        }

        private void Exec()
        {
            try
            {
                while (Core.IsRunning)
                {
                    try
                    {
                        if (!Connection.Ping())
                        {
                            connected = false;
                            Syslog.WarningLog("Mysql connection is dead, trying to fix");
                            Connect();
                        }
                    } catch (Exception fail)
                    {
                        Core.HandleException(fail);
                        Thread.Sleep(200000);
                        continue;
                    }
                    if (unwritten.PendingRows.Count > 0)
                    {
                        int count = 0;
                        Syslog.WarningLog("Performing recovery of " + unwritten.PendingRows.Count.ToString() + " MySQL rows");
                        Recovering = true;
                        List<SerializedRow> rows = new List<SerializedRow>();
                        lock (unwritten.PendingRows)
                        {
                            count = unwritten.PendingRows.Count;
                            unwritten.PendingRows.AddRange(rows);
                            unwritten.PendingRows.Clear();
                        }
                        int recovered = 0;
                        foreach (SerializedRow row in rows)
                        {
                            if (InsertRow(row.table, row.row))
                            {
                                recovered++;
                            }
                        }
                        Syslog.WarningLog("Recovery finished, recovered " + recovered.ToString() + " of total " + count.ToString());
                        Recovering = false;
                        Thread.Sleep(200000);
                    }
                    Thread.Sleep(200);
                }
            } catch (Exception fail)
            {
                Core.HandleException(fail);
				Syslog.ErrorLog("Recovery thread for Mysql is down");
            }
        }

        public override string Select(string table, string rows, string query, int columns, char separator = '|')
        {
            string sql = "";
            string result = "";
            lock (DatabaseLock)
            {
                if (!IsConnected)
                {
                    ErrorBuffer = "Not connected";
                    return null;
                }
                sql = "SELECT " + rows + " FROM " + table + " " + query;
                MySqlCommand xx = Connection.CreateCommand();
                xx.CommandText = sql;
                MySqlDataReader r = xx.ExecuteReader();
                while (r.Read())
                {
                    int i = 0;
                    while (i < columns)
                    {
                        if (result == "")
                        {
                            result += r.GetString(i);
                        }
                        else
                        {
                            result += separator.ToString() + r.GetString(i);
                        }
                        i++;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Insert row
        /// </summary>
        /// <param name="table"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public override bool InsertRow(string table, Row row)
        {
            string sql = "";
            lock(DatabaseLock)
            {
                try
                {
                    if (!IsConnected)
                    {
                        Syslog.DebugLog("Postponing request to insert a row into database which is not connected");
                        lock(unwritten.PendingRows)
                        {
                            unwritten.PendingRows.Add(new SerializedRow(table, row));
                        }
                        FlushRows();
                        return false;
                    }

                    MySqlCommand xx = Connection.CreateCommand();
                    sql = "INSERT INTO " + table + " VALUES (";
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
                            case DataType.Date:
                                sql += "'" + MySql.Data.MySqlClient.MySqlHelper.EscapeString(value.Data) + "', ";
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
                } catch (MySqlException me)
                {
                    ErrorBuffer = me.Message;
                    Syslog.Log("Error while storing a row to DB " + me.ToString(), true);
                    Syslog.DebugLog("SQL: " + sql);
                    lock(unwritten.PendingRows)
                    {
                        unwritten.PendingRows.Add(new SerializedRow(table, row));
                    }
                    FlushRows();
                    return false;
                }
            }
        }

        public override int CacheSize()
        {
            return unwritten.PendingRows.Count;
        }

        private void FlushRows()
        {
            if (Recovering)
            {
                return;
            }
            string file = Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "unwrittensql.xml";
            if (File.Exists(file))
            {
                Core.BackupData(file);
                if (!File.Exists(Configuration.TempName(file)))
                {
                    Syslog.WarningLog("Unable to create backup file for " + file);
                    return;
                }
            }
            try
            {
                File.Delete(file);
                XmlSerializer xs = new XmlSerializer(typeof(Unwritten));
                StreamWriter writer = File.AppendText(file);
                lock(unwritten)
                {
                    xs.Serialize(writer, unwritten);
                }
                writer.Close();
            } catch (Exception fail)
            {
                Core.HandleException(fail);
                Syslog.WarningLog("Recovering the mysql unwritten dump because of exception to: " + file);
                Core.RecoverFile(file);
            }
        }

        /// <summary>
        /// Disconnect mysql
        /// </summary>
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

        /// <summary>
        /// Connect mysql
        /// </summary>
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
                    Connection = new MySql.Data.MySqlClient.MySqlConnection("Server=" + Configuration.MySQL.MysqlHost + ";" +
                                                                              "Database=" + Configuration.MySQL.Mysqldb + ";" +
                                                                              "User ID=" + Configuration.MySQL.MysqlUser + ";" +
                                                                              "Password=" + Configuration.MySQL.MysqlPw + ";" +
                                                                              "port=" + Configuration.MySQL.MysqlPort + ";" +
                                                                              "Pooling=false");
                    Connection.Open();
                    connected = true;
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    Syslog.Log("MySQL: Unable to connect to server: " + ex.ToString(), true);
                    connected = false;
                }
            }
        }
    }
}
