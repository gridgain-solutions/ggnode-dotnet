using Apache.Ignite.Core;
using Apache.Ignite.Core.Communication.Tcp;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Configuration;
using Apache.Ignite.Core.Deployment;
using Apache.Ignite.Core.Discovery.Tcp;
using Apache.Ignite.Core.Discovery.Tcp.Static;
using Apache.Ignite.Core.Plugin;
using Apache.Ignite.Core.Ssl;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Log;

using GridGain.Core;
using GridGain.Core.Security;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Schwab.Shared;
using Schwab.Shared.Model;
using Apache.Ignite.Core.Cache.Affinity;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Datastream;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Core.Cache.Configuration;


// using Schwab.Shared;
//using Schwab.Shared;
// using Schwab.Shared.Model;

namespace Schwab.ClientNode
{ 
    class DataGeneratorAction : IComputeAction
    {
        [InstanceResource]
        private readonly IIgnite _ignite;

        private readonly long _firstKey;
        private readonly long _keyCount;
        private readonly string _cacheName;

        public DataGeneratorAction(long firstKey, long keyCount, string cacheName)
        {
            _firstKey = firstKey;
            _keyCount = keyCount;
            _cacheName = cacheName;
        }

        public void Invoke()
        {
            using (var streamer = _ignite.GetDataStreamer<long, Client>(_cacheName))

                for (long id = _firstKey; id < _firstKey + _keyCount; id++)
                {
                    var client = new Client(id);

                    streamer.Add(id, client);
                }
        }
    }


    class Program
    {

        /*
        private static String SDemoSpringConfigUrl = "C:\\clients\\Schwab\\demo\\Schwab.Client\\Schwab.Client\\Properties\\spring.xml";


        class BuildBinaryClient : IComputeFunc<KeyValuePair<IIgnite, int>, IBinaryObject>
        {
            public IBinaryObject Invoke(KeyValuePair<IIgnite, int> kv)
            {
                IBinaryObjectBuilder builder = kv.Key.GetBinary().GetBuilder("Client");

                IBinaryObject client = builder
                  .SetField("Id", kv.Value)
                  .SetField("Name", String.Format("C{0}", kv.Value.ToString().PadLeft(7, '0')))
                  .SetField("Status", "New")
                  .Build();

                return client;
            }
        }
        */

        static void Main(string[] args)
        {
            const int defaultClientId = 1;
            const decimal defaultAggrBalanceLimit = 100000M;

            // Program parameters
            int clientId;
            decimal aggrBalanceLimit;

            // Initialize program parameter using default values or program arguments 
            if (args.Length == 0 || !int.TryParse(args[0], out clientId))
                clientId = defaultClientId;
            if (args.Length < 2 || !Decimal.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out aggrBalanceLimit))
                aggrBalanceLimit = defaultAggrBalanceLimit;


            IgniteConfiguration cfg = Utils.GetClientNodeConfiguration();
            // cfg.JavaPeerClassLoadingEnabled = true;
            // cfg.ClientMode = true;

            using (IIgnite ignite = Ignition.Start(cfg))
            {
                 ignite.DestroyCache("CLIENT_CACHE");

                 var clientCfg = new CacheConfiguration {
                    Name = "CLIENT_CACHE",
                    CacheMode = CacheMode.Partitioned,
                    Backups = 0,
                    QueryEntities = new[]
                    {
                        new QueryEntity
                        {
                            KeyType = typeof(long),
                            ValueType = typeof(Client),
                            Fields = new[]
                            {
                                new QueryField("Id", typeof(long)),
                                new QueryField("Name", typeof(string)),
                                new QueryField("Status", typeof(string))
                            },
                            Indexes = new[]
                            {
                                new QueryIndex("Id"),
                            }
                        }
                    }
                 };          

                var clientCache = ignite.GetOrCreateCache<long, Client>(clientCfg);
                Console.WriteLine(String.Format(">>> Cache Name: {0} ...", clientCache.Name));

                long keyCount = 80; // 1_000_000_000;

                var nodeCount = ignite.GetCluster().GetNodes().Count;
                var nodeProcessorCount = 8; // CPU count on server nodes.
                var jobCount = nodeCount * nodeProcessorCount;
                var batchCount = keyCount / jobCount + 1;
                var batchSize = keyCount / batchCount;

                var actions = new List<IComputeAction>();

                for (int batch = 0; batch < batchCount; batch++)
                {
                    var firstKey = batch * batchSize;
                    var lastKey = firstKey + batchSize;

                    if (lastKey > keyCount)
                    {
                        lastKey = keyCount;
                    }

                    if (lastKey <= firstKey)
                    {
                        break;
                    }

                    actions.Add(new DataGeneratorAction(firstKey, lastKey - firstKey, clientCache.Name));
                }

                ignite.GetCompute().Run(actions);

                // var cursor = clientCache.Query(new ScanQuery<long, Client>());

                IList<IList<object>> res = clientCache.Query(new SqlFieldsQuery("SELECT * FROM Client")).GetAll();
                Console.WriteLine(res.Count);


                using (var cursor = clientCache.Query(new ScanQuery<long, Client>()))
                {
                    foreach (var entry in cursor)
                    {
                        Console.WriteLine("Key = " + entry.Key + ", Value = " + entry.Value);
                    }
                }

                Console.WriteLine();
                Console.WriteLine(">>> CLIENT_CACHE load finished, press any key to exit ...");

            }
        }
    }
}
