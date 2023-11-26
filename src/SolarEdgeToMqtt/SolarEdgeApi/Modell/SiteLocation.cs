using System;

namespace SolarEdgeToMqtt.SolarEdgeApi.Modell
{
    public class SiteLocation
    {
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string Zip { get; set; }
        public string TimeZone { get; set; }
        public TimeZoneInfo TimeZoneInfo { get; set; }
    }
}
