namespace Schwab.Shared.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Apache.Ignite.Core.Cache.Affinity;
    using Apache.Ignite.Core.Cache.Configuration;


    /// <summary>
    /// Employee.
    /// </summary>
    public class Client
    {
        public Client(int id)
        {
            this.Id = id;
            this.Name = String.Format("C{0}", id.ToString().PadLeft(7, '0'));
            this.Status = "New";
        }

        [QuerySqlField(IsIndexed = true)]
        public int Id { get; set; }


        [QuerySqlField]
        public string Name { get; set; }


        [QuerySqlField]
        public string Status { get; set; }


        public override string ToString()
        {
            return string.Format("{0} [Id={1}, Name={2}, Status={3}]", typeof(Client).Name, Id, Name, Status);
        }

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