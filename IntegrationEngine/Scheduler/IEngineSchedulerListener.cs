using Common.Logging;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using Quartz;

namespace IntegrationEngine.Scheduler
{
    public interface IEngineSchedulerListener : ISchedulerListener
    {
        IRepository<IHasStringId, string> Repository { get; set; }
        ILog Log { get; set; }
    }
}
