﻿using System;
using System.Collections.Generic;
using System.Globalization;
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

        public void PowerDetails(Site site)
        {

        }

        public EnergyDetailsResult EnergyDetails(Site site, DateTime start, DateTime end)
        {
            var data = Request<EnergyDetailsResult>($"site/{site.Id}/energyDetails?timeUnit=QUARTER_OF_AN_HOUR&startTime={ConvertToString(start)}&endTime={ConvertToString(end)}");
            return data;
        }

        private string ConvertToString(DateTime time)
            => time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

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
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                options.Converters.Add(new DateTimeConverter());
                return JsonSerializer.Deserialize<TData>(strContent, options);
            }
        }
    }
}