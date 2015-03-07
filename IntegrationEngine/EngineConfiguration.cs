using IntegrationEngine.Core.Configuration;
using FX.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;

namespace IntegrationEngine
{
    public class EngineConfiguration : JsonConfiguration, IEngineConfiguration
    {
        public int JobProcessorCount { get; set; }
        public string JobProcessorMessageQueueName { get; set; }
        public string JobTriggerRepositoryName { get; set; }
        public WebApiConfiguration WebApi { get; set; }
        public NLogAdapterConfiguration NLogAdapter { get; set; }
        public IntegrationPointConfigurations IntegrationPoints { get; set; }

        public EngineConfiguration()
            : base("IntegrationEngine.json")
        { }
    }
}
