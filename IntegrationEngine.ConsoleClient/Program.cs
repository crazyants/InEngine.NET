﻿using CommandLine;
using CommandLine.Text;
using IntegrationEngine.Client;
using IntegrationEngine.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntegrationEngine.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            var invokedVerb = "";
            var invokedVerbInstance = new object();
            if (args == null || 
                args.Length == 0 || 
                !CommandLine.Parser.Default.ParseArguments(args, options, (verb, subOptions) => {
                invokedVerb = verb;
                invokedVerbInstance = subOptions;
            }))
            {
                Console.WriteLine(options.GetUsage());
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            var client = !string.IsNullOrWhiteSpace(options.WebApiUrl) ? 
                new InEngineClient(options.WebApiUrl) :
                new InEngineClient();

            if (invokedVerb == "get") {
                var getSubOptions = (GetSubOptions)invokedVerbInstance;
                if (getSubOptions.Id != null)
                    switch (getSubOptions.Resource)
                    {
                    case Endpoint.CronTrigger:
                        ResolveResult(client.Get<CronTrigger>(getSubOptions.Id));
                        break;
                    case Endpoint.SimpleTrigger:
                        ResolveResult(client.Get<SimpleTrigger>(getSubOptions.Id));
                        break;
                    }
                else
                    switch (getSubOptions.Resource)
                    {
                    case Endpoint.CronTrigger:
                        ResolveResult(client.GetCollection<CronTrigger>());
                        break;
                    case Endpoint.SimpleTrigger:
                        ResolveResult(client.GetCollection<SimpleTrigger>());
                        break;
                    case Endpoint.JobType:
                        ResolveResult(client.GetCollection<JobType>());
                        break;
                    case Endpoint.LogEvent:
                        ResolveResult(client.GetCollection<LogEvent>());
                        break;
                    case Endpoint.HealthStatus:
                        ResolveResult(client.GetHealthStatus());
                        break;
                    }
            }
            if (invokedVerb == "ping") {
                Console.WriteLine(client.Ping());
            }
        }

        public static void ResolveResult(dynamic result)
        {
            if (result == null)
                Console.WriteLine("The web API did not return a result. Is the URL correct and the server online? Try -mPing.");
            else if (result is IEnumerable) 
            {
                Console.WriteLine("Number of items: {0}", result.Count);
                foreach (var item in result)
                    Console.WriteLine(item);
            }
            else
                Console.WriteLine(result);
        }
    }
}
