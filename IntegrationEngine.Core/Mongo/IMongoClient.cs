using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Mongo
{
    public interface IMongoClient
    {
        MongoDatabase GetDatabase(string name, MongoDatabaseSettings settings = null);
    }
}
