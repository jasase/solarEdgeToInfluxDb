using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using SolarEdgeToMqtt.Repositories;
using SolarEdgeToMqtt.SolarEdgeApi;
using SolarEdgeToMqtt.SolarEdgeApi.Modell;
using SolarEdgeToMqtt.Mqtt;
using SolarEdgeToMqtt.SolarEdgeApi;
using Microsoft.Extensions.Logging;

namespace SolarEdgeToMqtt.Jobs
{
    public class SolaredgePowerFlowJob : BackgroundService
    {
        private readonly SolarEdgeApiClient _apiClient;
        private readonly SiteListRepository _siteListRepository;
        private readonly MqttClient _managedMqttClient;
        private readonly ILogger<SolaredgePowerFlowJob> _logger;

        public string Name => "SolarEdge-PowerFlow";

        public SolaredgePowerFlowJob(SolarEdgeApiClient apiClient,
                                     SiteListRepository siteListRepository,
                                     MqttClient managedMqttClient,
                                     ILogger<SolaredgePowerFlowJob> logger)
        {
            _apiClient = apiClient;
            _siteListRepository = siteListRepository;
            _managedMqttClient = managedMqttClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var site in await _siteListRepository.GetSites())
                {
                    try
                    {
                        await Execute(site);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Getting data failed");
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private async Task Execute(Site site)
        {
            var powerFlow = await _apiClient.CurrentPowerflow(site);

            await PublishEnergy("Grid",
                   site,
                   powerFlow.SiteCurrentPowerFlow.Grid,
                   powerFlow.SiteCurrentPowerFlow.Connections.Any(x => x.To.Equals("Grid", StringComparison.InvariantCultureIgnoreCase)));
            await PublishEnergy("Load",
                   site,
                   powerFlow.SiteCurrentPowerFlow.Load,
                   false);
            await PublishEnergy("Pv",
                   site,
                   powerFlow.SiteCurrentPowerFlow.Pv,
                   false);
            await PublishStorage("Storage",
                          site,
                          powerFlow.SiteCurrentPowerFlow.Storage,
                          powerFlow.SiteCurrentPowerFlow.Connections.Any(x => x.From.Equals("Storage", StringComparison.InvariantCultureIgnoreCase)));
        }


        private async Task PublishEnergy(string type, Site site, PowerflowData data, bool switchFlowDirection)
        {
            var currentPower = switchFlowDirection ? data.CurrentPower * -1 : data.CurrentPower;
            await SendMessage(site.Id, type, "currrentpower", currentPower.ToString());
            await SendMessage(site.Id, type, "status", data.Status);
        }
        private async Task PublishStorage(string type, Site site, PowerflowDataStorage data, bool switchFlowDirection)
        {
            var currentPower = switchFlowDirection ? data.CurrentPower * -1 : data.CurrentPower;
            await SendMessage(site.Id, type, "currrentpower", currentPower.ToString());
            await SendMessage(site.Id, type, "chargelevel", (data.ChargeLevel ?? 0).ToString());
            await SendMessage(site.Id, type, "status", data.Status);
        }


        private async Task SendMessage(int siteId, string type, string valueName, string value)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"solaredge/state/{siteId}/{type.ToLowerInvariant()}/{valueName.ToLowerInvariant()}")
                .WithPayload(value)
                .Build();
            await _managedMqttClient.Client.PublishAsync(message);
        }


    }
}
