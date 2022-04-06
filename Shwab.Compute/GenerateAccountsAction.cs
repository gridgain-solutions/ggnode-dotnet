using System;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Resource;
using Schwab.Shared.Model;

namespace Shwab.Compute
{
    public class GenerateAccountsAction : IComputeAction
    {
        [InstanceResource] private readonly IIgnite _ignite;

        private readonly long _clientFirstKey;
        private readonly long _clientKeyCount;
        private readonly string _accountCacheName;
        private readonly long _numAccountsPerClient;

        public GenerateAccountsAction(long clientFirstKey, long clientKeyCount, string accountCacheName,
            long numAccountsPerClient)
        {
            _clientFirstKey = clientFirstKey;
            _clientKeyCount = clientKeyCount;
            _accountCacheName = accountCacheName;
            _numAccountsPerClient = numAccountsPerClient;
        }

        public void Invoke()
        {
            Console.WriteLine("!!!");
            Console.WriteLine("GenerateAccountsAction");
            Random random = new Random();

            using (var streamer = _ignite.GetDataStreamer<AccountKey, Account>(_accountCacheName))
            {
                streamer.AllowOverwrite = true;
                long maxClientId = _clientFirstKey + _clientKeyCount;
                for (long clientId = _clientFirstKey; clientId < maxClientId; clientId++)
                {
                    for (long accountNum = 0; accountNum < _numAccountsPerClient; accountNum++)
                    {
                        var accountId = clientId * _numAccountsPerClient + accountNum;
                        var accountName = string.Format("C{0}.A{1}", clientId, accountNum);
                        var accountBal = new Decimal(random.Next(0, 50_000));

                        var accountKey = new AccountKey {Id = accountId, ClientId = clientId};
                        var account = new Account {Name = accountName, Type = 0, Balance = accountBal};

                        streamer.Add(accountKey, account);
                    }
                }
            }
        }
    }
}