using IntegrationEngine.Core.IntegrationPoint;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationEngine.Core.Configuration;

namespace IntegrationEngine.Core.MongoDB
{
    public class MongoClientAdapter : MongoClient, IMongoDBClient, IIntegrationPoint<IMongoConfiguration>
    {
        public MongoClientAdapter() 
        {}

        public MongoClientAdapter(MongoConfiguration mongoDBConfiguration)
                    : base()
        {
        }
    }
}
