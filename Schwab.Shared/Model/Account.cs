namespace Schwab.Shared.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Apache.Ignite.Core.Cache.Affinity;
    using Apache.Ignite.Core.Cache.Configuration;


    public class Account
    {
        public Account(int id, int clientId)
        {
            this.Id = id;
            this.ClientId = clientId;
            this.Name = string.Format("C{0}.A{1}", id, clientId);
            this.Type = 0;
            this.Balance = 0L;
            this.Status = "NEW";
        }

        public Account(int id, int clientId, string name)
        {
            this.Id = id;
            this.ClientId = clientId;
            this.Name = name;
            this.Type = 0;
            this.Balance = 0L;
            this.Status = "NEW";
        }

        public Account(int id, int clientId, string name, int type)
        {
            this.Id = id;
            this.ClientId = ClientId;
            this.Name = name;
            this.Type = type;
            this.Balance = 0L;
            this.Status = "NEW";
        }

        public Account(int id, int clientId, String name, int type, long balance)
        {
            this.Id = id;
            this.ClientId = clientId;
            this.Name = name;
            this.Type = type;
            this.Balance = balance;
            this.Status = "NEW";
        }


        [QuerySqlField(IsIndexed = true)]
        public int Id { get; set; }

        [QuerySqlField(IsIndexed = true)]
        [AffinityKeyMapped]
        public int ClientId { get; set; }

        [QuerySqlField]
        public string Name { get; set; }

        [QuerySqlField]
        public int Type { get; set; }

        [QuerySqlField]
        public long Balance { get; set; }

        [QuerySqlField]
        public string Status { get; set; }

        public override string ToString()
        {
            return String.Format("{0} [Id={1}, ClientId={2}, Name={3}, Type={4}, Balance={5}, Status={6}]", typeof(Account).Name, Id, ClientId, Name, Type, Balance, Status);
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