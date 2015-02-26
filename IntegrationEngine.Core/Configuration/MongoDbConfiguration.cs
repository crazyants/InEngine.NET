using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Configuration
{
    public class MongoDBConfiguration : IMongoDBConfiguration
    {
        public string IntegrationPointName { get; set; }
        public string HostName { get; set; }

        public MongoDBConfiguration()
        {}

        public MongoDBConfiguration(IEngineConfiguration engineConfiguration, string integrationPointName)
            : this()
        {
            var config = engineConfiguration.IntegrationPoints.MongoDB.Single(x => x.IntegrationPointName == integrationPointName);
            IntegrationPointName = integrationPointName;
            HostName = config.HostName;
        }
    }
}
