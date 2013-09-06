using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    /// <summary>
    /// Database
    /// </summary>
    public class Database
    {
        /// <summary>
        /// One row in a database
        /// </summary>
        public class Row
        {
            /// <summary>
            /// One value in a row
            /// </summary>
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

            /// <summary>
            /// Values of a row
            /// </summary>
            public List<Value> Values = new List<Value>();
        }

        /// <summary>
        /// This lock should be locked in case you are working with database to prevent other threads from working with it
        /// </summary>
        public object DatabaseLock = new object();

        /// <summary>
        /// If database is connected or not
        /// </summary>
        public virtual bool IsConnected
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Last db error is stored here
        /// </summary>
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
