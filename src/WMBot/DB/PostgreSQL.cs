//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

using System.Text;
using System;
using System.Threading;
using System.Collections.Generic;

namespace wmib
{
    public class PostgreSQL : Database
    {
        private static readonly string LocalName = "PostgreSQL";

        private Npgsql.NpgsqlConnection connection;

        public static bool IsAvailable
        {
            get
            {
                return (!String.IsNullOrEmpty(Configuration.Postgres.DBNM) &&
                        !String.IsNullOrEmpty(Configuration.Postgres.Host) &&
                        !String.IsNullOrEmpty(Configuration.Postgres.Pass) &&
                        !String.IsNullOrEmpty(Configuration.Postgres.User));
            }
        }

        public PostgreSQL()
        {
            Npgsql.NpgsqlConnectionStringBuilder conn = new Npgsql.NpgsqlConnectionStringBuilder();
            conn.Database = Configuration.Postgres.DBNM;
            conn.Host = Configuration.Postgres.Host;
            conn.UserName = Configuration.Postgres.User;
            conn.Port = Configuration.Postgres.Port;
            conn.Password = Configuration.Postgres.Pass;
            this.connection = new Npgsql.NpgsqlConnection(conn.ConnectionString);
            //this.connection.Open();
        }

        public override void Connect()
        {
            if (this.IsConnected)
                return;
            this.connection.Open();
        }

        public override void Disconnect()
        {
            this.connection.Close();
        }

        public override void Commit()
        {
            SystemHooks.OnSQL(LocalName, "commit;");
            Npgsql.NpgsqlCommand c = new Npgsql.NpgsqlCommand("commit;", this.connection);
            c.ExecuteNonQuery();
        }

        public override bool IsConnected
        {
            get
            {
                switch (this.connection.State)
                {
                    case System.Data.ConnectionState.Open:
                    case System.Data.ConnectionState.Connecting:
                    case System.Data.ConnectionState.Fetching:
                    case System.Data.ConnectionState.Executing:
                        return true;
                }
                return false;
            }
        }

        private void BindVars(string sql, List<Bind> bind_var, Npgsql.NpgsqlCommand c)
        {
            int n = 0;
            foreach (Bind bind in bind_var)
            {
                switch (bind.Type)
                {
                    case DataType.Boolean:
                        c.Parameters.Add(new Npgsql.NpgsqlParameter(bind.Name, NpgsqlTypes.NpgsqlDbType.Boolean));
                        c.Parameters[n++].Value = bool.Parse(bind.Value);
                        break;
                    case DataType.Integer:
                        c.Parameters.Add(new Npgsql.NpgsqlParameter(bind.Name, NpgsqlTypes.NpgsqlDbType.Integer));
                        c.Parameters[n++].Value = int.Parse(bind.Value);
                        break;
                    case DataType.Varchar:
                        c.Parameters.Add(new Npgsql.NpgsqlParameter(bind.Name, NpgsqlTypes.NpgsqlDbType.Varchar));
                        c.Parameters[n++].Value = bind.Value;
                        break;
                    case DataType.Text:
                        c.Parameters.Add(new Npgsql.NpgsqlParameter(bind.Name, NpgsqlTypes.NpgsqlDbType.Text));
                        c.Parameters[n++].Value = bind.Value;
                        break;
                    case DataType.Date:
                        c.Parameters.Add(new Npgsql.NpgsqlParameter(bind.Name, NpgsqlTypes.NpgsqlDbType.Timestamp));
                        c.Parameters[n++].Value = DateTime.Parse(bind.Value);
                        break;
                }
            }
        }

        public override void ExecuteNonQuery(string sql, List<Bind> bind_var = null)
        {
            if (!this.IsConnected)
                throw new WmibException("The database is not connected");

            Npgsql.NpgsqlCommand c = new Npgsql.NpgsqlCommand(sql, this.connection);
            if (bind_var != null)
                BindVars(sql, bind_var, c);
            SystemHooks.OnSQL(LocalName, sql);
            c.ExecuteNonQuery();
        }

        public override string EscapeInput(string data)
        {
            return null;
        }

        public override int CacheSize()
        {
            return base.CacheSize();
        }

        public override List<List<string>> Select(string sql, List<Database.Bind> bind_var = null)
        {
            if (!this.IsConnected)
                throw new WmibException("The database is not connected");

            Npgsql.NpgsqlCommand command = new Npgsql.NpgsqlCommand(sql, this.connection);
            if (bind_var != null)
                BindVars(sql, bind_var, command);
            SystemHooks.OnSQL(LocalName, sql);
            Npgsql.NpgsqlDataReader dr = command.ExecuteReader();
            List<List<string>> results = new List<List<string>>();
            while (dr.Read())
            {
                List<string> line = new List<string>();
                results.Add(line);
                foreach (object value in dr)
                {
                    line.Add(value.ToString());
                }
            }
            return results;
        }

        public override int Delete(string table, string query)
        {
            int result = 0;
            string sql = "DELETE FROM " + table;
            if (!String.IsNullOrEmpty(query))
                sql += " " + query;
            SystemHooks.OnSQL(LocalName, sql);
            lock (DatabaseLock)
            {
                try
                {
                    Npgsql.NpgsqlCommand SqlCommand = new Npgsql.NpgsqlCommand(sql, this.connection);
                    result = SqlCommand.ExecuteNonQuery();
                }
                catch (Npgsql.NpgsqlException me)
                {
                    ErrorBuffer = me.Message;
                }
            }
            return result;
        }

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
                        //lock(unwritten.PendingRows)
                        //{
                        //    unwritten.PendingRows.Add(new SerializedRow(table, row));
                        //}
                        //FlushRows();
                        return false;
                    }
                    Npgsql.NpgsqlCommand s = new Npgsql.NpgsqlCommand();
                    s.Connection = this.connection;
                    sql.Append("INSERT INTO ");
                    sql.Append(table);
                    // let's check if there are headers
                    bool headers = true;
                    string header = "";
                    foreach (Row.Value value in row.Values)
                    {
                        if (value.Column == null)
                        {
                            headers = false;
                            break;
                        }
                        header += value.Column + ", ";
                    }
                    if (header.EndsWith(", "))
                        header = header.Substring(0, header.Length - 2);
                    if (headers)
                    {
                        sql.Append(" (" + header + ")");
                    }
                    sql.Append(" VALUES (");
                    int cv = 0;
                    foreach (Row.Value value in row.Values)
                    {
                        sql.Append(":v" + cv.ToString() + ", ");
                        switch (value.Type)
                        {
                            case DataType.Boolean:
                                s.Parameters.Add(new Npgsql.NpgsqlParameter("v" + cv.ToString(), NpgsqlTypes.NpgsqlDbType.Boolean));
                                s.Parameters[cv].Value = bool.Parse(value.Data);
                                break;
                                case DataType.Integer:
                                s.Parameters.Add(new Npgsql.NpgsqlParameter("v" + cv.ToString(), NpgsqlTypes.NpgsqlDbType.Integer));
                                s.Parameters[cv].Value = int.Parse(value.Data);
                                break;
                                case DataType.Varchar:
                                s.Parameters.Add(new Npgsql.NpgsqlParameter("v" + cv.ToString(), NpgsqlTypes.NpgsqlDbType.Varchar));
                                s.Parameters[cv].Value = value.Data;
                                break;
                                case DataType.Text:
                                s.Parameters.Add(new Npgsql.NpgsqlParameter("v" + cv.ToString(), NpgsqlTypes.NpgsqlDbType.Text));
                                s.Parameters[cv].Value = value.Data;
                                break;
                                case DataType.Date:
                                s.Parameters.Add(new Npgsql.NpgsqlParameter("v" + cv.ToString(), NpgsqlTypes.NpgsqlDbType.Timestamp));
                                s.Parameters[cv].Value = DateTime.Parse(value.Data);
                                break;
                        }
                        cv++;
                    }
                    if (sql.ToString().EndsWith(", "))
                    {
                        sql.Remove(sql.Length - 2, 2);
                    }
                    sql.Append(");");
                    s.CommandText = sql.ToString();
                    SystemHooks.OnSQL(LocalName, sql.ToString());
                    s.ExecuteNonQuery();
                    return true;
                } catch (Npgsql.NpgsqlException me)
                {
                    ErrorBuffer = me.Message;
                    Syslog.Log("Error while storing a row to DB " + me, true);
                    Syslog.DebugLog("SQL: " + sql);
                    /*lock(unwritten.PendingRows)
                    {
                        unwritten.PendingRows.Add(new SerializedRow(table, row));
                    }
                    FlushRows();


                    */
                    return false;
                }
            }
        }
    }
}

