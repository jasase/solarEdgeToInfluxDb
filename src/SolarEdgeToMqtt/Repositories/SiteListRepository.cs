using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SolarEdgeToMqtt.SolarEdgeApi.Modell;
using SolarEdgeToMqtt.SolarEdgeApi;
using System.Threading.Tasks;

namespace SolarEdgeToMqtt.Repositories
{
    public class SiteListRepository
    {
        private readonly SolarEdgeApiClient _apiClient;
        private readonly ILogger<SiteListRepository> _logger;
        private readonly object _refreshLock;
        private DateTime _lastRefresh;
        private Site[] _sites;

        public SiteListRepository(SolarEdgeApiClient apiClient, ILogger<SiteListRepository> logger)
        {
            _refreshLock = new object();
            _apiClient = apiClient;
            _logger = logger;
            _lastRefresh = DateTime.MinValue;
            _sites = Array.Empty<Site>();
        }

        public async Task<Site[]> GetSites()
        {
            if (_lastRefresh < DateTime.Now.AddHours(-24))
            {
                _logger.LogDebug("Refresh time reached. Query site list from API");
                await RefreshSites();
            }

            return _sites;
        }

        public async Task RefreshSites()
        {
            if (_lastRefresh > DateTime.Now.AddHours(-24))
            {
                return;
            }
            var sites = await _apiClient.ListSites();

            lock (_refreshLock)
            {

                if (_lastRefresh > DateTime.Now.AddHours(-24))
                {
                    return;
                }
                _logger.LogDebug("Refresh site list from API");
                try
                {
                    _sites = sites;
                    foreach (var site in _sites)
                    {
                        SetTimezoneInformation(site);
                    }

                    _lastRefresh = DateTime.Now;

                    const string FORMAT = "{0,15} | {1,50} | {2,30} | {3,30}";
                    _logger.LogDebug("Site List: {0}{1}",
                        Environment.NewLine,
                        string.Join(Environment.NewLine, _sites.Select(x => string.Format(FORMAT, x.Id, x.Name, x.Location.TimeZone, x.Location.TimeZoneInfo.DisplayName))));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Refreshing of site list not possible. Delay next refresh in 5 Minutes");
                    _sites = Array.Empty<Site>();
                    _lastRefresh = DateTime.Now.AddHours(-24).AddMinutes(5);
                }
            }
        }

        private void SetTimezoneInformation(Site site)
        {
            if (site.Location == null)
            {
                site.Location = new SiteLocation();
            }

            var timeZone = TimeZoneInfo.GetSystemTimeZones()
                                       .FirstOrDefault(x => x.Id
                                                             .Equals(site.Location.TimeZone, StringComparison.InvariantCultureIgnoreCase));
            if (timeZone != null)
            {
                site.Location.TimeZoneInfo = timeZone;
            }
            else
            {
                site.Location.TimeZoneInfo = TimeZoneInfo.Local;
            }
        }
    }
}
