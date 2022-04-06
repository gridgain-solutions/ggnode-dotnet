using System;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Resource;
using Schwab.Shared.Model;

namespace Shwab.Compute
{
    public class GenerateClientsAction : IComputeAction
    {
        [InstanceResource] private readonly IIgnite _ignite;

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
            Console.WriteLine("!!!");
            Console.WriteLine("GenerateClientsAction");
            using (var streamer = _ignite.GetDataStreamer<long, Client>(_clientCacheName))
            {
                long maxId = _clientFirstKey + _clientKeyCount;
                for (long id = _clientFirstKey; id < maxId; id++)
                {
                    var client = new Client {Name = String.Format("C{0}", id.ToString().PadLeft(7, '0')), Level = 0};
                    streamer.Add(id, client);
                }
            }
        }
    }
}