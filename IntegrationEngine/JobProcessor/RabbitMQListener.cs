﻿using Common.Logging;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.IntegrationJob;
using IntegrationEngine.Core.MessageQueue;
using IntegrationEngine.Model;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Practices.Unity;
using RabbitMQ.Client.Exceptions;

namespace IntegrationEngine.JobProcessor
{
    public class RabbitMQListener : IMessageQueueListener
    {
        int _listenerId;
        public IUnityContainer UnityContainer { get; set; }
        public QueueingBasicConsumer Consumer { get; set; }
        public IList<Type> IntegrationJobTypes { get; set; }
        public IRabbitMQConfiguration RabbitMQConfiguration { get; set; }
        public IMessageQueueConnection MessageQueueConnection { get; set; }
        public ILog Log { get; set; }
        public IConnection Connection { get; set; }

        public RabbitMQListener()
        {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void Dispose()
        {
            if (Consumer != null)
                Consumer.Queue.Close();
            if (Connection != null)
                Connection.Close();
        }

        public void Listen(CancellationToken cancellationToken, int listenerId)
        {
            _listenerId = listenerId;
            Connection = MessageQueueConnection.GetConnectionFactory().CreateConnection();
            using (var channel = Connection.CreateModel())
            {
                Consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(RabbitMQConfiguration.QueueName, true, Consumer);
                Log.Info(x => x("(Id={0}) Waiting for messages...", _listenerId));

                while (true)
                {
                    var message = new DispatchTrigger();
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var eventArgs = (BasicDeliverEventArgs)Consumer.Queue.Dequeue();
                        var body = eventArgs.Body;
                        message = JsonConvert.DeserializeObject<DispatchTrigger>(Encoding.UTF8.GetString(body));
                        Log.Debug(x => x("Message queue listener received {0}", message));
                        if (IntegrationJobTypes != null && !IntegrationJobTypes.Any())
                            continue;
                        var type = IntegrationJobTypes.FirstOrDefault(t => t.FullName.Equals(message.JobType));
                        var integrationJob = UnityContainer.Resolve(type) as IIntegrationJob;
                        if (integrationJob != null)
                        {
                            if (integrationJob is IParameterizedJob) {
                                var parameterizedJob = integrationJob as IParameterizedJob;
                                parameterizedJob.Parameters = message.Parameters;
                                Log.Info(x => x("Running job: {0} with parameters {1}", integrationJob, message.Parameters));
                                parameterizedJob.Run();
                            }
                            else {
                                Log.Info(x => x("Running job: {0}", integrationJob));
                                integrationJob.Run();
                            }
                            Log.Info(x => x("Integration job ran successfully: {0}", integrationJob));
                        }
                    }
                    catch (OperationCanceledException exception)
                    { 
                        Log.Info(x => x("Message queue listener (id: {0}), listening on queue \"{1}\", has gracefully shutdown.", 
                            _listenerId,
                            RabbitMQConfiguration.QueueName), exception);
                        return;
                    }
                    catch (IntegrationJobRunFailureException exception)
                    {
                        Log.Error(x => x("Integration job did not run successfully ({0}).", message.JobType), exception);
                    }
                    catch (EndOfStreamException exception)
                    {
                        Log.Debug(x => x("The message queue ({0}) has closed.", RabbitMQConfiguration.QueueName), exception);
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Issue receiving/decoding dispatch message or running job.", exception);
                    }
                }
            }
        }
    }
}
