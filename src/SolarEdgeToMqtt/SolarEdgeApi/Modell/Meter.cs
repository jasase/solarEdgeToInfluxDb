using System;
using System.Collections.Generic;
using System.Text;

namespace SolarEdgeToMqtt.SolarEdgeApi.Modell
{
    public class Meter
    {
        public string Type { get; set; }
        public MeterValue[] Values { get; set; }
    }
}
