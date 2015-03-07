using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.NLog;
using IntegrationEngine.Api;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.Elasticsearch;
using IntegrationEngine.Core.IntegrationJob;
using IntegrationEngine.Core.IntegrationPoint;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.MessageQueue;
using IntegrationEngine.Core.Mongo;
using IntegrationEngine.Core.R;
using IntegrationEngine.Core.ServiceStack;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.JobProcessor;
using IntegrationEngine.Scheduler;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;
using MongoDB.Driver;
using Nest;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace IntegrationEngine
{
    public class EngineHostCompositionRoot : IDisposable
    {
        public IUnityContainer Container { get; set; }
        public IEngineConfiguration EngineConfiguration { get; set; }
        public IList<Type> IntegrationJobTypes { get; set; }
        public ILog Log { get; set; }
        public IWebApiApplication WebApiApplication { get; set; }
        public IMessageQueueListenerManager MessageQueueListenerManager { get; set; }
        public bool IsWebApiEnabled { get; set; }
        public bool IsSchedulerEnabled { get; set; }
        public bool IsMessageQueueListenerManagerEnabled { get; set; }
        public string JobProcessorMessageQueueName { get; set; }
        public string JobTriggerRepositoryName { get; set; }
        public IStringIdRepository Repository { get; set; }

        public EngineHostCompositionRoot()
        {}

        public EngineHostCompositionRoot(IList<Assembly> assembliesWithJobs)
            : this()
        {
            Container = new UnityContainer();
            IntegrationJobTypes = ExtractIntegrationJobTypesFromAssemblies(assembliesWithJobs);
        }

        public IList<Type> ExtractIntegrationJobTypesFromAssemblies(IList<Assembly> assembliesWithJobs)
        {
            return assembliesWithJobs
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(IIntegrationJob).IsAssignableFrom(x) && x.IsClass)
                .ToList();
        }

        public void Configure()
        {
            LoadConfiguration();
            SetupLogging();
            RegisterIntegrationPoints();
            RegisterIntegrationJobs();
            SetupRScriptRunner();
            SetupRepository();
            if (IsMessageQueueListenerManagerEnabled) {
                SetupMessageQueueListenerManager();
                StartMessageQueueListener();
            }
            if (IsSchedulerEnabled)
                SetupEngineScheduler();
            if (IsWebApiEnabled)
                SetupWebApi();
        }

        public bool DoesIntegrationPointConfigurationExist<T>(IList<T> integrationPointConfigList, string integrationPointName)
            where T : IIntegrationPointConfiguration
        {
            return integrationPointConfigList.Where(x => x.IntegrationPointName == integrationPointName).Any();
        }

        public void LoadConfiguration()
        {
            try
            {
                var config = new EngineConfiguration();
                JobProcessorMessageQueueName = config.JobProcessorMessageQueueName;
                JobTriggerRepositoryName = config.JobTriggerRepositoryName;
                if (JobProcessorMessageQueueName == null)
                    throw new Exception("JobProcessorMessageQueueName config option should not be null.");
                if (JobTriggerRepositoryName == null)
                    throw new Exception("JobTriggerRepositoryName config option should not be null.");
                if (!DoesIntegrationPointConfigurationExist(config.IntegrationPoints.RabbitMQ, JobProcessorMessageQueueName))
                    throw new Exception(string.Format("JobProcessorMessageQueueName ({0}) is not an integration point", JobProcessorMessageQueueName));
                if (!DoesIntegrationPointConfigurationExist(config.IntegrationPoints.Elasticsearch, JobTriggerRepositoryName) &&
                    !DoesIntegrationPointConfigurationExist(config.IntegrationPoints.Mongo, JobTriggerRepositoryName))
                    throw new Exception(string.Format("JobTriggerRepositoryName ({0}) is not an integration point", JobTriggerRepositoryName));
            }
            catch (Exception exception)
            {
                throw new Exception("Issue loading configuration.", exception);
            }
            Container.RegisterType<IEngineConfiguration, EngineConfiguration>();
            EngineConfiguration = Container.Resolve<IEngineConfiguration>();
        }

        public void SetupLogging()
        {
            var config = EngineConfiguration.NLogAdapter;
            var properties = new NameValueCollection();
            properties["configType"] = config.ConfigType;
            properties["configFile"] = config.ConfigFile;
            Common.Logging.LogManager.Adapter = new NLogLoggerFactoryAdapter(properties);  
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void SetupDatabaseRepository(IntegrationEngineContext integrationEngineContext)
        {
            Container.RegisterInstance<IDatabaseRepository>(new DatabaseRepository(integrationEngineContext));
        }

        void RegisterConfig(Type from, Type to, string integrationPointName)
        {
            Container.RegisterType(from, to, integrationPointName,
                new InjectionConstructor(new ResolvedParameter<IEngineConfiguration>(), integrationPointName)
            );
        }

        public void RegisterIntegrationPoints()
        {
            Container.RegisterType<Elasticsearch.Net.Connection.IConnection, Elasticsearch.Net.Connection.HttpConnection>();
            Container.RegisterType<INestSerializer, NestSerializer>();
            Container.RegisterType<Elasticsearch.Net.Connection.ITransport, Elasticsearch.Net.Connection.Transport>();
            Container.RegisterType<IConnectionSettingsValues, ConnectionSettings>();
            foreach (var config in EngineConfiguration.IntegrationPoints.Mail) {
                RegisterConfig(typeof(IMailConfiguration), typeof(MailConfiguration), config.IntegrationPointName);
                Container.RegisterType<IMailClient, MailClient>(config.IntegrationPointName, new InjectionConstructor(config));
            }

            Func<IUnityContainer, Type, string, object> elasticClientFactory = (container, type, configName) => {
                var config = container.Resolve<IElasticsearchConfiguration>(configName);
                var serverUri = new UriBuilder(config.Protocol, config.HostName, config.Port).Uri;
                var settings = new ConnectionSettings(serverUri, config.DefaultIndex);
                return new ElasticClient(settings);
            };
            foreach (var config in EngineConfiguration.IntegrationPoints.Elasticsearch) {
                RegisterConfig(typeof(IElasticsearchConfiguration), typeof(ElasticsearchConfiguration), config.IntegrationPointName);
                Container.RegisterType<IElasticClient, ElasticClientAdapter>(config.IntegrationPointName, new InjectionFactory(elasticClientFactory));
            }
            foreach (var config in EngineConfiguration.IntegrationPoints.RabbitMQ) {
                RegisterConfig(typeof(IRabbitMQConfiguration), typeof(RabbitMQConfiguration), config.IntegrationPointName);
                Container.RegisterType<IRabbitMQClient, RabbitMQClient>(config.IntegrationPointName, new InjectionConstructor(config));
            }
            foreach (var config in EngineConfiguration.IntegrationPoints.JsonService) {
                RegisterConfig(typeof(IJsonServiceConfiguration), typeof(JsonServiceConfiguration), config.IntegrationPointName);
                Container.RegisterType<IJsonServiceClient, JsonServiceClientAdapter>(config.IntegrationPointName, new InjectionConstructor(config));
            }
            if (EngineConfiguration.IntegrationPoints.Mongo != null)
                foreach (var config in EngineConfiguration.IntegrationPoints.Mongo)
                {
                    RegisterConfig(typeof(IMongoConfiguration), typeof(MongoConfiguration), config.IntegrationPointName);
                    Container.RegisterType<IMongoClient, MongoClientAdapter>(config.IntegrationPointName, new InjectionConstructor(config));
                }
        }

        /// <summary>
        /// Registers the integration jobs.
        /// Resolve the integration point type (specified in a job's parameters).
        /// Configure the integration point type with a configuration, based on a parameter's name.                    
        /// </summary>
        public void RegisterIntegrationJobs()
        {
            IntegrationJobTypes.ForEach(jobType => {
                Func<ParameterInfo[], object[]> resolveParameters = infos => {
                    var resolvedParameters = new List<object>();
                    foreach (var parameterInfo in infos)
                    {
                        var parameterType = parameterInfo.ParameterType; // The type of integration point (e.g. IElasticClient)
                        var parameterName = parameterInfo.ParameterType.Name; // The name of the configuration endpoint (e.g. "MyElasticClient")
                        // If the parameter implements IIntegrationPoint, resolve it's configuration type from the container.
                        if (typeof(IIntegrationPoint).IsAssignableFrom(parameterType))
                        {
                            var configType = parameterType.GetInterface(typeof(IIntegrationPoint<IIntegrationPointConfiguration>).Name).GetGenericArguments().Single(); ;
                            resolvedParameters.Add(Activator.CreateInstance(parameterType, Container.Resolve(configType, parameterName)));
                        }
                    }
                    return resolvedParameters.Cast<object>().ToArray();
                };
                var constructors = jobType.GetConstructors();
                if (constructors.Count() == 1 && !constructors.Single().GetParameters().Any()) // Handle Default Constructor case.
                    Container.RegisterType(jobType, new InjectionFactory((c, t, s) => Activator.CreateInstance(jobType)));
                else
                {
                    // Use the first constructor with parameters.
                    var constructor = constructors.First(x => x.GetParameters().Any());
                    Container.RegisterType(jobType, new InjectionFactory((c, t, s) => Activator.CreateInstance(jobType, resolveParameters(constructor.GetParameters())))); 
                }
            });
        }

        public void SetupMessageQueueListenerManager()
        {
            var config = Container.Resolve<IRabbitMQConfiguration>(JobProcessorMessageQueueName);
            var messageQueueListenerFactory = new MessageQueueListenerFactory(Container, IntegrationJobTypes, config);
            MessageQueueListenerManager = new MessageQueueListenerManager() {
                MessageQueueListenerFactory = messageQueueListenerFactory,
                ListenerTaskCount = EngineConfiguration.JobProcessorCount,
            };
        }

        public async void StartMessageQueueListener()
        {
            await MessageQueueListenerManager.StartListener();
        }

        public void SetupEngineScheduler()
        {
            var dispatcher = new Dispatcher() {
                MessageQueueClient = Container.Resolve<IRabbitMQClient>(JobProcessorMessageQueueName),
            };
            var engineScheduler = new EngineScheduler() {
                Scheduler = StdSchedulerFactory.GetDefaultScheduler(),
                IntegrationJobTypes = IntegrationJobTypes,
                Dispatcher = dispatcher,
            };
            Container.RegisterInstance<IEngineScheduler>(engineScheduler);
            var engineSchedulerListener = new EngineSchedulerListener() {
                Repository = Repository,
            };
            engineScheduler.AddSchedulerListener(engineSchedulerListener);
            engineScheduler.Start();
            var simpleTriggers = Repository.SelectAll<SimpleTrigger>();
            var allCronTriggers = Repository.SelectAll<CronTrigger>();
            var cronTriggers = allCronTriggers.Where(x => !string.IsNullOrWhiteSpace(x.CronExpressionString));
            foreach (var trigger in simpleTriggers)
                engineScheduler.ScheduleJobWithTrigger(trigger);
            foreach (var trigger in cronTriggers)
                engineScheduler.ScheduleJobWithTrigger(trigger);
            foreach(var cronTrigger in allCronTriggers.Where(x => string.IsNullOrWhiteSpace(x.CronExpressionString)))
                Log.Warn(x => x("Cron expression for trigger ({0}) is null, empty, or whitespace.", cronTrigger.Id));
        }

        public void SetupRepository()
        {
            if (DoesIntegrationPointConfigurationExist(EngineConfiguration.IntegrationPoints.Elasticsearch, JobTriggerRepositoryName))
            {
                Container.RegisterType<IElasticsearchRepository, ElasticsearchRepository>(new InjectionConstructor(new ResolvedParameter<IElasticClient>(JobTriggerRepositoryName)));
                Repository = Container.Resolve<IElasticsearchRepository>();
            }
            if (DoesIntegrationPointConfigurationExist(EngineConfiguration.IntegrationPoints.Mongo, JobTriggerRepositoryName))
            {
                Container.RegisterType<IMongoRepository, MongoRepository>(new InjectionConstructor(new ResolvedParameter<IMongoClient>(JobTriggerRepositoryName)));
                Repository = Container.Resolve<IMongoRepository>();
            }
            Container.RegisterInstance<IStringIdRepository>(Repository);
        }

        public void SetupRScriptRunner()
        {
            Container.RegisterInstance<IRScriptRunner>(new RScriptRunner());
        }

        public void SetupWebApi()
        {
            WebApiApplication = new WebApiApplication() { 
                WebApiConfiguration = EngineConfiguration.WebApi,
                ContainerResolver = new ContainerResolver(Container)
            };
            WebApiApplication.Start();
        }

        public void Dispose()
        {
            if (WebApiApplication != null)
                WebApiApplication.Dispose();
            if (MessageQueueListenerManager != null)
                MessageQueueListenerManager.Dispose();
        }
    }
}
