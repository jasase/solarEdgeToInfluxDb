using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet;

namespace SolarEdgeToMqtt.Mqtt
{
    public class MqttClient : IHostedService, IHealthCheck
    {
        private readonly ILogger<MqttClient> _logger;
        private readonly ILoggerFactory _logProvider;
        private readonly IOptions<SolarEdgeSetting> _options;
        private readonly SemaphoreSlim _semaphore;
        private IManagedMqttClient _client;

        public MqttClient(ILogger<MqttClient> logger, ILoggerFactory logProvider, IOptions<SolarEdgeSetting> options)
        {
            _logger = logger;
            _logProvider = logProvider;
            _options = options;
            _semaphore = new SemaphoreSlim(1);
        }

        public IManagedMqttClient Client => _client;

        public bool IsConnected => _client != null &&
                                   _client.IsStarted &&
                                   _client.IsConnected;

        public async Task StartAsync(CancellationToken cancellationToken)
            => await Connect();

        public async Task StopAsync(CancellationToken cancellationToken)
            => await _client.StopAsync();


        private async Task Connect()
        {
            try
            {
                await _semaphore.WaitAsync();

                if (IsConnected)
                {
                    return;
                }
                if (_client != null && _client.IsStarted)
                {
                    return;
                }

                if (_client != null)
                {
                    _client.Dispose();
                }
                _client = null;

                _logger.LogInformation("MQTT Client not connected. Do connect");

                var clientOptions = new MqttClientOptionsBuilder()
                  .WithClientId("DeconzToMqtt")
                  .WithTcpServer(_options.Value.MqttAddress)
                  .WithWillMessage(new MqttApplicationMessageBuilder()
                                       .WithRetainFlag(true)
                                       .WithTopic("tele/deconztomqtt/LWT")
                                       .WithPayload("offline")
                                       .Build());

                if (!string.IsNullOrWhiteSpace(_options.Value.MqttUsername))
                {
                    clientOptions.WithCredentials(_options.Value.MqttUsername, _options.Value.MqttPassword);
                }


                var managedOptions = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .WithClientOptions(clientOptions);

                var factory = new MqttFactory(new MqttNetLogger(_logProvider));
                _client = factory.CreateManagedMqttClient();

                _client.UseDisconnectedHandler(async e =>
                {
                    _logger.LogWarning("Disconnected from MQTT server. Try reconnect...");
                });
                _client.UseConnectedHandler(async e =>
                {
                    _logger.LogInformation("Connected to MQTT server");

                    await _client.PublishAsync(new MqttApplicationMessageBuilder()
                                        .WithRetainFlag(true)
                                        .WithTopic("tele/deconztomqtt/LWT")
                                        .WithPayload("online")
                                        .Build());
                });

                _logger.LogInformation("Connecting to MQTT server '{0}'", _options.Value.MqttAddress);
                await _client.StartAsync(managedOptions.Build());
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(_client != null &&
                           _client.IsStarted &&
                           _client.IsConnected
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy());
    }
}
