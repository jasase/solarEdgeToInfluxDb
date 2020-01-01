using Framework.Abstraction.Services.DataAccess.InfluxDb;
using Framework.Abstraction.Services.Scheduling;
using SolarEdgeToInfluxDb.Repositories;
using SolarEdgeToInfluxDb.SolarEdgeApi;
using SolarEdgeToInfluxDb.SolarEdgeApi.Modell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarEdgeToInfluxDb
{
    public class SolaredgePowerFlowJob : IJob
    {
        private readonly SolarEdgeApiClient _apiClient;
        private readonly SiteListRepository _siteListRepository;
        private readonly IInfluxDbUpload _influxDbUpload;
        private readonly SolarEdgeSetting _solarEdgeSetting;

        public string Name => "SolarEdge-PowerFlow";

        public SolaredgePowerFlowJob(SolarEdgeApiClient apiClient,
                                     SiteListRepository siteListRepository,
                                     IInfluxDbUpload influxDbUpload,
                                     SolarEdgeSetting solarEdgeSetting)
        {
            _apiClient = apiClient;
            _siteListRepository = siteListRepository;
            _influxDbUpload = influxDbUpload;
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
            => new InfluxDbEntry
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
                        new InfluxDbEntryField { Name = "CurrentPower", Value = switchFlowDirection ? data.CurrentPower * -1 : data.CurrentPower },
                        new InfluxDbEntryField { Name = "Status", Value = data.Status }
                    }
            };

        private InfluxDbEntry CreateStorage(string type, Site site, PowerflowDataStorage data, bool switchFlowDirection)
            => new InfluxDbEntry
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
                        new InfluxDbEntryField { Name = "CurrentPower", Value = switchFlowDirection ? data.CurrentPower * -1 : data.CurrentPower },
                        new InfluxDbEntryField { Name = "ChargeLevel", Value = data.ChargeLevel },
                        new InfluxDbEntryField { Name = "Critical", Value = data.Critical },
                        new InfluxDbEntryField { Name = "Status", Value = data.Status }
                    }
            };
    }
}
