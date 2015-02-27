using IntegrationServer.IntegrationPoints;
using IntegrationEngine.Core.IntegrationJob;
using IntegrationEngine.Core.Mail;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using RazorEngine.Configuration;
using IntegrationEngine.Core.Mongo;
using MongoDB.Driver;

namespace IntegrationServer.IntegrationJobs.CarReport
{
    public class CarMailMessageJob : IParameterizedJob
    {
        public IMailClient MailClient { get; set; }
        public ExampleJsonService ExampleJsonService { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
        public IMongoClient MongoClient { get; set; }

        public CarMailMessageJob()
        {
        }

        public CarMailMessageJob(FooMailClient mailClient, ExampleJsonService exampleJsonService, ExampleMongo mongoClient)
            : this()
        {
            MailClient = mailClient;
            ExampleJsonService = exampleJsonService;
            MongoClient = mongoClient;
        } 

        public void Run()
        {
            var mailMessage = new MailMessage();
            mailMessage.To.Add("ethanhann@gmail.com");
            mailMessage.Subject = "Your car report is ready.";
            mailMessage.From = new MailAddress("root@localhost");
            mailMessage.Body = "Body content about cars.";
            var database = MongoClient.GetDatabase("IntegrationEngine");
            var mailMessages = database.GetCollection<MailMessage>("MailMessages");
            mailMessages.Insert(mailMessage);
            MailClient.Send(mailMessage);
        }
    }
}

