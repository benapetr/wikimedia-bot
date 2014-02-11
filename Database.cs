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
        [Serializable]
        public class Row
        {
            /// <summary>
            /// One value in a row
            /// </summary>
            [Serializable]
            public class Value
            {
                /// <summary>
                /// Type
                /// </summary>
                public Database.DataType Type;
                /// <summary>
                /// Data
                /// </summary>
                public string Data = null;

                /// <summary>
                /// Creates a new value of type int
                /// </summary>
                /// <param name="number"></param>
                public Value(int number)
                {
                    Data = number.ToString();
                    Type = DataType.Integer;
                }

                /// <summary>
                /// Creates a new value of type date
                /// </summary>
                /// <param name="date"></param>
                public Value(DateTime date)
                {
                    Data = date.Year.ToString() + "-" + date.Month.ToString().PadLeft(2, '0') + "-" + date.Day.ToString().PadLeft(2, '0') + " " + date.Hour.ToString().PadLeft(2, '0') + ":" 
                        + date.Minute.ToString().PadLeft(2, '0') + ":" + date.Second.ToString().PadLeft(2, '0');
                    Type = DataType.Date;
                }

                /// <summary>
                /// Creates a new value of type bool
                /// </summary>
                /// <param name="text"></param>
                public Value(bool text)
                {
                    Data = text.ToString();
                    Type = DataType.Boolean;
                }

                /// <summary>
                /// Creates a new value of type text
                /// </summary>
                /// <param name="text"></param>
                /// <param name="type"></param>
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

		public virtual int CacheSize()
		{
			return 0;
		}

        public virtual bool InsertRow(string table, Row row)
        {
            return false;
        }

        /// <summary>
        /// Select a data from db
        /// </summary>
        /// <param name="table">name of table</param>
        /// <param name="rows">Rows separated by comma</param>
        /// <param name="query">Conditions</param>
        /// <param name="columns"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public virtual string Select(string table, string rows, string query, int columns, char separator = '|')
        {
            ErrorBuffer = "SELECT: function is not implemented";
            return null;
        }

        /// <summary>
        /// Data type
        /// </summary>
        public enum DataType
        {
            /// <summary>
            /// Text SQL
            /// </summary>
            Text,
            /// <summary>
            /// Varchar SQL
            /// </summary>
            Varchar,
            /// <summary>
            /// Integer SQL
            /// </summary>
            Integer,
            /// <summary>
            /// Boolean SQL
            /// </summary>
            Boolean,
            /// <summary>
            /// Date SQL
            /// </summary>
            Date,
        }
    }
}
