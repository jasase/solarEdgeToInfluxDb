using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SolarEdgeToMqtt.Mqtt;
using SolarEdgeToMqtt.Repositories;
using SolarEdgeToMqtt.SolarEdgeApi;
using SolarEdgeToMqtt.Jobs;

namespace SolarEdgeToMqtt
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Logging.AddSimpleConsole(x =>
            {
                x.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Disabled;
                x.SingleLine = true;
                x.UseUtcTimestamp = true;
                x.TimestampFormat = "yyyy-MM-dd_hh:mm:ss.ffff - ";
            });

            builder.Services.AddOptions<SolarEdgeSetting>()
                            .BindConfiguration("SolarEdge");

            builder.Services.AddSingleton<MqttClient>();
            builder.Services.AddHostedService(x => x.GetService<MqttClient>());


            builder.Services.AddSingleton<SiteListRepository>();
            builder.Services.AddSingleton<SolarEdgeApiClient>();

            builder.Services.AddHostedService<SolaredgePowerFlowJob>();

            var app = builder.Build();

            app.Run();
        }
    }
}
