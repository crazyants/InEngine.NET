using Common.Logging;
using IntegrationEngine.Core.Mongo;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Storage
{
    public class MongoRepository : IMongoRepository
    {
        public IMongoClient MongoClient { get; set; }
        public string DatabaseName { get; set; }

        public ILog Log { get; set; }

        public MongoRepository()
        {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public MongoRepository(IMongoClient mongoClient)
            : this()
        {
            MongoClient = mongoClient;
        }

        public IEnumerable<TItem> SelectAll<TItem>() where TItem : class, Model.IHasStringId
        {
            return MongoClient.GetDatabase("IntegrationEngine")
                .GetCollection<TItem>(typeof(TItem).Name)
                .AsQueryable<TItem>()
                .AsEnumerable<TItem>();
        }

        public TItem SelectById<TItem>(string id) where TItem : class, Model.IHasStringId
        {
            throw new NotImplementedException();
        }

        public TItem Insert<TItem>(TItem item) where TItem : class, Model.IHasStringId
        {
            throw new NotImplementedException();
        }

        public TItem Update<TItem>(TItem item) where TItem : class, Model.IHasStringId
        {
            throw new NotImplementedException();
        }

        public void Delete<TItem>(string id) where TItem : class
        {
            throw new NotImplementedException();
        }

        public bool Exists<TItem>(string id) where TItem : class
        {
            throw new NotImplementedException();
        }

        public bool IsServerAvailable()
        {
            throw new NotImplementedException();
        }
    }
}
