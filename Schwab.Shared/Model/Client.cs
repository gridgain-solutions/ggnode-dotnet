namespace Schwab.Shared.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Apache.Ignite.Core.Cache.Affinity;
    using Apache.Ignite.Core.Cache.Configuration;

    public class ClientKey
    {
        public ClientKey(long id)
        {
            this.Id = id;
        }

        public long Id { get; set; }
    }


    /// <summary>
    /// Employee.
    /// </summary>
    public class Client
    {
        public static string SQL_SCHEMA = "SDEMO";
        public static string CACHE_NAME = "CLIENT_CACHE";
        
        public static CacheConfiguration CacheCfg() {

            return new CacheConfiguration
            {
                SqlSchema = SQL_SCHEMA,
                Name = CACHE_NAME,
                CacheMode = CacheMode.Partitioned,
                Backups = 0,
                QueryEntities = new[] {
                    new QueryEntity {
                        KeyType = typeof(ClientKey),
                        KeyFieldName = "ClientId",
                        ValueType = typeof(Client),
                        Fields = new[] {
                            new QueryField("ClientId", typeof(ClientKey)),
                            new QueryField("Name", typeof(string)),
                            new QueryField("Status", typeof(string))
                        }
                    }
                }
            };
        }

        public Client(long id)
        {
            this.ClientId = new ClientKey(id);
            this.Name = String.Format("C{0}", id.ToString().PadLeft(7, '0'));
            this.Status = "New";
        }

        [QuerySqlField(IsIndexed = true)]
        public ClientKey ClientId {  get; set; }

        [QuerySqlField]
        public string Name { get; set; }

        [QuerySqlField]
        public string Status { get; set; }

        public override string ToString()
        {
            return string.Format("{0} [Id={1}, Name={2}, Status={3}", typeof(Client).Name, ClientId.Id, Name, Status);
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