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
    class GenerateClientsAction : IComputeAction
    {
        [InstanceResource]
        private readonly IIgnite _ignite;

        private readonly long _clientFirstKey;
        private readonly long _clientKeyCount;
        private readonly string _clientCacheName;

        public GenerateClientsAction(long clientFirstKey, long clientKeyCount, string clientCacheName)
        {
            _clientFirstKey = clientFirstKey;
            _clientKeyCount = clientKeyCount;
            _clientCacheName = clientCacheName;
        }

        public void Invoke()
        {
            using (var streamer = _ignite.GetDataStreamer<long, Client>(_clientCacheName))
            {
                long maxId = _clientFirstKey + _clientKeyCount;
                for (long id = _clientFirstKey; id < maxId; id++)
                {
                    var client = new Client { Name = String.Format("C{0}", id.ToString().PadLeft(7, '0')), Status = "New" };
                    streamer.Add(id, client);
                }
            }
        }
    }

    class GenerateAccountsAction : IComputeAction
    {
        [InstanceResource]
        private readonly IIgnite _ignite;

        private readonly long _clientFirstKey;
        private readonly long _clientKeyCount;
        private readonly string _accountCacheName;
        private readonly long _numAccountsPerClient;

        public GenerateAccountsAction(long clientFirstKey, long clientKeyCount, string accountCacheName, long numAccountsPerClient)
        {
            _clientFirstKey = clientFirstKey;
            _clientKeyCount = clientKeyCount;
            _accountCacheName = accountCacheName;
            _numAccountsPerClient = numAccountsPerClient;
        }

        public void Invoke()
        {
            Random random = new Random();

            using (var streamer = _ignite.GetDataStreamer<AccountKey, Account>(_accountCacheName))
            {
                long maxClientId = _clientFirstKey + _clientKeyCount;
                for (long clientId = _clientFirstKey; clientId < maxClientId; clientId++)
                {
                    for (long accountNum = 0; accountNum < _numAccountsPerClient; accountNum++)
                    {
                        var accountId = clientId * _numAccountsPerClient + accountNum;
                        var accountName = string.Format("C{0}.A{1}", clientId, accountNum);
                        var accountBal = random.Next(0, 50_000);

                        // var accountKey = new AffinityKey(accId, clientId);
                        // var account = new Account { Id = accId, ClientId = clientId, Name = accName, Type = 0, Balance = accBal, Status = "New" };

                        var accountKey = new AccountKey { Id = accountId, ClientId = clientId };
                        var account = new Account { Name = accountName, Type = 0, Balance = accountBal, Status = "New" };

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

                actions.Add(new GenerateClientsAction(firstKey, lastKey - firstKey, cacheName));
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

                actions.Add(new GenerateAccountsAction(firstKey, lastKey - firstKey, cacheName, numAccountsPerClient));
            }

            return actions;
        }

        static void TestClientsUsing(ICache<long, Client> cache, int modulus)
        {
            IList<IList<object>> res = cache.Query(new SqlFieldsQuery("SELECT * FROM Client")).GetAll();
            
            var numClients = (long)res[0][0];
            Console.WriteLine("SELECT Count(*) FROM Client --> " + numClients);

            using (var cursor = cache.Query(new ScanQuery<long, Client>()))
            {
                int i = 0;
                foreach (var entry in cursor)
                {
                    if (++i == 1 || i % modulus == 0 || i == numClients)
                        Console.WriteLine(i + ": Key = " + entry.Key + ", Value = " + entry.Value);
                }
            }
        }

        static void TestAccountsUsing(ICache<AccountKey, Account> cache, int modulus)
        {
            IList<IList<object>> res = cache.Query(new SqlFieldsQuery("SELECT * FROM Account")).GetAll();

            var numAccounts = (long)res[0][0];
            Console.WriteLine("SELECT Count(*) FROM Account --> " + numAccounts);

            using (var cursor = cache.Query(new ScanQuery<AccountKey, Account>()))
            {
                int i = 0;
                foreach (var entry in cursor)
                {
                    if (++i == 1 || i % modulus == 0 || i == numAccounts)
                        Console.WriteLine(i + ": Account = " + entry.Key.Id + ", Client = " + entry.Key.ClientId + ", Value = " + entry.Value);
                }
            }
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
            // Program argument - default values
            const long DEFAULT_NUM_CLIENTS = 1_000_000;
            const long DEFAULT_NUM_ACCOUNTS_PER_CLIENT = 10; // 10_000;
            const int DEFAULT_CLIENT_ID = 1;
            const decimal DEFAULT_AGGR_BALANCE_LIMIT = 100000M;
            const int DEFAULT_NUM_PROCESSORS_PER_DATA_NODE = 3;  // 8;

            // Program argument - run time values
            long numClients;
            long numAccountsPerClient;
            int clientId;
            decimal aggrBalanceLimit;
            int numProcessorsPerDataNode;

            // Initialize program argument run time values
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

            // Main program implementation

            IgniteConfiguration cfg = Utils.GetClientNodeConfiguration();
            using (IIgnite ignite = Ignition.Start(cfg))
            {
                // Destroy previous caches -- start caches from scratch every time
                ignite.DestroyCache(Client.CACHE_NAME);
                ignite.DestroyCache(Account.CACHE_NAME);

                // Create Client cache
                var clientCfg = Client.CacheCfg();
                var clientCache = ignite.GetOrCreateCache<long, Client>(clientCfg);

                // Create Account cache
                var accountCfg = Account.CacheCfg();
                var accountCache = ignite.GetOrCreateCache<AccountKey, Account>(accountCfg);

                // Calculate the total number of jobs required (based upon number of nodes and cpus/node)
                var nodeCount = ignite.GetCluster().GetNodes().Count - 1;
                var jobCount = nodeCount * numProcessorsPerDataNode;

                // Calculate the number and size of batches needed to distribute and populate clients in the compute grid.
                var batchCount = numClients / jobCount + 1;
                var batchSize = numClients / batchCount;


                // ***********   Populate and test Client records using the compute grid  ******************************

                Console.WriteLine("Begin CLIENT generation using cache {0}: {1}", clientCache.Name, DateTime.Now.ToString("h:mm:ss tt"));

                var actions = BuildClientActionsUsing(numClients, clientCache.Name, batchCount, batchSize);
                ignite.GetCluster().ForDataNodes(clientCache.Name).GetCompute().Run(actions);

                Console.WriteLine("Begin CLIENT testing using cache {0}: {1}", clientCache.Name, DateTime.Now.ToString("h:mm:ss tt"));

                TestClientsUsing(clientCache, 1000);

                Console.WriteLine("End CLIENT generation/test: {0}", DateTime.Now.ToString("h:mm:ss tt"));


                // ***********   Populate and test Account records using the compute grid  ******************************

                actions = BuildAccountActions(numClients, accountCache.Name, numAccountsPerClient, batchCount, batchSize);

                Console.WriteLine("Begin ACCOUNT generation using cache {0}: {1}", accountCache.Name, DateTime.Now.ToString("h:mm:ss tt"));

                ignite.GetCluster().ForDataNodes(accountCache.Name).GetCompute().Run(actions);

                Console.WriteLine("Begin ACCOUNT testing using cache {0}: {1}", accountCache.Name, DateTime.Now.ToString("h:mm:ss tt"));

                TestAccountsUsing(accountCache, 1000);

                Console.WriteLine("End ACCOUNT generation/test: {0}", DateTime.Now.ToString("h:mm:ss tt"));


                // ***********   End ClientNode actions  ******************************

                Console.WriteLine();
                Console.WriteLine(">>> ClientNode actions completed, press any key to exit ...");

            }
        }
    }
}
