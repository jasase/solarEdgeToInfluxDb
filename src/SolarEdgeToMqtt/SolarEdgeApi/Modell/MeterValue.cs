using System;
using System.Collections.Generic;
using System.Text;

namespace SolarEdgeToMqtt.SolarEdgeApi.Modell
{
    public class MeterValue
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }
}
