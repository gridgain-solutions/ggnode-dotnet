using Apache.Ignite.Core;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Log;
using Apache.Ignite.Core.Resource;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Schwab.Shared
{
    public class FuncSumBalancesForClient : IComputeFunc<Decimal>
    {
        [InstanceResource]
        private IIgnite _ignite;

        private int clientId = 1;

        public FuncSumBalancesForClient(int clientId)
        {
            this.clientId = clientId;
        }

        public Decimal Invoke()
        {
            Decimal aggrBalance = (Decimal)_ignite.GetCompute().ExecuteJavaTask<Decimal>("com.gridgain.ignite.ggnode.cgrid.SumBalancesForClientComputeTask", clientId);
            _ignite.Logger.Log(LogLevel.Info, string.Format("{0} [id={1}, aggrBalance={2}]", typeof(FuncSumBalancesForClient).Name, clientId, aggrBalance.ToString()));

            return aggrBalance;
        }
    }
}
