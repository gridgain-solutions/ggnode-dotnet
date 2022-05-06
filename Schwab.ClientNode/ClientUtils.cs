using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Plugin;
using GridGain.Core;
using GridGain.Core.Security;
using Schwab.Shared;
using Schwab.Shared.Model;
using Shwab.Compute;
using System;

namespace Schwab.ClientNode
{
    public class ClientUtils
    {

        public static IgniteConfiguration GetClientConfig()
        {
            return IsNebula() ? GetNebulaClientConfiguration() : GetLocalClientConfiguration();
        }
        
        public static string SchwabDemoSpringConfigUrl = "resources\\spring.xml";

        private static IgniteConfiguration GetNebulaClientConfiguration()
        {
            Environment.SetEnvironmentVariable("IGNITE_LOG_CLASSPATH_CONTENT_ON_STARTUP", "true");
            Environment.SetEnvironmentVariable("IGNITE_QUIET", "false");

            var config = GetLocalClientConfiguration();

            config.SpringConfigUrl = SchwabDemoSpringConfigUrl;
            //config.BinaryConfiguration = null;
            config.JavaPeerClassLoadingEnabled = false;

            config.PluginConfigurations = new IPluginConfiguration[] {
                new GridGainPluginConfiguration {
                    SecurityCredentialsProvider = new SecurityCredentialsBasicProvider {
                        Credentials = new SecurityCredentials {
                            Login = "sdemo",
                            Password = "my1testkey"
                        }
                    },
                    RollingUpdatesEnabled = true,
                },
            };

            
            return config;
        }


        private static IgniteConfiguration GetLocalClientConfiguration()
        {
            return new IgniteConfiguration(Utils.GetServerNodeConfiguration())
            {
                ClientMode = true,
                //BinaryConfiguration = new BinaryConfiguration(typeof(Account), typeof(AccountKey)),
                JavaPeerClassLoadingEnabled = false,
                //JvmClasspath = null,
            };
        }
        
        private static bool IsNebula()
        {
            return Environment.GetEnvironmentVariable("IsNebula")?.ToLower() == "true";
        }
    }
}