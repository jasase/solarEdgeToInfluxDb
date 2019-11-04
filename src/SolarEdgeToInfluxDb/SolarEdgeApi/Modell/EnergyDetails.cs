using System;
using System.Collections.Generic;
using System.Text;

namespace SolarEdgeToInfluxDb.SolarEdgeApi.Modell
{
    public class EnergyDetails
    {
        public string TimeUnit { get; set; }
        public string Unit { get; set; }
        public Meter[] Meters { get; set; }
    }
}
