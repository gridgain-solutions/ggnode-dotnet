using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Schwab.Shared;
using Shwab.Compute;

namespace Schwab.ClientNode
{
    public class ClientUtils
    {
        public static IgniteConfiguration GetClientNodeConfiguration()
        {
            return new IgniteConfiguration(Utils.GetServerNodeConfiguration())
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
    }
}