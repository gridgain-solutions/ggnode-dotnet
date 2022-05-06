using System;
using System.Linq;
using System.Threading;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Log;
using Apache.Ignite.Core.Resource;
using Schwab.Shared.Model;

namespace Shwab.Compute
{
    public class MyTestAction : IComputeFunc<int>
    {
        [InstanceResource] private IIgnite _ignite;


        public MyTestAction()
        {
        }

        public int Invoke()
        {
            var clientCfg = Client.CacheCfg();
            var clientCache = _ignite.GetOrCreateCache<long, Client>(clientCfg);
            int count = clientCache.GetLocalEntries().Count();
            
            Thread.Sleep(TimeSpan.FromMilliseconds(10000));

            return count;
        }
    }
}