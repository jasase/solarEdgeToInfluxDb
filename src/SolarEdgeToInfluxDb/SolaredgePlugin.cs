using System;
using Framework.Abstraction.Extension;
using Framework.Abstraction.IocContainer;
using Framework.Abstraction.Plugins;
using Framework.Abstraction.Services;
using Framework.Abstraction.Services.DataAccess.InfluxDb;
using Framework.Abstraction.Services.Scheduling;
using Framework.Core.Scheduling;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using ServiceHost.Contracts;
using SolarEdgeToInfluxDb.Repositories;
using SolarEdgeToInfluxDb.SolarEdgeApi;

namespace SolarEdgeToInfluxDb
{
    public class SolaredgePlugin : Framework.Abstraction.Plugins.Plugin, IServicePlugin
    {

        public SolaredgePlugin(IDependencyResolver resolver,
                               IDependencyResolverConfigurator configurator,
                               IEventService eventService,
                               ILogger logger)
            : base(resolver, configurator, eventService, logger)
        {
            Description = new AutostartServicePluginDescription()
            {
                Description = "Plugin um regelmäßig Daten von SolarEdge abzurufen",
                Name = "SolarEdge",
                NeededServices = new[] { typeof(IConfiguration), typeof(ISchedulingService), typeof(IInfluxDbUpload) }
            };
        }


        public override PluginDescription Description { get; }

        protected override void ActivateInternal()
        {
            SetupDatabase();
            SetupMqttClient();

            ConfigurationResolver.AddRegistration(new SingletonRegistration<SolarEdgeApiClient, SolarEdgeApiClient>());
            ConfigurationResolver.AddRegistration(new SingletonRegistration<SiteListRepository, SiteListRepository>());

            var historyJob = Resolver.CreateConcreteInstanceWithDependencies<SolarEdgeHistoryJob>();
            var powerFlowJob = Resolver.CreateConcreteInstanceWithDependencies<SolaredgePowerFlowJob>();

            var scheduler = Resolver.GetInstance<ISchedulingService>();
            scheduler.AddJob(historyJob, new PollingPlan(TimeSpan.FromHours(1)));
            scheduler.AddJob(powerFlowJob, new PollingPlan(TimeSpan.FromSeconds(5)));
        }

        private void SetupDatabase()
        {
            var solarEdgeSetting = Resolver.CreateConcreteInstanceWithDependencies<SolarEdgeSetting>();
            var influxManagement = Resolver.CreateConcreteInstanceWithDependencies<IInfluxDbManagement>();

            influxManagement.EnsureDatabase(solarEdgeSetting.TargetDatabase);

            influxManagement.EnsureRetentionPolicy(solarEdgeSetting.TargetDatabase, new InfluxDbRetentionPolicyDefinition("week_one", TimeSpan.FromDays(7), false));
            //influxManagement.EnsureRetentionPolicy(solarEdgeSetting.TargetDatabase, new InfluxDbRetentionPolicyDefinition("infinite", TimeSpan.MinValue, true));
        }

        private void SetupMqttClient()
        {
            var solarEdgeSetting = Resolver.CreateConcreteInstanceWithDependencies<SolarEdgeSetting>();


            var lwtMessage = new MqttApplicationMessageBuilder()
                                    .WithRetainFlag(true)
                                    .WithTopic("tele/solaredge/LWT")
                                    .WithPayload("offline")
                                    .Build();
            var clientOptions = new MqttClientOptionsBuilder().WithClientId("SolarEdge")
                                                              .WithTcpServer(solarEdgeSetting.MqttAddress)
                                                              .WithWillMessage(lwtMessage);

            if (!string.IsNullOrWhiteSpace(solarEdgeSetting.MqttUsername))
            {
                clientOptions.WithCredentials(solarEdgeSetting.MqttUsername, solarEdgeSetting.MqttPassword);
            }

            var options = new ManagedMqttClientOptionsBuilder().WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                                                               .WithClientOptions(clientOptions.Build())
                                                               .Build();

            var mqttClient = new MqttFactory().CreateManagedMqttClient();
            mqttClient.StartAsync(options).Wait();

            ConfigurationResolver.AddRegistration(new SingletonRegistration<IManagedMqttClient>(mqttClient));
        }
    }
}
