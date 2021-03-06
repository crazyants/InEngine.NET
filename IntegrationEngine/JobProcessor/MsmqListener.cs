﻿using Common.Logging;
using IntegrationEngine.Core.IntegrationJob;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.Storage;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading;
using MSMessageQueue = System.Messaging.MessageQueue;

namespace IntegrationEngine.JobProcessor
{
    public class MsmqListener : IMessageQueueListener
    {
        public IList<Type> IntegrationJobTypes { get; set; }
        public ILog Log { get; set; }
        public IMailClient MailClient { get; set; }
        public IntegrationEngineContext IntegrationEngineContext { get; set; }
        public IElasticClient ElasticClient { get; set; }
        public MSMessageQueue MSMessageQueue { get; set; }
        string _queueName { get; set; }
        public string QueueName
        {
            get { return _queueName; }
            set
            {
                if (!MSMessageQueue.Exists(QueueName))
                    MSMessageQueue.Create(QueueName);
                _queueName = value;
            }
        }

        public void Listen(CancellationToken cancellationToken, int listenerId)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Message newMessage = MSMessageQueue.Receive();
            var body = newMessage.Body as byte[];
            var message = Encoding.UTF8.GetString(body);
            Log.Debug(x => x("Message queue listener received {0}", message));
            if (IntegrationJobTypes != null && !IntegrationJobTypes.Any())
                return;
            var type = IntegrationJobTypes.FirstOrDefault(t => t.FullName.Equals(message));
            var integrationJob = Activator.CreateInstance(type) as IIntegrationJob;
            try
            {
                if (integrationJob != null)
                    integrationJob.Run();
            }
            catch (Exception exception)
            {
                Log.Error(x => x("Integration job did not run successfully ({0})}", message), exception);
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
