//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using MySql.Data.MySqlClient;

namespace wmib
{
    /// <summary>
    /// Mysql
    /// </summary>
    public class WMIBMySQL : Database, IDisposable
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

            public SerializedRow()
            {
                row = null;
                table = null;
            }

            public SerializedRow(string name, Row _row)
            {
                row = _row;
                table = name;
            }
        }

        private bool Recovering;
        private readonly Unwritten unwritten = new Unwritten();
        
        private MySqlConnection Connection = null;
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

        private bool connected;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (this.Connection != null)
                this.Connection.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public WMIBMySQL()
        {
            string file = Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "unwrittensql.xml";
            Core.RecoverFile(file);
            if (File.Exists(file))
            {
                Syslog.WarningLog("There is a mysql dump file from previous run containing mysql rows that were never successfuly inserted, trying to recover them");
                XmlDocument document = new XmlDocument();
                using (TextReader sr = new StreamReader(file))
                {
                    document.Load(sr);
                    using (XmlNodeReader reader = new XmlNodeReader(document.DocumentElement))
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(Unwritten));
                        Unwritten un = (Unwritten)xs.Deserialize(reader);
                        lock (unwritten.PendingRows)
                        {
                            unwritten.PendingRows.AddRange(un.PendingRows);
                        }
                    }
                }
            }
            Thread reco = new Thread(Exec) {Name = "MySQL/Recovery"};
            Core.ThreadManager.RegisterThread(reco);
            reco.Start();
        }

        private void Exec()
        {
            try
            {
                Thread.Sleep(8000);
                if (unwritten.PendingRows.Count == 0 && File.Exists (Variables.ConfigurationDirectory + 
                    Path.DirectorySeparatorChar + "unwrittensql.xml"))
                {
                    File.Delete (Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "unwrittensql.xml");
                }
                while (Core.IsRunning)
                {
                    if (unwritten.PendingRows.Count > 0)
                    {
                        int count;
                        Syslog.WarningLog("Performing recovery of " + unwritten.PendingRows.Count + " MySQL rows");
                        Recovering = true;
                        List<SerializedRow> rows = new List<SerializedRow>();
                        lock (unwritten.PendingRows)
                        {
                            count = unwritten.PendingRows.Count;
                            rows.AddRange(unwritten.PendingRows);
                            unwritten.PendingRows.Clear();
                        }
                        int recovered = 0;
                        foreach (SerializedRow row in rows)
                        {
                            if (InsertRow(row.table, row.row))
                            {
                                recovered++;
                            } else
                            {
                                Syslog.DebugLog("Failed to recover 1 row", 2);
                            }
                        }
                        Syslog.WarningLog("Recovery finished, recovered " + recovered + " of total " + count);
                        Recovering = false;
                        FlushRows();
                        Thread.Sleep(200000);
                        if (unwritten.PendingRows.Count == 0 && File.Exists(Variables.ConfigurationDirectory + 
                                                                            Path.DirectorySeparatorChar + "unwrittensql.xml"))
                        {
                            File.Delete(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "unwrittensql.xml");
                        }
                    }
                    Thread.Sleep(200);
                }
            } catch (Exception fail)
            {
                Core.HandleException(fail);
                Syslog.ErrorLog("Recovery thread for Mysql is down");
            }
        }

        public override List<List<string>> Select(string table, string rows, string query, int columns, char separator = '|')
        {
            lock (DatabaseLock)
            {
                if (!IsConnected)
                {
                    ErrorBuffer = "Not connected";
                    return null;
                }
                string sql = "SELECT " + rows + " FROM " + table + " " + query;
                MySqlCommand xx = Connection.CreateCommand();
                xx.CommandText = sql;
                MySqlDataReader r = xx.ExecuteReader();
                List<List<string>> result = new List<List<string>>();
                while (r.Read())
                {
                    List<string> row = new List<string>();
                    int i = 0;
                    while (i < columns)
                    {
                        row.Add(r.GetString(i));
                        i++;
                    }
                    result.Add(row);
                }
                r.Close();
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
            StringBuilder sql = new StringBuilder();
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

                    MySqlCommand mySqlCommand = Connection.CreateCommand();
                    sql.Append("INSERT INTO ");
                    sql.Append(table);
                    sql.Append(" VALUES (");
                    foreach (Row.Value value in row.Values)
                    {
                        switch (value.Type)
                        {
                            case DataType.Boolean:
                            case DataType.Integer:
                                sql.Append(value.Data);
                                sql.Append(", ");
                                break;
                            case DataType.Varchar:
                            case DataType.Text:
                            case DataType.Date:
                                sql.Append("'");
                                sql.Append(MySqlHelper.EscapeString(value.Data));
                                sql.Append("', ");
                                break;
                        }
                    }
                    if (sql.ToString().EndsWith(", "))
                    {
                        sql.Remove(sql.Length - 2, 2);
                    }
                    sql.Append(");");
                    mySqlCommand.CommandText = sql.ToString();
                    mySqlCommand.ExecuteNonQuery();
                    return true;
                } catch (MySqlException me)
                {
                    ErrorBuffer = me.Message;
                    Syslog.Log("Error while storing a row to DB " + me, true);
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

        public override string EscapeInput(string data)
        {
            return MySqlHelper.EscapeString(data);
        }

        public override int Delete(string table, string query)
        {
            int result = 0;
            string sql = "DELETE FROM " + table;
            if (!String.IsNullOrEmpty(query))
                sql += " " + query;
            lock (DatabaseLock)
            {
                try
                {
                    MySqlCommand mySqlCommand = Connection.CreateCommand();
                    mySqlCommand.CommandText = sql;
                    result = mySqlCommand.ExecuteNonQuery();
                }
                catch (MySqlException me)
                {
                    ErrorBuffer = me.Message;
                }
            }
            return result;
        }

        private void FlushRows()
        {
            if (Recovering)
            {
                return;
            }
            // prevent multiple threads calling this function at same time
            lock(this)
            {
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
                    if (File.Exists(Configuration.TempName(file)))
                    {
                        File.Delete(Configuration.TempName(file));
                    }
                } catch (Exception fail)
                {
                    Core.HandleException(fail);
                    Syslog.WarningLog("Recovering the mysql unwritten dump because of exception to: " + file);
                    Core.RecoverFile(file);
                }
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
                    Connection = new MySqlConnection("Server=" + Configuration.MySQL.MysqlHost + ";" +
                                                                              "Database=" + Configuration.MySQL.Mysqldb + ";" +
                                                                              "User ID=" + Configuration.MySQL.MysqlUser + ";" +
                                                                              "Password=" + Configuration.MySQL.MysqlPw + ";" +
                                                                              "port=" + Configuration.MySQL.MysqlPort + ";" +
                                                                              "CharSet=utf8;" +
                                                                              "Pooling=false");
                    Connection.Open();
                    connected = true;
                }
                catch (MySqlException ex)
                {
                    Syslog.Log("MySQL: Unable to connect to server: " + ex, true);
                    connected = false;
                }
            }
        }
    }
}
