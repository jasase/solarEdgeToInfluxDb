using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using SolarEdgeToInfluxDb.SolarEdgeApi.Modell;

namespace SolarEdgeToInfluxDb.SolarEdgeApi
{
    public class SolarEdgeApiClient
    {
        private readonly string _apiKey;
        private readonly Uri _baseUri;

        public SolarEdgeApiClient(string apiKey)
        {
            _apiKey = apiKey;
            _baseUri = new Uri("https://monitoringapi.solaredge.com/");
        }

        public Site[] ListSites()
        {
            var data = Request<SiteListResult>("sites/list");
            return data.Sites.Site;
        }

        private TData Request<TData>(string relativePath)
        {
            using (var client = new HttpClient())
            {
                var uri = new UriBuilder(new Uri(_baseUri, relativePath));

                if (string.IsNullOrEmpty(uri.Query))
                {
                    uri.Query = "?api_key=" + _apiKey;
                }
                else
                {
                    uri.Query += "&api_key=" + _apiKey;
                }
                var result = client.GetAsync(uri.Uri).Result;
                var strContent = result.Content.ReadAsStringAsync().Result;
                return JsonSerializer.Deserialize<TData>(strContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
    }
}
