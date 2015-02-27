using System.Linq;

namespace IntegrationEngine.Core.Configuration
{
    public class MongoConfiguration : IMongoConfiguration
    {
        public string IntegrationPointName { get; set; }
        public string HostName { get; set; }

        public MongoConfiguration()
        {}

        public MongoConfiguration(IEngineConfiguration engineConfiguration, string integrationPointName)
            : this()
        {
            var config = engineConfiguration.IntegrationPoints.Mongo.Single(x => x.IntegrationPointName == integrationPointName);
            IntegrationPointName = integrationPointName;
            HostName = config.HostName;
        }
    }
}
