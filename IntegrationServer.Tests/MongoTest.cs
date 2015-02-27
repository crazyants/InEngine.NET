using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeekmanLabs.UnitTesting;
using NUnit.Framework;

namespace IntegrationServer.Tests
{
    public class MongoTest
    {
        [Test]
        public void ShouldInitializeEngine()
        {
            Program.Start();

            Assert.That(Program.EngineHost, Is.Not.Null);

            Program.Stop();
        }
    }
}
