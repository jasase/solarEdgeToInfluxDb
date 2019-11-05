using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Abstraction.Extension;
using SolarEdgeToInfluxDb.SolarEdgeApi;
using SolarEdgeToInfluxDb.SolarEdgeApi.Modell;

namespace SolarEdgeToInfluxDb.Repositories
{
    public class SiteListRepository
    {
        private readonly SolarEdgeApiClient _apiClient;
        private readonly ILogger _logger;
        private readonly object _refreshLock;
        private DateTime _lastRefresh;
        private Site[] _sites;

        public SiteListRepository(SolarEdgeApiClient apiClient, ILogger logger)
        {
            _refreshLock = new object();
            _apiClient = apiClient;
            _logger = logger;
            _lastRefresh = DateTime.MinValue;
            _sites = Array.Empty<Site>();
        }

        public IEnumerable<Site> GetSites()
        {
            if (_lastRefresh < DateTime.Now.AddHours(-24))
            {
                _logger.Debug("Refresh time reached. Query site list from API");
                RefreshSites();
            }

            return _sites;
        }

        public void RefreshSites()
        {
            lock (_refreshLock)
            {
                if (_lastRefresh > DateTime.Now.AddHours(-24))
                {
                    return;
                }

                _logger.Debug("Refresh site list from API");
                try
                {
                    _sites = _apiClient.ListSites();
                    _lastRefresh = DateTime.Now;

                    _logger.Debug("Site List: {0}{1}",
                        Environment.NewLine,
                        string.Join(Environment.NewLine, _sites.Select(x => x.Id + " | " + x.Name)));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Refreshing of site list not possible. Delay next refresh in 5 Minutes");
                    _sites = Array.Empty<Site>();
                    _lastRefresh = DateTime.Now.AddHours(-24).AddMinutes(5);
                }
            }
        }
    }
}
