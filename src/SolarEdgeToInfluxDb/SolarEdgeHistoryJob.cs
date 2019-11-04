using Framework.Abstraction.Services.Scheduling;
using System;
using System.Collections.Generic;
using System.Text;

namespace SolarEdgeToInfluxDb
{
    public class SolarEdgeHistoryJob : IJob
    {
        public string Name => "SolarEdge-History";

        public void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
