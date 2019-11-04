using Framework.Abstraction.Extension;
using Framework.Abstraction.IocContainer;
using Framework.Abstraction.Plugins;
using Framework.Abstraction.Services;
using Framework.Abstraction.Services.DataAccess.InfluxDb;
using Framework.Abstraction.Services.Scheduling;
using ServiceHost.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

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
            throw new NotImplementedException();
        }
    }
}
