using System;
using System.Collections.Generic;
using System.Text;

namespace SolarEdgeToMqtt.SolarEdgeApi.Modell
{
    public class Powerflow
    {
        public int UpdateRefreshRate { get; set; }
        public string Unit { get; set; }
        public PowerflowConnection[] Connections { get; set; }
        public PowerflowData Grid { get; set; }
        public PowerflowData Load { get; set; }
        public PowerflowData Pv { get; set; }
        public PowerflowDataStorage Storage { get; set; }
    }

    public class PowerflowConnection
    {
        public string From { get; set; }
        public string To { get; set; }
    }

    public class PowerflowData
    {
        public string Status { get; set; }
        public double CurrentPower { get; set; }
    }

    public class PowerflowDataStorage  : PowerflowData
    {
        public double? ChargeLevel { get; set; }
        public bool Critical { get; set; }
    }
}
