using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    public class Database
    {
        public class Row
        {
            public class Value
            {
                public Database.DataType Type;
                public string Data = null;
            }

            public List<Value> Values;
        }

        public object DatabaseLock = new object();

        public virtual bool IsConnected
        {
            get
            {
                return false;
            }
        }

        public string ErrorBuffer = null;

        public virtual void Connect() { }

        public virtual void Disconnect() { }

        public virtual void Commit() { }

        public virtual void Rollback() { }

        public virtual bool InsertRow(string table, Row row)
        {
            return false;
        }

        public enum DataType
        {
            Text,
            Varchar,
            Integer,
            Boolean,
            Date,
        }
    }
}
