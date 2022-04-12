using Apache.Ignite.Core;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Resource;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Schwab.Shared.Model;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Query;
using Shwab.Compute;
using System.Collections;
using System.Diagnostics;


namespace Schwab.ClientNode
{
    class Program
    {
        static List<IComputeAction> BuildClientActionsUsing(long numClients, string cacheName, long batchCount,
            long batchSize)
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

        static List<IComputeAction> BuildAccountActions(long numClients, string cacheName, long numAccountsPerClient,
            long batchCount, long batchSize)
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

        static Hashtable SumBalancesForAllClients(decimal? threshold, IIgnite ignite)
        {
            return ignite.GetCluster().ForServers().GetCompute()
                .ExecuteJavaTask<Hashtable>(
                    "com.gridgain.ignite.ggnode.cgrid.SumBalancesForAllClientsTask", threshold);
        }

        static void TestClientsUsing(ICache<long, Client> cache, int modulus)
        {
            IList<IList<object>> res = cache.Query(new SqlFieldsQuery("SELECT COUNT(*) FROM Client")).GetAll();

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
            IList<IList<object>> res = cache.Query(new SqlFieldsQuery("SELECT COUNT(*) FROM Account")).GetAll();

            var numAccounts = (long)res[0][0];
            Console.WriteLine("SELECT Count(*) FROM Account --> " + numAccounts);

            using (var cursor = cache.Query(new ScanQuery<AccountKey, Account>()))
            {
                int i = 0;
                foreach (var entry in cursor)
                {
                    if (++i == 1 || i % modulus == 0 || i == numAccounts)
                        Console.WriteLine(i + ": KEY = AccountKey[Id=" + entry.Key.Id + ", Client = " +
                                          entry.Key.ClientId + "], Value = " + entry.Value);
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
            const long DEFAULT_NUM_CLIENTS = 1000;
            //const long DEFAULT_NUM_CLIENTS = 1_000_000;
            const long DEFAULT_NUM_ACCOUNTS_PER_CLIENT = 10_000; // 10_000;
            const decimal DEFAULT_AGGR_BALANCE_LIMIT = 75000M;
            const int DEFAULT_NUM_PROCESSORS_PER_DATA_NODE = 3; // 8;

            const int DEFAULT_MIN_TEST_CLIENT_ID = 1;
            const int DEFAULT_MAX_TEST_CLIENT_ID = 10;

            // Program argument - run time values
            long numClients;
            long numAccountsPerClient;
            decimal aggrBalanceLimit;
            int numProcessorsPerDataNode;

            int minTestClientId;
            int maxTestClientId;

            // Initialize program argument run time values
            if (args.Length == 0 || !long.TryParse(args[0], out numClients))
                numClients = DEFAULT_NUM_CLIENTS;
            if (args.Length < 2 || !long.TryParse(args[1], out numAccountsPerClient))
                numAccountsPerClient = DEFAULT_NUM_ACCOUNTS_PER_CLIENT;
            if (args.Length < 3 || !Decimal.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out aggrBalanceLimit))
                aggrBalanceLimit = DEFAULT_AGGR_BALANCE_LIMIT;
            if (args.Length < 4 || !int.TryParse(args[3], out numProcessorsPerDataNode))
                numProcessorsPerDataNode = DEFAULT_NUM_PROCESSORS_PER_DATA_NODE;
            if (args.Length < 5 || !int.TryParse(args[4], out minTestClientId))
                minTestClientId = DEFAULT_MIN_TEST_CLIENT_ID;
            if (args.Length < 6 || !int.TryParse(args[5], out maxTestClientId))
                maxTestClientId = DEFAULT_MAX_TEST_CLIENT_ID;

            // Main program implementation

            IgniteConfiguration cfg = ClientUtils.GetClientConfig();
            using (IIgnite ignite = Ignition.Start(cfg))
            {
                //Console.Write("Initialize and populate caches: Y/N [N]");
                // string ok = Console.ReadLine().Trim().ToLower();
                //  bool populateCaches = ok.Length > 0 && ok[0] == 'y';


                bool populateCaches = true;
                if (populateCaches)
                {
                    PopulateCaches(ignite, numProcessorsPerDataNode, numClients, numAccountsPerClient);
                }

                Console.WriteLine();
                Console.WriteLine(">>> Example: .Net-C#/Java Compute Tasks");
                Console.WriteLine(
                    ">>> Demonstrate .Net/C# compute tasks invoking Java compute tasks on any/all GridGain cluster server node(s).");
                Console.WriteLine();
                
                {
                    Console.WriteLine(">>> Example1: all computations on java side");
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    FindAllClientsWithAggregateBalanceFilterOnJavaSide(aggrBalanceLimit, ignite);
                    Console.WriteLine($"$>>> Example1 took {sw.Elapsed}");
                }
                
                {
                    Console.WriteLine(">>> Example2: most computations on java side, filter on .net side");
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    FindAllClientsWithAggregateBalanceFilterOnNetSide(aggrBalanceLimit, ignite);
                    Console.WriteLine($"$>>> Example2 took {sw.Elapsed}");
                }
                
                {
                    Console.WriteLine(">>> Example3: using small java jobs, reducing and filtering on .net");
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    FindAllClientsWithAggregateBalanceUsingSql(aggrBalanceLimit, ignite);
                    Console.WriteLine($"$>>> Example3 took {sw.Elapsed}");
                }
                
                {
                    Console.WriteLine(">>> Example4: using small java jobs, reducing and filtering on .net");
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    FindAllClientsWithAggregateBalanceUsingSmallJobs(aggrBalanceLimit, ignite);
                    Console.WriteLine($"$>>> Example4 took {sw.Elapsed}");
                }

                Console.WriteLine();
                Console.WriteLine(">>> ClientNode actions completed, press any key to exit ...");
            }
        }

        private static void PopulateCaches(IIgnite ignite, int numProcessorsPerDataNode, long numClients,
            long numAccountsPerClient)
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
            // var batchCount = numClients / jobCount + 1;
            // var batchSize = numClients / batchCount;

            var batchCount = 1;
            var batchSize = 1;


            // ***********   Populate and test Client records using the compute grid  ******************************

            PrintRed("Starting");


            Console.WriteLine("Begin CLIENT generation using cache {0}: {1}", clientCache.Name,
                DateTime.Now.ToString("h:mm:ss tt"));

            var actions = BuildClientActionsUsing(numClients, clientCache.Name, batchCount, batchSize);

            PrintRed(actions.Count);

            ignite.GetCluster().ForDataNodes(clientCache.Name).GetCompute().Run(actions);

            Console.WriteLine("Begin CLIENT testing using cache {0}: {1}", clientCache.Name,
                DateTime.Now.ToString("h:mm:ss tt"));

            TestClientsUsing(clientCache, 1000);

            Console.WriteLine("End CLIENT generation/test: {0}", DateTime.Now.ToString("h:mm:ss tt"));


            // ***********   Populate and test Account records using the compute grid  ******************************

            actions = BuildAccountActions(numClients, accountCache.Name, numAccountsPerClient, batchCount,
                batchSize);

            PrintRed(actions.Count);

            Console.WriteLine("Begin ACCOUNT generation using cache {0}: {1}", accountCache.Name,
                DateTime.Now.ToString("h:mm:ss tt"));

            ignite.GetCluster().ForDataNodes(accountCache.Name).GetCompute().Run(actions);

            Console.WriteLine("Begin ACCOUNT testing using cache {0}: {1}", accountCache.Name,
                DateTime.Now.ToString("h:mm:ss tt"));

            // TestAccountsUsing(accountCache, 1000);

            Console.WriteLine("End ACCOUNT generation/test: {0}", DateTime.Now.ToString("h:mm:ss tt"));
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
            Environment.Exit(1);
        }

        private static void PrintRed(object message)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void FindAllClientsWithAggregateBalanceFilterOnJavaSide(decimal threshold, IIgnite ignite)
        {
            Hashtable res = SumBalancesForAllClients(threshold, ignite);
            
            Console.WriteLine($"$Got {res.Count} results");
        }

        public static void FindAllClientsWithAggregateBalanceFilterOnNetSide(decimal threshold, IIgnite ignite)
        {
            Hashtable jobRes = SumBalancesForAllClients(null, ignite);

            var res = new Dictionary<AccountKey, decimal>(); 
            foreach (DictionaryEntry e in jobRes)
            {
                if ((decimal) e.Value < threshold)
                {
                    res.Add((AccountKey)e.Key, (decimal)e.Value);
                }
            }
            
            Console.WriteLine($"$got {res.Count} results");
        }

        public static void FindAllClientsWithAggregateBalanceUsingSmallJobs(decimal threshold, IIgnite ignite)
        {
            try
            {
                var nodeHits = ignite.GetCluster().ForDataNodes(Client.CACHE_NAME).GetCompute()
                    .Broadcast<List<AggregateBalance>>(
                        new FuncFindClientsWithAggregateBalanceLessThan(threshold));


                var hits = nodeHits.SelectMany(list => list).ToList();
                Console.WriteLine(String.Format("{0} Clients found: {1}", hits.Count,
                    DateTime.Now.ToString("h:mm:ss tt")));
                foreach (var hit in hits) Console.WriteLine(hit);
                Console.WriteLine(String.Format("{0} Clients found: {1}", hits.Count,
                    DateTime.Now.ToString("h:mm:ss tt")));
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void FindAllClientsWithAggregateBalanceUsingSql(decimal threshold, IIgnite ignite)
        {
            ICache<AccountKey, Account> cache = ignite.GetCache<AccountKey, Account>(Account.CACHE_NAME);
            var query = new SqlFieldsQuery("select clientId, sum(balance) from Account group by clientId having sum(balance) < ?");
            query.Arguments = new object[] {threshold};
            IList<IList<object>> res = cache.Query(query).GetAll();
            
            Console.WriteLine($"$got {res.Count} results");
        }
    }
}