using IntegrationEngine.Core.IntegrationPoint;
using MongoDB.Bson;
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
    public class MongoClientAdapter : MongoClient, IMongoClient, IIntegrationPoint<IMongoConfiguration>
    {
        public MongoClientAdapter() 
        {}

        public MongoClientAdapter(MongoConfiguration mongoDBConfiguration)
                    : base()
        {
        }
    }
}
