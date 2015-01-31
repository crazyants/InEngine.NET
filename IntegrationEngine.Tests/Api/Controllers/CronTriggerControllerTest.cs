﻿using BeekmanLabs.UnitTesting;
using IntegrationEngine.Api.Controllers;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Scheduler;
using Moq;
using NUnit.Framework;

namespace IntegrationEngine.Tests.Api.Controllers
{
    public class CronTriggerControllerTest : TestBase<CronTriggerController>
    {
        [Test]
        public void ShouldScheduleJobWhenCronTriggerIsCreatedWithValidCronExpression()
        {
            var cronExpression = "0 6 * * 1-5 ?";
            var jobType = "MyProject.MyIntegrationJob";
            var expected = new CronTrigger() {
                JobType = jobType,
                CronExpressionString = cronExpression
            };
            var engineScheduler = new Mock<IEngineScheduler>();
            engineScheduler.Setup(x => x.ScheduleJobWithTrigger(expected));
            Subject.EngineScheduler = engineScheduler.Object;
            var esRepository = new Mock<IElasticsearchRepository>();
            esRepository.Setup(x => x.Insert(expected)).Returns(expected);
            Subject.Repository = esRepository.Object;

            Subject.Post(expected);

            engineScheduler.Verify(x => x
                .ScheduleJobWithTrigger(It.Is<CronTrigger>(y => y.JobType == jobType &&
                                                                y.CronExpressionString == cronExpression)), Times.Once);
        }

        [Test]
        public void ShouldDeleteCronTrigger()
        {
            string id = "1";
            var expected = new CronTrigger() { Id = id };
            var engineScheduler = new Mock<IEngineScheduler>();
            engineScheduler.Setup(x => x.DeleteTrigger(expected));
            Subject.EngineScheduler = engineScheduler.Object;
            var esRepository = new Mock<IElasticsearchRepository>();
            esRepository.Setup(x => x.SelectById<CronTrigger>(id)).Returns(expected);
            esRepository.Setup(x => x.Delete<CronTrigger>(id));
            Subject.Repository = esRepository.Object;

            Subject.Delete(id);

            esRepository.Verify(x => x.SelectById<CronTrigger>(id), Times.Once);
            esRepository.Verify(x => x.Delete<CronTrigger>(id), Times.Once);
            engineScheduler.Verify(x => x.DeleteTrigger(expected), Times.Once);
        }
    }
}