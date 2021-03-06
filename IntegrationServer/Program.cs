﻿using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using IntegrationEngine;

namespace IntegrationServer
{
    public class Program
    {
        public const string ServiceName = "InEngine.NET Server";
        public static EngineHost EngineHost { get; set; }
        public static void Main(string[] args)
        {
            var isRunningUnderMono = Type.GetType("Mono.Runtime") != null;
            if (!Environment.UserInteractive && !isRunningUnderMono)
            {
                // Set current working directory as services use the system directory by default.
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                using (var service = new Service())
                    ServiceBase.Run(service);
            }
            else
            {
                Start(args);
                Console.WriteLine("Press any key to stop...");
                Console.ReadLine();
                Stop();
            }
        }

        private static void Start(string[] args)
        {
            EngineHost = new EngineHost(typeof(Program).Assembly);
            EngineHost.Initialize();
        }

        private static void Stop()
        {
            EngineHost.Dispose();
        }

        public class Service : ServiceBase
        {
            public Service()
            {
                ServiceName = Program.ServiceName;
            }

            protected override void OnStart(string[] args)
            {
                Program.Start(args);
            }

            protected override void OnStop()
            {
                Program.Stop();
            }
        }
    }
}
