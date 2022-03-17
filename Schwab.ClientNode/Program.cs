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


namespace Schwab.ClientNode
{
    class ClientGeneratorAction : IComputeAction
    {
        [InstanceResource]
        private readonly IIgnite _ignite;

        private readonly long _clientFirstKey;
        private readonly long _clientKeyCount;
        private readonly string _clientCacheName;

        public ClientGeneratorAction(long clientFirstKey, long clientKeyCount, string clientCacheName)
        {
            _clientFirstKey = clientFirstKey;
            _clientKeyCount = clientKeyCount;
            _clientCacheName = clientCacheName;
        }

        public void Invoke()
        {
            using (var streamer = _ignite.GetDataStreamer<long, Client>(_clientCacheName))
            {
                for (long id = _clientFirstKey; id < _clientFirstKey + _clientKeyCount; id++)
                {
                    // var clientKey = new ClientKey { Id = id };
                    var client = new Client { Name = String.Format("C{0}", id.ToString().PadLeft(7, '0')), Status = "New" };

                    streamer.Add(id, client);

                }
            }
        }
    }

    class AccountGeneratorAction : IComputeAction
    {
        [InstanceResource]
        private readonly IIgnite _ignite;

        private readonly long _clientFirstKey;
        private readonly long _clientKeyCount;
        private readonly string _accountCacheName;
        private readonly long _numAccountsPerClient;

        public AccountGeneratorAction(long clientFirstKey, long clientKeyCount, string accountCacheName, long numAccountsPerClient)
        {
            _clientFirstKey = clientFirstKey;
            _clientKeyCount = clientKeyCount;
            _accountCacheName = accountCacheName;
            _numAccountsPerClient = numAccountsPerClient;
        }

        public void Invoke()
        {
            using (var streamer = _ignite.GetDataStreamer<AccountKey, Account>(_accountCacheName))
            {
                for (long clientid = _clientFirstKey; clientid < _clientFirstKey + _clientKeyCount; clientid++)
                {
                    long minId = clientid * _numAccountsPerClient;
                    long maxId = minId + _numAccountsPerClient;
                    for (long accountId = minId; accountId < maxId; accountId++)
                    {
                        // var clientKey = new ClientKey { Id = clientid };
                        var accountKey = new AccountKey { Id = accountId, ClientId = clientid };
                        var account = new Account(accountId, clientid);

                        streamer.Add(accountKey, account);
                    }
                }
            }
        }
    }

    class Program
    {

        static List<IComputeAction> BuildClientActionsUsing(long numClients, string cacheName, long batchCount, long batchSize)
        { 
            var actions = new List<IComputeAction>();
            var lastBatch = batchCount - 1;

            for (long batch = 0; batch < batchCount; batch++)
            {
                var firstKey = batch * batchSize;
                var lastKey = firstKey + batchSize;

                if (lastKey > numClients || batch == lastBatch)
                {
                    lastKey = numClients;
                }

                if (lastKey <= firstKey)
                {
                    break;
                }

                actions.Add(new ClientGeneratorAction(firstKey, lastKey - firstKey, cacheName));
            }

            return actions;
        }

        static List<IComputeAction> BuildAccountActions(long numClients, string cacheName, long numAccountsPerClient, long batchCount, long batchSize)
        {
            var actions = new List<IComputeAction>();
            var lastBatch = batchCount - 1;

            for (int batch = 0; batch < batchCount; batch++)
            {
                var firstKey = batch * batchSize;
                var lastKey = firstKey + batchSize;

                if (lastKey > numClients || batch == lastBatch)
                {
                    lastKey = numClients;
                }

                if (lastKey <= firstKey)
                {
                    break;
                }

                actions.Add(new AccountGeneratorAction(firstKey, lastKey - firstKey, cacheName, numAccountsPerClient));
            }

            return actions;
        }

        static void TestClientsUsing(ICache<long, Client> cache, long numClients)
        {
            IList<IList<object>> res = cache.Query(new SqlFieldsQuery("SELECT * FROM Client")).GetAll();
            Console.WriteLine("SELECT * FROM Client result count: " + res.Count);

            using (var cursor = cache.Query(new ScanQuery<long, Client>()))
            {
                int i = 0;

                foreach (var entry in cursor)
                {
                    i = i + 1;
                    if (i == 1 || i % 1000 == 0 || i > numClients - 100)
                        Console.WriteLine("I= " + i + ", Key = " + entry.Key + ", Value = " + entry.Value);
                }
            }
        }

        static void TestAccountsUsing(ICache<long, Account> cache, long numClients, long numAccountsPerClient)
        {
            IList<IList<object>> res = cache.Query(new SqlFieldsQuery("SELECT * FROM Account")).GetAll();
            Console.WriteLine("SELECT * FROM Account result count: " + res.Count);

            /*
            using (var cursor = cache.Query(new ScanQuery<long, Account>()))
            {
                int i = 0;

                foreach (var entry in cursor)
                {
                    i = i + 1;
                    if (i == 1 || i % 1000 == 0 || i > numClients - 100)
                        Console.WriteLine("I= " + i + ", Key = " + entry.Key + ", Value = " + entry.Value);
                }
            }
            */
        }

        /*

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
            // Default program argument values
            const long DEFAULT_NUM_CLIENTS = 1_000_000;
            const long DEFAULT_NUM_ACCOUNTS_PER_CLIENT = 10; // 10_000;
            const int DEFAULT_CLIENT_ID = 1;
            const decimal DEFAULT_AGGR_BALANCE_LIMIT = 100000M;
            const int DEFAULT_NUM_PROCESSORS_PER_DATA_NODE = 8;

            // Run time program argument values
            long numClients;
            long numAccountsPerClient;
            int clientId;
            decimal aggrBalanceLimit;
            int numProcessorsPerDataNode;

            // Initialize program parameter using either default values or program arguments 
            if (args.Length == 0 || !long.TryParse(args[0], out numClients))
                numClients = DEFAULT_NUM_CLIENTS;
            if (args.Length < 2 || !long.TryParse(args[0], out numAccountsPerClient))
                numAccountsPerClient = DEFAULT_NUM_ACCOUNTS_PER_CLIENT;
            if (args.Length < 3 || !int.TryParse(args[0], out clientId))
                clientId = DEFAULT_CLIENT_ID;
            if (args.Length < 4 || !Decimal.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out aggrBalanceLimit))
                aggrBalanceLimit = DEFAULT_AGGR_BALANCE_LIMIT;
            if (args.Length < 5 || !int.TryParse(args[0], out numProcessorsPerDataNode))
                numProcessorsPerDataNode = DEFAULT_NUM_PROCESSORS_PER_DATA_NODE;


            IgniteConfiguration cfg = Utils.GetClientNodeConfiguration();
            using (IIgnite ignite = Ignition.Start(cfg))
            {
                // Destroy previous caches -- start from scratch every time
                ignite.DestroyCache(Client.CACHE_NAME);
                ignite.DestroyCache(Account.CACHE_NAME);

                // Get or create client cache
                var clientCfg = Client.CacheCfg();
                var clientCache = ignite.GetOrCreateCache<long, Client>(clientCfg);

                // Get or create account cache
                var accountCfg = Account.CacheCfg();
                var accountCache = ignite.GetOrCreateCache<long, Account>(accountCfg);

                // Calculate jobCount - the max number of concurrent jobs (based upon the total number of processors in the data nodes)
                var nodeCount = ignite.GetCluster().GetNodes().Count - 1;
                var jobCount = nodeCount * numProcessorsPerDataNode;

                // Calculate batchCount and batchSize - the number and size of batches needed to distribute and populate clients in the compute grid.
                var batchCount = numClients / jobCount + 1;
                var batchSize = numClients / batchCount;



                // ***********   Populate Client records using the compute grid  ******************************

                Console.WriteLine("Begin Clients Compute using cache {0}: {1}", clientCache.Name, DateTime.Now.ToString("h:mm:ss tt"));

                var actions = BuildClientActionsUsing(numClients, clientCache.Name, batchCount, batchSize);
                ignite.GetCluster().ForDataNodes(clientCache.Name).GetCompute().Run(actions);

                Console.WriteLine("End Clients Compute using cache {0}: {1}", clientCache.Name, DateTime.Now.ToString("h:mm:ss tt"));

                TestClientsUsing(clientCache, numClients);

                Console.WriteLine("End Test Clients using cache {0}: {1}", clientCache.Name, DateTime.Now.ToString("h:mm:ss tt"));



                // ***********   Populate Account records using the compute grid  ******************************

                actions = BuildAccountActions(numClients, accountCache.Name, numAccountsPerClient, batchCount, batchSize);

                Console.WriteLine("Begin Accounts Compute using cache {0}: {1}", accountCache.Name, DateTime.Now.ToString("h:mm:ss tt"));

                ignite.GetCluster().ForDataNodes(accountCache.Name).GetCompute().Run(actions);

                Console.WriteLine("End Accounts Compute using cache {0}: {1}", accountCache.Name, DateTime.Now.ToString("h:mm:ss tt"));

                TestAccountsUsing(accountCache, numClients, numAccountsPerClient);

                Console.WriteLine("End Test Accounts using cache {0}: {1}", accountCache.Name, DateTime.Now.ToString("h:mm:ss tt"));

                Console.WriteLine();
                Console.WriteLine(">>> CLIENT_CACHE load finished, press any key to exit ...");

            }
        }
    }
}
