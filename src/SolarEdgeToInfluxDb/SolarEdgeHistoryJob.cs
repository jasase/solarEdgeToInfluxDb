using Framework.Abstraction.Services.DataAccess.InfluxDb;
using Framework.Abstraction.Services.Scheduling;
using SolarEdgeToInfluxDb.Repositories;
using SolarEdgeToInfluxDb.SolarEdgeApi;
using SolarEdgeToInfluxDb.SolarEdgeApi.Modell;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace SolarEdgeToInfluxDb
{
    public class SolarEdgeHistoryJob : IJob
    {
        private readonly SolarEdgeApiClient _apiClient;
        private readonly SiteListRepository _siteListRepository;
        private readonly IInfluxDbUpload _influxDbUpload;
        private readonly SolarEdgeSetting _solarEdgeSetting;
        private DateTime _lastRequest;

        public string Name => "SolarEdge-History";

        public SolarEdgeHistoryJob(SolarEdgeApiClient apiClient,
                                   SiteListRepository siteListRepository,
                                   IInfluxDbUpload influxDbUpload,
                                   SolarEdgeSetting solarEdgeSetting)
        {
            _lastRequest = DateTime.MinValue;
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
            var now = DateTime.Now;
            var nowMinus1Day = now.AddDays(-1);
            var from = nowMinus1Day > _lastRequest ? nowMinus1Day : _lastRequest;

            var energyData = ProcessEnergyDetails(site, from, now);
            _influxDbUpload.QueueWrite(energyData.ToArray(), 5, _solarEdgeSetting.TargetDatabase);

            var powerData = ProcessPowerDetails(site, from, now);
            _influxDbUpload.QueueWrite(powerData.ToArray(), 5, _solarEdgeSetting.TargetDatabase);

            _lastRequest = now;
        }

        private IEnumerable<InfluxDbEntry> ProcessEnergyDetails(Site site, DateTime start, DateTime end)
        {
            var data = _apiClient.EnergyDetails(site, start, end);
            return ProcessMeterList(site, data.EnergyDetails, "Energy");
        }

        private IEnumerable<InfluxDbEntry> ProcessPowerDetails(Site site, DateTime start, DateTime end)
        {
            var data = _apiClient.PowerDetails(site, start, end);
            return ProcessMeterList(site, data.PowerDetails, "Power");
        }

        private IEnumerable<InfluxDbEntry> ProcessMeterList(Site site, MeterList meterList, string measurement)
        {
            foreach (var dateGroup in from m in meterList.Meters
                                      from v in m.Values
                                      group new { m.Type, v.Value } by v.Date into groupByDate
                                      select groupByDate)
            {

                var fields = dateGroup.ToLookup(x => x.Type);

                yield return new InfluxDbEntry
                {
                    Time = dateGroup.Key,
                    Fields = fields.Select(x => new InfluxDbEntryField
                    {
                        Name = x.Key,
                        Value = x.FirstOrDefault().Value
                    }).ToArray(),
                    Tags = new[]
                    {
                        new InfluxDbEntryField
                        {
                            Name = "Site",
                            Value = site.Id
                        },
                        new InfluxDbEntryField
                        {
                            Name = "SiteName",
                            Value = site.Name
                        },
                        new InfluxDbEntryField
                        {
                            Name = "Unit",
                            Value = meterList.Unit
                        },
                        new InfluxDbEntryField
                        {
                            Name = "TimeUnit",
                            Value = meterList.TimeUnit
                        },
                    },
                    Measurement = measurement
                };
            }
        }
    }
}
