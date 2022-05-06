using Apache.Ignite.Core.Binary;

namespace Schwab.Shared.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Apache.Ignite.Core.Cache.Affinity;
    using Apache.Ignite.Core.Cache.Configuration;


    public class AccountKey : IBinarizable
    {
        [QuerySqlField]
        public long Id { get; set; }

        [QuerySqlField]
        [AffinityKeyMapped]
        public long ClientId { get; set; }

        public void WriteBinary(IBinaryWriter writer)
        {
            var rawWriter = writer.GetRawWriter();
            rawWriter.WriteLong(this.Id);
            rawWriter.WriteLong(this.ClientId);
        }

        public void ReadBinary(IBinaryReader reader)
        {
            var rawReader = reader.GetRawReader();
            Id = rawReader.ReadLong();
            ClientId = rawReader.ReadLong();
        }
    }


    public class Account :IBinarizable
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
                    }
                }
            };
        }

        [QuerySqlField]
        public string Name { get; set; }

        [QuerySqlField]
        public int Type { get; set; }

        [QuerySqlField]
        public Decimal Balance { get; set; }

        public override string ToString()
        {
            return String.Format("{0}[Name={1}, Type={2}, Balance={3:C}]", typeof(Account).Name, Name, Type, Balance);
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

        public void WriteBinary(IBinaryWriter writer)
        {
            var rawWriter = writer.GetRawWriter();
            rawWriter.WriteString( Name);
            rawWriter.WriteInt(Type);
            rawWriter.WriteDecimal( Balance);
        }

        public void ReadBinary(IBinaryReader reader)
        {
            var rawReader = reader.GetRawReader();
            Name = rawReader.ReadString();
            Type = rawReader.ReadInt();
            Balance = rawReader.ReadDecimal().GetValueOrDefault();
        }
    }
}