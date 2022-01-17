namespace Schwab.Shared.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Apache.Ignite.Core.Cache.Configuration;


    /// <summary>
    /// Employee.
    /// </summary>
    public class Client
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name.</param>
        public Client(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param id="id">Client Id.</param>
        public Client(int id)
        {

            Name = "Client-" + id.ToString();
        }


        /// <summary>
        /// Name.
        /// </summary>
        [QuerySqlField]
        public string Name { get; set; }


        /// <summary>
        /// Name.
        /// </summary>
        [QuerySqlField]
        public string Status { get; set; }


        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} [name={1}, status={2}]", typeof(Client).Name, Name, Status);
        }

        /// <summary>
        /// Get string representation of collection.
        /// </summary>
        /// <returns></returns>
        private static string CollectionToString<T>(ICollection<T> col)
        {
            if (col == null)
                return "null";

            var elements = col.Any()
                ? col.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y)
                : string.Empty;

            return string.Format("[{0}]", elements);
        }
    }
}