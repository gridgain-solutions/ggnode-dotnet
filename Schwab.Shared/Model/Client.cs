namespace Schwab.Shared.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Apache.Ignite.Core.Cache.Affinity;
    using Apache.Ignite.Core.Cache.Configuration;

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
                        KeyType = typeof(long),
                        ValueType = typeof(Client),
                        KeyFieldName = "Id",
                        Fields = new[] {
                            new QueryField("Id", typeof(long)),
                            new QueryField("Name", typeof(string)),
                            new QueryField("Status", typeof(string))
                        }
                    }
                }
            };
        }

        // [QuerySqlField] - Annotation not used so that Key field "Id" (above) can be of type 'long' instead of class ClientKey.
        public string Name { get; set; }

        // [QuerySqlField] - Annotation method not used (for same reason above).
        public string Status { get; set; }

        public override string ToString()
        {
            return string.Format("{0} [Name={1}, Status={2}]", typeof(Client).Name, Name, Status);
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