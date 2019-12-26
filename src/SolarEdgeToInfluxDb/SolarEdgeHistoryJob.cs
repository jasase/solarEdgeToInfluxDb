using Framework.Abstraction.Services.DataAccess.InfluxDb;
using Framework.Abstraction.Services.Scheduling;
using SolarEdgeToInfluxDb.Repositories;
using SolarEdgeToInfluxDb.SolarEdgeApi;
using SolarEdgeToInfluxDb.SolarEdgeApi.Modell;
using System;
using System.Linq;
using System.Collections.Generic;

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

            var storageData = ProcessStorageData(site, from, now);
            _influxDbUpload.QueueWrite(storageData.ToArray(), 5, _solarEdgeSetting.TargetDatabase);

            _lastRequest = now;
        }

        private IEnumerable<InfluxDbEntry> ProcessStorageData(Site site, DateTime start, DateTime end)
        {
            var data = _apiClient.StorageData(site, start, end);
            return from b in data.StorageData.Batteries
                   from t in b.Telemetries
                   select new InfluxDbEntry
                   {
                       Measurement = "Storage",
                       Time = TimeZoneInfo.ConvertTimeToUtc(t.TimeStamp, site.Location.TimeZoneInfo),
                       Fields = new[]
                       {
                            new InfluxDbEntryField { Name = "Power", Value = t.Power },
                            new InfluxDbEntryField { Name = "BatteryPercentage", Value = t.BatteryPercentageState },
                            new InfluxDbEntryField { Name = "State", Value = t.BatteryState },
                            new InfluxDbEntryField { Name = "LifeTimeEnergyCharged", Value = t.LifeTimeEnergyCharged },
                            new InfluxDbEntryField { Name = "LifeTimeEnergyDischarged", Value = t.LifeTimeEnergyDischarged },
                            new InfluxDbEntryField { Name = "FullPackEnergyAvailable", Value = t.FullPackEnergyAvailable },
                            new InfluxDbEntryField { Name = "InternalTemp", Value = t.InternalTemp },
                            new InfluxDbEntryField { Name = "ACGridCharging", Value = t.ACGridCharging }
                       },
                       Tags = new[]
                       {
                           new InfluxDbEntryField { Name = "Site", Value = site.Id },
                           new InfluxDbEntryField { Name = "SiteName", Value = site.Name },
                           new InfluxDbEntryField { Name = "SerialNumber", Value = b.SerialNumber },
                       }
                   };

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
                    Time = TimeZoneInfo.ConvertTimeToUtc(dateGroup.Key, site.Location.TimeZoneInfo),
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

