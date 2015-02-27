using Common.Logging;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using IQuartzSimpleTrigger = Quartz.ISimpleTrigger;
using IQuartzTrigger = Quartz.ITrigger;
using IQuartzJobDetail = Quartz.IJobDetail;
using QuartzJobKey = Quartz.JobKey;
using QuartzSchedulerException = Quartz.SchedulerException;
using QuartzTriggerKey = Quartz.TriggerKey;
using System;
using System.Reflection;

namespace IntegrationEngine.Scheduler
{
    public class EngineSchedulerListener : IEngineSchedulerListener
    {
        public IRepository<IHasStringId, string> Repository { get; set; }
        public ILog Log { get; set; }

        public EngineSchedulerListener()
        {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void JobAdded(IQuartzJobDetail jobDetail)
        {
        }

        public void JobDeleted(QuartzJobKey jobKey)
        {
        }

        public void JobPaused(QuartzJobKey jobKey)
        {
        }

        public void JobResumed(QuartzJobKey jobKey)
        {
        }

        public void JobScheduled(IQuartzTrigger trigger)
        {
        }

        public void JobUnscheduled(QuartzTriggerKey triggerKey)
        {
        }

        public void JobsPaused(string jobGroup)
        {
        }

        public void JobsResumed(string jobGroup)
        {
        }

        public void SchedulerError(string msg, QuartzSchedulerException cause)
        {
        }

        public void SchedulerInStandbyMode()
        {
        }

        public void SchedulerShutdown()
        {
        }

        public void SchedulerShuttingdown()
        {
        }

        public void SchedulerStarted()
        {
        }

        public void SchedulerStarting()
        {
        }

        public void SchedulingDataCleared()
        {
        }

        public void TriggerFinalized(IQuartzTrigger trigger)
        {
            try
            {
                if (trigger is IQuartzSimpleTrigger)
                    Repository.Delete<SimpleTrigger>(trigger.Key.Name);       
            }
            catch (Exception exception)
            {
                Log.Error("Error deleting simple trigger.", exception);
            }
        }

        public void TriggerPaused(QuartzTriggerKey triggerKey)
        {
        }

        public void TriggerResumed(QuartzTriggerKey triggerKey)
        {
        }

        public void TriggersPaused(string triggerGroup)
        {
        }

        public void TriggersResumed(string triggerGroup)
        {
        }
    }
}
