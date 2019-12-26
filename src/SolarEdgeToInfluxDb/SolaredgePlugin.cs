using Framework.Abstraction.Extension;
using Framework.Abstraction.IocContainer;
using Framework.Abstraction.Plugins;
using Framework.Abstraction.Services;
using Framework.Abstraction.Services.DataAccess.InfluxDb;
using Framework.Abstraction.Services.Scheduling;
using Framework.Core.Scheduling;
using ServiceHost.Contracts;
using SolarEdgeToInfluxDb.Repositories;
using SolarEdgeToInfluxDb.SolarEdgeApi;
using System;

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

            ConfigurationResolver.AddRegistration(new SingletonRegistration<SolarEdgeApiClient, SolarEdgeApiClient>());
            ConfigurationResolver.AddRegistration(new SingletonRegistration<SiteListRepository, SiteListRepository>());

            var historyJob = Resolver.CreateConcreteInstanceWithDependencies<SolarEdgeHistoryJob>();
            var powerFlowJob = Resolver.CreateConcreteInstanceWithDependencies<SolaredgePowerFlowJob>();

            var scheduler = Resolver.GetInstance<ISchedulingService>();
            scheduler.AddJob(historyJob, new PollingPlan(TimeSpan.FromHours(1.5)));
            scheduler.AddJob(powerFlowJob, new PollingPlan(TimeSpan.FromSeconds(15)));
        }

        private void SetupDatabase()
        {
            var solarEdgeSetting = Resolver.CreateConcreteInstanceWithDependencies<SolarEdgeSetting>();
            var influxManagement = Resolver.CreateConcreteInstanceWithDependencies<IInfluxDbManagement>();

            influxManagement.EnsureDatabase(solarEdgeSetting.TargetDatabase);

            influxManagement.EnsureRetentionPolicy(solarEdgeSetting.TargetDatabase, new InfluxDbRetentionPolicyDefinition("week_one", TimeSpan.FromDays(7), false));
            //influxManagement.EnsureRetentionPolicy(solarEdgeSetting.TargetDatabase, new InfluxDbRetentionPolicyDefinition("infinite", TimeSpan.MinValue, true));
        }
    }
}
