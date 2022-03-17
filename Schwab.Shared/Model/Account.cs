namespace Schwab.Shared.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Apache.Ignite.Core.Cache.Affinity;
    using Apache.Ignite.Core.Cache.Configuration;


    public class AccountKey
    {
        public long Id { get; set; }

        [AffinityKeyMapped] public ClientKey ClientId { get; set; }
    }


    public class Account
    {
        public static string SQL_SCHEMA = "SDEMO";
        public static string CACHE_NAME = "ACCOUNT_CACHE";

        public static CacheConfiguration CacheCfg()
        {
            return new CacheConfiguration
            {
                SqlSchema = SQL_SCHEMA,
                Name = CACHE_NAME,
                CacheMode = CacheMode.Partitioned,
                Backups = 0,
                QueryEntities = new[]
                {
                    new QueryEntity
                    {
                        KeyType = typeof(AccountKey),
                        ValueType = typeof(Account),
                        Fields = new[] 
                        {
                            new QueryField("Id", typeof(long)),
                            new QueryField("CId", typeof(long)),
                            new QueryField("Name", typeof(string)),
                            new QueryField("Type", typeof(int)),
                            new QueryField("Status", typeof(string)),
                            new QueryField("Balance", typeof(long)),
                            new QueryField("Status", typeof(string))
                        }
                    }
                }
            };
        }

        public Account(long id, long clientId)
        {
            this.Id = id;
            this.CId = clientId;
            this.Name = string.Format("C{0}.A{1}", id, clientId);
            this.Type = 0;
            this.Balance = id % 10;
            this.Status = "NEW";
        }

        public Account(long id, long clientId, string name)
        {
            this.Id = id;
            this.CId = clientId;
            this.Name = name;
            this.Type = 0;
            this.Balance = 0L;
            this.Status = "NEW";
        }

        public Account(long id, long clientId, string name, int type)
        {
            this.Id = id;
            this.CId = clientId;
            this.Name = name;
            this.Type = type;
            this.Balance = 0L;
            this.Status = "NEW";
        }

        public Account(long id, long clientId, String name, int type, long balance)
        {
            this.Id = id;
            this.CId = clientId;
            this.Name = name;
            this.Type = type;
            this.Balance = balance;
            this.Status = "NEW";
        }


        [QuerySqlField(IsIndexed = true)]
        public long Id { get; set; }

        [QuerySqlField(IsIndexed = true)]
        public long CId { get; set; }

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
            return String.Format("{0} [Id={1}, ClientId={2}, Name={3}, Type={4}, Balance={5}, Status={6}]", typeof(Account).Name, Id, CId, Name, Type, Balance, Status);
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