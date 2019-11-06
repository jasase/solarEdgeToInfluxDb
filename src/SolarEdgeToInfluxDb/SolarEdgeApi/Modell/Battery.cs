using System;
using System.Collections.Generic;
using System.Text;

namespace SolarEdgeToInfluxDb.SolarEdgeApi.Modell
{
    public class Battery
    {
        public double Nameplate { get; set; }
        public string SerialNumber { get; set; }
        public string ModelNumber { get; set; }
        public int TelemetryCount { get; set; }
        public BatteryTelemetry[] Telemetries { get; set; }
    }
}
