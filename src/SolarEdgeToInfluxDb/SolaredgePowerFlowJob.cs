using System;
using System.Linq;
using Framework.Abstraction.Services.DataAccess.InfluxDb;
using Framework.Abstraction.Services.Scheduling;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using SolarEdgeToInfluxDb.Repositories;
using SolarEdgeToInfluxDb.SolarEdgeApi;
using SolarEdgeToInfluxDb.SolarEdgeApi.Modell;

namespace SolarEdgeToInfluxDb
{
    public class SolaredgePowerFlowJob : IJob
    {
        private readonly SolarEdgeApiClient _apiClient;
        private readonly SiteListRepository _siteListRepository;
        private readonly IInfluxDbUpload _influxDbUpload;
        private readonly IManagedMqttClient _managedMqttClient;
        private readonly SolarEdgeSetting _solarEdgeSetting;

        public string Name => "SolarEdge-PowerFlow";

        public SolaredgePowerFlowJob(SolarEdgeApiClient apiClient,
                                     SiteListRepository siteListRepository,
                                     IInfluxDbUpload influxDbUpload,
                                     IManagedMqttClient managedMqttClient,
                                     SolarEdgeSetting solarEdgeSetting)
        {
            _apiClient = apiClient;
            _siteListRepository = siteListRepository;
            _influxDbUpload = influxDbUpload;
            _managedMqttClient = managedMqttClient;
            _solarEdgeSetting = solarEdgeSetting;
        }

        public void Execute()
        {
            foreach (var site in _siteListRepository.GetSites())
            {
                Execute(site);
            }
        }

        private void Execute(Site site)
        {
            var powerFlow = _apiClient.CurrentPowerflow(site);

            var entry = new[]
            {
                Create("Grid",
                       site,
                       powerFlow.SiteCurrentPowerFlow.Grid,
                       powerFlow.SiteCurrentPowerFlow.Connections.Any(x => x.To.Equals( "Grid", StringComparison.InvariantCultureIgnoreCase))),
                Create("Load",
                       site,
                       powerFlow.SiteCurrentPowerFlow.Load,
                       false),
                Create("Pv",
                       site,
                       powerFlow.SiteCurrentPowerFlow.Pv,
                       false),
                CreateStorage("Storage",
                              site,
                              powerFlow.SiteCurrentPowerFlow.Storage,
                              powerFlow.SiteCurrentPowerFlow.Connections.Any(x => x.From.Equals( "Storage", StringComparison.InvariantCultureIgnoreCase)))
            };
            _influxDbUpload.QueueWrite(entry, 5, _solarEdgeSetting.TargetDatabase, "week_one");
        }

        private InfluxDbEntry Create(string type, Site site, PowerflowData data, bool switchFlowDirection)
        {
            var currentPower = switchFlowDirection ? data.CurrentPower * -1 : data.CurrentPower;
            SendMessage(site.Id, type, "currrentpower", currentPower.ToString());
            SendMessage(site.Id, type, "status", data.Status);

            return new InfluxDbEntry
            {
                Measurement = "PowerFlow",
                Tags = new[]
                          {
                               new InfluxDbEntryField { Name = "Site", Value = site.Id },
                               new InfluxDbEntryField { Name = "SiteName", Value = site.Name },
                               new InfluxDbEntryField { Name = "Type", Value = type }
                           },
                Fields = new[]
                    {
                        new InfluxDbEntryField { Name = "CurrentPower", Value = currentPower },
                        new InfluxDbEntryField { Name = "Status", Value = data.Status }
                    }
            };
        }


        private InfluxDbEntry CreateStorage(string type, Site site, PowerflowDataStorage data, bool switchFlowDirection)
        {
            var currentPower = switchFlowDirection ? data.CurrentPower * -1 : data.CurrentPower;
            SendMessage(site.Id, type, "currrentpower", currentPower.ToString());
            SendMessage(site.Id, type, "chargelevel", data.ChargeLevel.ToString());
            SendMessage(site.Id, type, "status", data.Status);

            return new InfluxDbEntry
            {
                Measurement = "PowerFlow",
                Tags = new[]
                            {
                               new InfluxDbEntryField { Name = "Site", Value = site.Id },
                               new InfluxDbEntryField { Name = "SiteName", Value = site.Name },
                               new InfluxDbEntryField { Name = "Type", Value = type }
                           },
                Fields = new[]
                      {
                        new InfluxDbEntryField { Name = "CurrentPower", Value = currentPower },
                        new InfluxDbEntryField { Name = "ChargeLevel", Value = data.ChargeLevel },
                        new InfluxDbEntryField { Name = "Critical", Value = data.Critical },
                        new InfluxDbEntryField { Name = "Status", Value = data.Status }
                    }
            };
        }

        private void SendMessage(int siteId, string type, string valueName, string value)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"solaredge/state/{siteId}/{type.ToLowerInvariant()}/{valueName.ToLowerInvariant()}")
                .WithPayload(value)
                .Build();
            _managedMqttClient.PublishAsync(message).Wait();
        }
    }
}
