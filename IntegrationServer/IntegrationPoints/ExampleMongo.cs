using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationServer.IntegrationPoints
{
    public class ExampleMongo : MongoClientAdapter
    {
        public ExampleMongo(MongoConfiguration mongoConfiguration)
            : base(mongoConfiguration)
        {
        }
    }
}
