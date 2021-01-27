using Framework.Abstraction.Plugins;
using ServiceHost.Docker;

namespace SolarEdgeToInfluxDb
{
    public class Program : Startup
    {
        static void Main(string[] args)
            => new Program().Run(args, BootstrapInCodeConfiguration.Default());
    }
}
