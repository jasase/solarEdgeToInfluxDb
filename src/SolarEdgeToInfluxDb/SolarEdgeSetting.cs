using Framework.Abstraction.Extension;

namespace SolarEdgeToInfluxDb
{
    public class SolarEdgeSetting : ISetting
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool EnablePowerflow { get; set; }
        public string ApiKey { get; set; }

        public string TargetDatabase { get; set; }


        public string MqttAddress { get; set; }
        public string MqttUsername { get; set; }
        public string MqttPassword { get; set; }

        public SolarEdgeSetting()
        {
            EnablePowerflow = true;
            Username = string.Empty;
            Password = string.Empty;
            ApiKey = string.Empty;
            TargetDatabase = "SolarEdge";

            MqttAddress = "";
            MqttUsername = "";
            MqttPassword = "";
        }
    }
}
