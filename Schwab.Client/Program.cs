using Apache.Ignite.Core;
using Schwab.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using Schwab.Shared.Model;

namespace Schwab.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            // Program constants and default values
            const int defaultClientId = 1;
            const decimal defaultAggrBalanceLimit = 100000M;

            // Program parameters
            int clientId;
            decimal aggrBalanceLimit;

            // Initialize program parameter using default values or program arguments 
            if (args.Length == 0 || !int.TryParse(args[0], out clientId))
                clientId = defaultClientId;
            if (args.Length < 2 || !Decimal.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out aggrBalanceLimit))
                aggrBalanceLimit = defaultAggrBalanceLimit;

            // Start GridGain/Ignite thick-client server and perform aggregate balance calculations using both C# and Java compute tasks.
            using (IIgnite ignite = Ignition.Start(Utils.GetClientNodeConfiguration()))
            {
                Console.WriteLine();
                Console.WriteLine(">>> Example: .Net-C#/Java Compute Tasks");
                Console.WriteLine(">>> Demonstrate .Net/C# compute tasks invoking Java compute tasks on any/all GridGain cluster server node(s).");

                Console.WriteLine();
                Console.WriteLine(">>> Calcuate aggregate account balance for the client with id: {0}", clientId);
                Decimal aggrBalance = (Decimal)ignite.GetCompute().Call(new FuncSumBalancesForClient(clientId));
                Console.WriteLine("Aggregate balance: {0:C}", aggrBalance);

                Console.WriteLine();
                Console.WriteLine(">>> Find all clients with an aggregate account balance less than: {0:C}", aggrBalanceLimit);
                var hits = (List<AggregateBalance>)ignite.GetCompute().Call(new FuncFindClientsWithAggregateBalanceLessThan(aggrBalanceLimit));
                Console.WriteLine(String.Format("{0} Clients found:", hits.Count));
                foreach (var hit in hits) Console.WriteLine(hit);

                Console.WriteLine();
                Console.WriteLine(">>> Example finished, press any key to exit ...");
            }
        }
    }
}
