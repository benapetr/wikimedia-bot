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

                public Value(int number)
                {
                    Data = number.ToString();
                    Type = DataType.Integer;
                }

                public Value(DateTime date)
                {
                    Data = date.Year.ToString() + "-" + date.Month.ToString().PadLeft(2, '0') + "-" + date.Day.ToString().PadLeft(2, '0') + " " + date.Hour.ToString().PadLeft(2, '0') + ":" 
                        + date.Minute.ToString().PadLeft(2, '0') + ":" + date.Second.ToString().PadLeft(2, '0');
                    Type = DataType.Date;
                }

                public Value(bool text)
                {
                    Data = text.ToString();
                    Type = DataType.Boolean;
                }

                public Value(string text, Database.DataType type)
                {
                    Data = text;
                    Type = type;
                }
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
