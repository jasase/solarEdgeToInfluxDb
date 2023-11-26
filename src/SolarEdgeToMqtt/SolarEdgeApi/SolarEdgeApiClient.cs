using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SolarEdgeToMqtt.SolarEdgeApi.Modell;

namespace SolarEdgeToMqtt.SolarEdgeApi
{
    public class SolarEdgeApiClient
    {
        private readonly Uri _baseUri;
        private readonly SolarEdgeSetting _setting;

        public SolarEdgeApiClient(IOptions<SolarEdgeSetting> setting)
        {
            if (setting is null)
            {
                throw new ArgumentNullException(nameof(setting));
            }

            _baseUri = new Uri("https://monitoringapi.solaredge.com/");
            _setting = setting.Value;
        }

        public async Task<Site[]> ListSites()
        {
            var data = await Request<SiteListResult>("sites/list");
            return data.Sites.Site;
        }

        public async Task<EnergyDetailsResult> EnergyDetails(Site site, DateTime start, DateTime end)
        {
            var data = await Request<EnergyDetailsResult>($"site/{site.Id}/energyDetails?timeUnit=QUARTER_OF_AN_HOUR&startTime={ConvertToString(start)}&endTime={ConvertToString(end)}");
            return data;
        }

        public async Task<PowerDetailsResult> PowerDetails(Site site, DateTime start, DateTime end)
        {
            var data = await Request<PowerDetailsResult>($"site/{site.Id}/powerDetails?timeUnit=QUARTER_OF_AN_HOUR&startTime={ConvertToString(start)}&endTime={ConvertToString(end)}");
            return data;
        }

        public async Task<StorageDataResult> StorageData(Site site, DateTime start, DateTime end)
        {
            var data = await Request<StorageDataResult>($"site/{site.Id}/storageData?startTime={ConvertToString(start)}&endTime={ConvertToString(end)}");
            return data;
        }

        public async Task<CurrentPowerflowResult> CurrentPowerflow(Site site)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                                                                                           System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_setting.Username}:{_setting.Password}")));

                var result = await client.GetAsync(new Uri($"https://monitoring.solaredge.com/solaredge-apigw/api/site/{site.Id}/currentPowerFlow.json"));
                var strContent = await result.Content.ReadAsStringAsync();

                return Convert<CurrentPowerflowResult>(strContent);
            }
        }

        private string ConvertToString(DateTime time)
            => time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        private async Task<TData> Request<TData>(string relativePath)
        {
            using (var client = new HttpClient())
            {
                var uri = new UriBuilder(new Uri(_baseUri, relativePath));

                if (string.IsNullOrEmpty(uri.Query))
                {
                    uri.Query = "?api_key=" + _setting.ApiKey;
                }
                else
                {
                    uri.Query += "&api_key=" + _setting.ApiKey;
                }
                var result = await client.GetAsync(uri.Uri);
                var strContent = await result.Content.ReadAsStringAsync();
                return Convert<TData>(strContent);
            }
        }

        private TData Convert<TData>(string content)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            options.Converters.Add(new DateTimeConverter());
            return JsonSerializer.Deserialize<TData>(content, options);
        }


    }
}
