using Akka.Configuration;

namespace Treci_projekat
{
    public static class SystemConfig
    {
        public static Config GetAkkaConfig() =>
            ConfigurationFactory.ParseString(@"
            gutenberg-dispatcher {
                type = Dispatcher
                executor = ""fork-join-executor""
                fork-join-executor {
                    parallelism-min = 4
                    parallelism-factor = 2.0
                    parallelism-max = 16
                }
                throughput = 100
            }");
    }
}
