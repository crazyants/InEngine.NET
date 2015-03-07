using IntegrationEngine.Core.Configuration;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Configuration
{
    public interface IEngineConfiguration
    {
        int JobProcessorCount { get; set; }
        string JobProcessorMessageQueueName { get; set; }
        string JobTriggerRepositoryName { get; set; }
        WebApiConfiguration WebApi { get; set; }
        NLogAdapterConfiguration NLogAdapter { get; set; }
        IntegrationPointConfigurations IntegrationPoints { get; set; }
    }
}
