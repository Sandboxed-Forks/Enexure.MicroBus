using Enexure.MicroBus.Sagas;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Enexure.MicroBus.Saga.Autofac.Tests
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public class TestCommandingSaga : ISaga,
        ISagaStartedBy<SagaStartingAEvent>,
        IEventHandler<SagaEndingEvent>
    {
        public Guid Id { get; protected set; }
        public bool IsCompleted { get; protected set; }
        public static string Status { get; protected set; }

        private readonly IMicroBus Bus;
        public TestCommandingSaga(IMicroBus bus)
        {
            Bus = bus;
        }

        public async Task Handle(SagaStartingAEvent @event)
        {
            Id = @event.CorrelationId;
            Status = "Started, ";
            await Bus.SendAsync(new EndSaga { CorrelationId = Id });
        }

        public async Task Handle(SagaEndingEvent @event)
        {
            Status += "Finished.";
            IsCompleted = true;
        }
    }

    public class EndSagaCommandHandler : ICommandHandler<EndSaga>
    {
        private readonly IMicroBus Bus;
        public EndSagaCommandHandler(IMicroBus bus)
        {
            Bus = bus;
        }

        public Task Handle(EndSaga command)
        {
            Bus.PublishAsync(new SagaEndingEvent { CorrelationId = command.CorrelationId });
            return Task.CompletedTask;
        }
    }

    public class TestCommandingSagaRepository : ISagaRepository<TestCommandingSaga>
    {
        Dictionary<Guid, TestCommandingSaga> sagas = new Dictionary<Guid, TestCommandingSaga>();
        private readonly IMicroBus Bus;
        public TestCommandingSagaRepository(IMicroBus bus)
        {
            Bus = bus;
        }

        public Task CompleteAsync(TestCommandingSaga saga)
        {
            sagas.Remove(saga.Id);
            return Task.CompletedTask;
        }

        public Task CreateAsync(TestCommandingSaga saga)
        {
            sagas.Add(saga.Id, saga);
            return Task.CompletedTask;
        }

        public Task<TestCommandingSaga> FindAsync(IEvent message)
        {
            var correlatedMessage = message as IHaveCorrelationId;
            if (correlatedMessage != null)
            {
                return FindById(correlatedMessage.CorrelationId);
            }

            throw new Exception("message must inherit from the interface IHaveCorrelationId");
        }

        public Task<TestCommandingSaga> FindById(Guid id)
        {
            return Task.FromResult(sagas.ContainsKey(id) ? sagas[id] : null);
        }

        public TestCommandingSaga NewSaga()
        {
            return new TestCommandingSaga(Bus);
        }

        public Task UpdateAsync(TestCommandingSaga saga)
        {
            return Task.CompletedTask;
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public class EndSaga : ICommand
    {
        public Guid CorrelationId { get; set; }
    }
}
