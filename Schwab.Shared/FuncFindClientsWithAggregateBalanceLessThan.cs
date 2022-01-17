using Apache.Ignite.Core;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Log;
using Apache.Ignite.Core.Resource;
using Schwab.Shared.Model;
using System;
using System.Collections.Generic;

namespace Schwab.Shared
{
    public class FuncFindClientsWithAggregateBalanceLessThan : IComputeFunc<List<AggregateBalance>>
    {
        [InstanceResource]
        private IIgnite _ignite;

        private Decimal Val = 100000M;

        public FuncFindClientsWithAggregateBalanceLessThan(Decimal val)
        {
            this.Val = val;
        }

        public List<AggregateBalance> Invoke()
        {
            _ignite.Logger.Log(LogLevel.Info, string.Format("{0} invoked.", typeof(FuncFindClientsWithAggregateBalanceLessThan).Name));

            // Get client cache
            var clientCache = _ignite.GetCache<int, Client>("CLIENT_CACHE");

            // Initialize results to an empty list of AggregateBalance
            var found = new List<AggregateBalance>();

            // Set up a sql query that returns all client ids and names in the client cache
            var qry = clientCache.Query(new SqlFieldsQuery("SELECT id, name from CLIENT_CACHE.CLIENT"));

            // foreach (int id in clientCache.GetLocalEntries().Select(x => x.Key))  <--- TODO why did this not work? try again time permiiting?
            foreach (var row in qry)
            {
                int clientId = (int)row[0];
                var aggrBalance = (Decimal)_ignite.GetCompute().ExecuteJavaTask<Decimal>("com.gridgain.ignite.ggnode.cgrid.SumBalancesForClientComputeTask", clientId);

                //if (((Object)aggBalance) == null) TODO fix this hack later (JAVA Task should return null or N/A value, not 0 when no recs found?
                //    aggBalance = 0M;

                if (aggrBalance < Val)
                {
                    var ab = new AggregateBalance(clientId, (String)row[1], aggrBalance);
                    found.Add(ab);
                    _ignite.Logger.Log(LogLevel.Debug, string.Format("{0} {1}", typeof(FuncFindClientsWithAggregateBalanceLessThan).Name, ab.ToString()));
                }
            }

            _ignite.Logger.Log(LogLevel.Info, string.Format("{0} found {1} matching clients.", typeof(FuncFindClientsWithAggregateBalanceLessThan).Name, found.Count));

            return found;
        }
    }
}
