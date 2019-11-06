using System;

namespace SolarEdgeToInfluxDb.SolarEdgeApi.Modell
{
    public class BatteryTelemetry
    {
        public DateTime TimeStamp { get; set; }
        public double Power { get; set; }
        public int BatteryState { get; set; }
        public int LifeTimeEnergyDischarged { get; set; }
        public int LifeTimeEnergyCharged { get; set; }
        public double BatteryPercentageState { get; set; }
        public double FullPackEnergyAvailable { get; set; }
        public double InternalTemp { get; set; }
        public double ACGridCharging { get; set; }
    }
}
