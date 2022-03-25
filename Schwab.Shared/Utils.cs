/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Schwab.Shared
{
    using System;
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Client;
    using Apache.Ignite.Core.Client.Cache;
    using Apache.Ignite.Core.Deployment;
    using Apache.Ignite.Core.Discovery.Tcp;
    using Apache.Ignite.Core.Discovery.Tcp.Static;
    using Apache.Ignite.Core.Log;
    using Schwab.Shared.Services;
    using Apache.Ignite.Core.Binary;
    using Schwab.Shared.Model;
    using System.IO;

    using GridGain.Core;
    using GridGain.Core.Security;
    using Apache.Ignite.Core.Plugin;

    /// <summary>
    /// Common configuration and sample data.
    /// </summary>
    public static class Utils
    {
        //public static string GG_CONFIGS_PATH = Environment.GetEnvironmentVariable("SCHWAB_DEMO_GG_CONFIGS_PATH");
        //public static string SchwabDemoJvmClassPath = Environment.GetEnvironmentVariable("SCHWAB_DEMO_GG_JVMCLASSPATH");
        //public static string SchwabDemoSpringConfigUrl = Path.Combine(GG_CONFIGS_PATH, "config", "server-config.xml");

        // Currently not used or redundant
        // public static string SchwabDemoOptionalPath = Path.Combine(GG_CONFIGS_PATH, "optional");  // path to optional libs such as control-center-agent 
        // public static string SchwabDemoUserlibsPath = Path.Combine(GG_CONFIGS_PATH, "userlibs");  // path to folder containing the same jar files referenced in SchwabDemoJvmClassPath

        // Hard-wired setttings (that were used before converting to enviroment variables above)
        // public static string SchwabDemoSpringConfigUrl = "C:\\clients\\Schwab\\demo\\gg\\config\\nebula-server-config.xml";
        public static string SchwabDemoSpringConfigUrl = "config\\local-dotnet-server-config.xml";
        //public static string SchwabDemoOptionalPath = "gg/optional";
        //public static string SchwabDemoUserlibsPath = "gg/userlibs";
        public static string SchwabDemoJvmClassPath = "C:\\clients\\Schwab\\demo\\ggnode-dotnet\\gg\\userlibs\\model-1.0.jar;C:\\clients\\Schwab\\demo\\ggnode-dotnet\\gg\\userlibs\\application-1.0.jar";
        // public static string SchwabDemoJvmClassPath = "C:\\clients\\Schwab\\demo\\ggnode\\model\\target\\model-1.0.jar;C:\\clients\\Schwab\\demo\\ggnode\\application\\target\\application-1.0.jar";

        /// <summary>
        /// Initializes the <see cref="Utils"/> class.
        /// </summary>
        static Utils()
        {
            // Only necessary during Ignite development.
            Environment.SetEnvironmentVariable("IGNITE_NATIVE_TEST_CLASSPATH", "true");
        }

        /// <summary>
        /// Gets the server node configuration.
        /// </summary>
        public static IgniteConfiguration GetServerNodeConfiguration()
        {
            // None of the options below are mandatory for the examples to work.
            // * Discovery and localhost settings improve startup time
            // * Logging options reduce console output
            //Localhost = "127.0.0.1",
            //DiscoverySpi = new TcpDiscoverySpi
            //{
            //    IpFinder = new TcpDiscoveryStaticIpFinder
            //    {
            //        Endpoints = new[] { "127.0.0.1:47500..47505" }
            //    },
            //    SocketTimeout = TimeSpan.FromSeconds(0.3)
            //},
            return new IgniteConfiguration
            {
                JvmOptions = new[]
                {
                    "-DIGNITE_QUIET=true",
                    "-DIGNITE_PERFORMANCE_SUGGESTIONS_DISABLED=true",
                    "--add-exports=java.base/jdk.internal.misc=ALL-UNNAMED",
                    "--add-exports=java.base/sun.nio.ch=ALL-UNNAMED",
                    "--add-exports=java.management/com.sun.jmx.mbeanserver=ALL-UNNAMED",
                    "--add-exports=jdk.internal.jvmstat/sun.jvmstat.monitor=ALL-UNNAMED",
                    "--add-exports=java.base/sun.reflect.generics.reflectiveObjects=ALL-UNNAMED",
                    "--add-opens=jdk.management/com.sun.management.internal=ALL-UNNAMED",
                    "--illegal-access="
                },
                Logger = new ConsoleLogger
                {
                    MinLevel = LogLevel.Error
                },
                PeerAssemblyLoadingMode = PeerAssemblyLoadingMode.CurrentAppDomain,
                JavaPeerClassLoadingEnabled = true,
                SpringConfigUrl = SchwabDemoSpringConfigUrl,
                JvmClasspath = SchwabDemoJvmClassPath,
                BinaryConfiguration = new BinaryConfiguration(typeof(FuncSumBalancesForClient))
            };
        }


        public static SecurityCredentials clientCredentials = new SecurityCredentials
        {
            Login = "sdemo",
            Password = "my1testkey"
        };

        /// <summary>
        /// Gets the thick client node configuration.
        /// </summary>
        public static IgniteConfiguration GetClientNodeConfiguration()
        {
            return new IgniteConfiguration(GetServerNodeConfiguration())
            {
                ClientMode = true,
                BinaryConfiguration = new BinaryConfiguration(typeof(FuncSumBalancesForClient)),
                /*
                PluginConfigurations = new IPluginConfiguration[]
                {
                    new GridGainPluginConfiguration
                    {
                        SecurityCredentialsProvider = new SecurityCredentialsBasicProvider
                        {
                            Credentials = clientCredentials
                        },
                        RollingUpdatesEnabled = true,
                    },
                }
                */
            };
        }

        /// <summary>
        /// Gets the thin client node configuration.
        /// </summary>
        public static IgniteClientConfiguration GetThinClientConfiguration()
        {
            return new IgniteClientConfiguration
            {
                Endpoints = new[]
                {
                    "127.0.0.1"
                }
            };
        }

        /// <summary>
        /// Deploys default services.
        /// </summary>
        public static void DeployDefaultServices(IIgnite ignite)
        {
            ignite.GetServices().DeployNodeSingleton("default-map-service", new MapService<int, string>());
        }
    }
}
