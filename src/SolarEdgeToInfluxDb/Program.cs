using ServiceHost.Docker;

namespace SolarEdgeToInfluxDb
{
    public class Program : Startup
    {
        static void Main(string[] args)
            => new Program().Run(args);
    }
}
