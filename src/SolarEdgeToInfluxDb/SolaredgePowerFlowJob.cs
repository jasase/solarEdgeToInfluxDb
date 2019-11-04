using Framework.Abstraction.Services.Scheduling;
using System;
using System.Collections.Generic;
using System.Text;

namespace SolarEdgeToInfluxDb
{
    public class SolaredgePowerFlowJob : IJob
    {
        public string Name => "SolarEdge-PowerFlow";

        public void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
