using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Schwab.Shared;
using System;
using System.IO;
using System.Threading;

namespace Schwab.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            IgniteConfiguration igniteConfig;
            
            Console.WriteLine(Environment.GetEnvironmentVariable("JAVA_HOME"));
            

            using (var ignite = Ignition.Start(igniteConfig = Utils.GetServerNodeConfiguration()))
            {
                
                Console.WriteLine(ignite.GetConfiguration().JvmClasspath);
                
                Utils.DeployDefaultServices(ignite);

                Console.WriteLine(">>> Server node started, press any key to exit ...");
                Console.WriteLine();

                // Console.ReadKey();
                Thread.Sleep(Timeout.Infinite);
            }

      
        }
    }
}
