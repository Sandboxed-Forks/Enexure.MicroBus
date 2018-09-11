using Autofac;
using Enexure.MicroBus.Autofac;
using Enexure.MicroBus.Sagas;
using Xunit;
using System;
using System.Threading.Tasks;
using FluentAssertions;
using System.Threading;

namespace Enexure.MicroBus.Saga.Autofac.Tests
{
    public class CompleteCommandingSagaTests
    {
        private readonly Guid id = Guid.NewGuid();

        [Fact]
        public async Task RunningACommandingSagaToCompletion()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<TestCommandingSagaRepository>().AsImplementedInterfaces().SingleInstance();

            var busBuilder = new BusBuilder()
                .RegisterSaga<TestCommandingSaga>()
                .RegisterHandlers(this.GetType().Assembly);

            var container = builder
                .RegisterMicroBus(busBuilder)
                .Build();

            var bus = container.Resolve<IMicroBus>();

            await bus.PublishAsync(new SagaStartingAEvent() { CorrelationId = id });
            string expected = "Started, Finished";
            Thread.Sleep(5000);
            Assert.Equal(expected, TestCommandingSaga.Status);
        }
    }
}
