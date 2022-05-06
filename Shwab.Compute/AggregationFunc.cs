using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Log;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Core.Cluster;
using Schwab.Shared.Model;

namespace Shwab.Compute
{
    public class AggregationFunc : IComputeJob<Dictionary<int, Decimal>>
    {
        [InstanceResource] private IIgnite _ignite;

        public AggregationFunc()
        {
        }

        public Dictionary<int, decimal> Execute()
        {
            var accounts = _ignite.GetCache<IBinaryObject, IBinaryObject>("ACCOUNT_CACHE").WithKeepBinary<IBinaryObject, IBinaryObject>();

            var partitions = _ignite.GetAffinity("ACCOUNT_CACHE").GetPrimaryPartitions(_ignite.GetCluster().GetLocalNode());

            //partitions = partitions.Take(100).ToArray();
            
            Console.WriteLine($"TOTAL: {partitions.Length}");
            Console.WriteLine($"AAAAA: {partitions.Length}");

            var result = new ConcurrentDictionary<int, Decimal>();

            var tasks = new List<Task>();

            foreach (var p in partitions)
            {
                var task = new Task(() =>
                {
                    var cursor = accounts.Query(
                        new ScanQuery<IBinaryObject, IBinaryObject>()
                        {
                            Local = true,
                            Partition = p
                        });

                    int test = 0;
                    foreach (var e in cursor)
                    {
                        test++;
                        var accKey = e.Key.Deserialize<AccountKey>();
                       // Console.WriteLine(accKey.ClientId);
                        var account = e.Value.Deserialize<Account>();
                        // var balance = account.GetField<Decimal>("Balance");
                        // int clientId = accKey.GetField<int>("ClientId");
                        // if (balance > 0)
                        // {
                        //     result.AddOrUpdate(clientId, 
                        //         k => balance, 
                        //         (k, curr) => curr + balance
                        //     );
                        // }
                    }
                });
                
                task.Start();
                tasks.Add(task);

            };

            Task.WaitAll(tasks.ToArray());
                   

            var MAX = 250_000_000M;

            // return result.Where(x => x.Value > MAX)
            //     .ToDictionary(x => x.Key, y => y.Value);

            return result.ToDictionary(x => x.Key, y => y.Value);
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

     
    }
}