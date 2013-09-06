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
using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    /// <summary>
    /// Mysql
    /// </summary>
    public class WMIBMySQL : Database
    {
        MySql.Data.MySqlClient.MySqlConnection Connection = null;
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

        /// <summary>
        /// Insert row
        /// </summary>
        /// <param name="table"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public override bool InsertRow(string table, Row row)
        {
            string sql = "";
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
                }
                catch (MySqlException me)
                {
                    ErrorBuffer = me.Message;
                    core.Log("Error while storing a row to DB " + me.ToString(), true);
                    core.DebugLog("SQL: " + sql);
                    return false;
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
