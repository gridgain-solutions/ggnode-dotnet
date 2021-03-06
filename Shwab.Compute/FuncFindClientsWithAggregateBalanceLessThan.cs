using System;
using System.Collections.Generic;
using System.Linq;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Log;
using Apache.Ignite.Core.Resource;
using Schwab.Shared.Model;

namespace Shwab.Compute
{
    public class FuncFindClientsWithAggregateBalanceLessThan : IComputeFunc<List<AggregateBalance>>
    {
        [InstanceResource]
        private IIgnite _ignite;

        private Decimal Val = 250000M;

        public FuncFindClientsWithAggregateBalanceLessThan(Decimal val)
        {
            this.Val = val;
        }

        public List<AggregateBalance> Invoke()
        {
            Console.WriteLine("!!!");
            Console.WriteLine("FuncFindClientsWithAggregateBalanceLessThan");
            
            _ignite.Logger.Log(LogLevel.Info, string.Format("{0} invoked.", typeof(FuncFindClientsWithAggregateBalanceLessThan).Name));

            // Get client cache
            var clientCfg = Client.CacheCfg();
            var clientCache = _ignite.GetOrCreateCache<long, Client>(clientCfg);

            // Initialize results to an empty list of AggregateBalance
            var found = new List<AggregateBalance>();

            var keys = clientCache.GetLocalEntries().Select(x => x.Key).ToList();

            foreach (var key in keys)
            {
                int clientId = Convert.ToInt32(key);
                var aggrBalance = (Decimal?)_ignite.GetCluster().ForLocal().GetCompute().ExecuteJavaTask<Decimal?>("com.gridgain.ignite.ggnode.cgrid.SumBalancesForClientTaskLocal", clientId);

                if (aggrBalance != null && aggrBalance < Val)
                {
                    //var ab = new AggregateBalance(clientId, (String)row[1], aggrBalance);
                    var ab = new AggregateBalance(clientId, String.Format("C{0}", key.ToString().PadLeft(7, '0')), aggrBalance.Value);
                    found.Add(ab);
                    _ignite.Logger.Log(LogLevel.Debug, string.Format("{0} {1}", typeof(FuncFindClientsWithAggregateBalanceLessThan).Name, ab.ToString()));
                }
            }

            _ignite.Logger.Log(LogLevel.Info, string.Format("{0} found {1} matching clients.", typeof(FuncFindClientsWithAggregateBalanceLessThan).Name, found.Count));

            return found;
        }
    }
}