using IntegrationEngine.Core.IntegrationPoint;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.Mongo;

namespace IntegrationEngine.Core.Mongo
{
    public class MongoClientAdapter : IMongoClient, IIntegrationPoint<IMongoConfiguration>
    {
        public MongoClient MongoClient { get; set; }

        public MongoClientAdapter() 
        {
        }

        public MongoClientAdapter(MongoConfiguration mongoConfiguration)
        {
            MongoClient = new MongoClient("mongodb://" + mongoConfiguration.HostName);
        }

        public MongoDatabase GetDatabase(string name, MongoDatabaseSettings settings = null)
        {
            return MongoClient.GetServer().GetDatabase(name);
        }
    }
}
