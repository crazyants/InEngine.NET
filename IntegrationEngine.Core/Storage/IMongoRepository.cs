using IntegrationEngine.Model;

namespace IntegrationEngine.Core.Storage
{
    public interface IMongoRepository : IRepository<IHasStringId, string>
    {
    }
}
