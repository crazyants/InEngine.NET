﻿using IntegrationEngine.Api;
using IntegrationEngine.Configuration;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.R;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.MessageQueue;
using IntegrationEngine.Model;
using log4net;
using Nest;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IntegrationEngine
{
    public class EngineHostConfiguration
    {
        public EngineConfiguration Configuration { get; set; }
        public IList<Type> IntegrationJobTypes { get; set; }

        public EngineHostConfiguration()
        {
        }

        public void Configure(IList<Assembly> assembliesWithJobs)
        {
            IntegrationJobTypes = assembliesWithJobs
                        .SelectMany(x => x.GetTypes())
                        .Where(x => typeof(IIntegrationJob).IsAssignableFrom(x) && x.IsClass)
                        .ToList();
            TryAndLogFailure("Loading Configuration", LoadConfiguration);
            TryAndLogFailure("Setup Logging", SetupLogging);
            TryAndLogFailure("Setup Database Repository", SetupDatabaseRepository);
            TryAndLogFailure("Setup Mail Client", SetupMailClient);
            TryAndLogFailure("Setup Elastic Client", SetupElasticClient);
            TryAndLogFailure("Setup RScript Runner", SetupRScriptRunner);
            TryAndLogFailure("Setup Message Queue Client", SetupMessageQueueClient);
            TryAndLogFailure("Setup Scheduler", SetupScheduler);
            TryAndLogFailure("Setup Web Api", SetupApi);
            TryAndLogFailure("Setup Message Queue Listener", SetupMessageQueueListener);
        }

        static void TryAndLogFailure(string description, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                var log = LogManager.GetLogger(typeof(EngineHost));
                log.Error(description, ex);
            }
        }

        public void LoadConfiguration()
        {
            Configuration = new EngineConfiguration();
            Container.Register<EngineConfiguration>(Configuration);
        }

        public void SetupLogging()
        {
            var log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            Container.Register<ILog>(log);
        }

        public void SetupDatabaseRepository()
        {
            var dbContext = new DatabaseInitializer(Configuration.Database).GetDbContext();
            Container.Register<IntegrationEngineContext>(dbContext);
        }

        public void SetupApi()
        {
            var config = Configuration.WebApi;
            IntegrationEngineApi.Start((new UriBuilder("http", config.HostName, config.Port)).Uri.AbsoluteUri);
        }

        public void SetupMailClient()
        {
            var mailClient = new MailClient() {
                MailConfiguration = Configuration.Mail,
                Log = Container.Resolve<ILog>(),
            };
            Container.Register<IMailClient>(mailClient);
        }

        public void SetupMessageQueueListener()
        {
            var rabbitMqListener = new RabbitMqListener() {
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
            };
            rabbitMqListener.Listen();
        }

        public void SetupMessageQueueClient()
        {
            var messageQueueClient = new RabbitMqClient() {
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
            };
            Container.Register<IMessageQueueClient>(messageQueueClient);
        }

        public void SetupScheduler()
        {
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
            Container.Register<IScheduler>(scheduler);
            scheduler.Start();
            var simpleTriggers = Container.Resolve<IElasticClient>().Search<SimpleTrigger>(x => x).Documents;
            var cronTriggers = Container.Resolve<IElasticClient>().Search<CronTrigger>(x => x).Documents;
            foreach (var jobType in IntegrationJobTypes)
            {
                // Register job
                var integrationJob = Activator.CreateInstance(jobType) as IIntegrationJob;
                var jobDetailsDataMap = new JobDataMap();
                jobDetailsDataMap.Put("IntegrationJob", integrationJob);
                var jobDetail = JobBuilder.Create<IntegrationJobDispatcherJob>()
                    .SetJobData(jobDetailsDataMap)
                    .WithIdentity(jobType.Name, jobType.Namespace)
                    .Build();
                // Schedule the job with applicable triggers
                ScheduleJobsWithSimpleTriggers(scheduler, jobType, jobDetail, simpleTriggers);
                ScheduleJobsWithCronTriggers(scheduler, jobType, jobDetail, cronTriggers);
            }
        }

        string GenerateTriggerId(Type jobType, IHasStringId triggerDefinition)
        {
            return string.Format("{0}-{1}", jobType.Name, triggerDefinition.Id);
        }

        void ScheduleJobsWithCronTriggers(IScheduler scheduler, Type jobType, IJobDetail jobDetail, IEnumerable<CronTrigger> triggers)
        {
            foreach (var triggerDefinition in triggers.Where(x => x.JobType == jobType.FullName))
            {
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(GenerateTriggerId(jobType, triggerDefinition), jobType.Namespace);
                if (triggerDefinition.CronExpressionString != null)
                    trigger.WithCronSchedule(triggerDefinition.CronExpressionString, x => x.InTimeZone(triggerDefinition.TimeZone));
                scheduler.ScheduleJob(jobDetail, trigger.Build());
            }
        }

        void ScheduleJobsWithSimpleTriggers(IScheduler scheduler, Type jobType, IJobDetail jobDetail, IEnumerable<SimpleTrigger> triggers)
        {
            foreach (var triggerDefinition in triggers.Where(x => x.JobType == jobType.FullName))
            {
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(GenerateTriggerId(jobType, triggerDefinition), jobType.Namespace);
                Action<SimpleScheduleBuilder> simpleScheduleBuilderAction;
                if (triggerDefinition.RepeatCount > 0)
                    simpleScheduleBuilderAction = x => x.WithInterval(triggerDefinition.RepeatInterval).WithRepeatCount(triggerDefinition.RepeatCount);
                else 
                    simpleScheduleBuilderAction = x => x.WithInterval(triggerDefinition.RepeatInterval);
                trigger.WithSimpleSchedule(simpleScheduleBuilderAction);
                if (!object.Equals(triggerDefinition.StartTimeUtc, default(DateTimeOffset)))
                    trigger.StartAt(triggerDefinition.StartTimeUtc);
                else
                    trigger.StartNow();
                scheduler.ScheduleJob(jobDetail, trigger.Build());
            }
        }

        public void SetupRScriptRunner()
        {
            Container.Register<RScriptRunner>(new RScriptRunner());
        }

        public void SetupElasticClient()
        {
            var config = Configuration.Elasticsearch;
            var serverUri = new UriBuilder(config.Protocol, config.HostName, config.Port).Uri;
            var settings = new ConnectionSettings(serverUri, config.DefaultIndex);
            var elasticClient = new ElasticClient(settings);
            Container.Register<IElasticClient>(elasticClient);
            Container.Register<ESRepository<SimpleTrigger>>(new ESRepository<SimpleTrigger>() {
                ElasticClient = elasticClient
            });
            Container.Register<ESRepository<CronTrigger>>(new ESRepository<CronTrigger>() {
                ElasticClient = elasticClient
            });
        }

        public void Shutdown()
        {
            var scheduler = Container.Resolve<IScheduler>();
            scheduler.Shutdown();
        }
    }
}

