﻿using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace Enexure.MicroBus.MicrosoftDependencyInjection.Tests
{
    public class MicrosoftDependencyInjectionEventTests
    {
        class Event : IEvent
        {
            public int Tally { get; set; }
        }

        class EventHandler : IEventHandler<Event>
        {
            public Task Handle(Event @event)
            {
                @event.Tally += 1;

                return Task.FromResult(0);
            }
        }

        class EventHandler2 : IEventHandler<Event>
        {
            public Task Handle(Event @event)
            {
                @event.Tally += 1;

                return Task.FromResult(0);
            }
        }

        [Fact]
        public async Task TestEvent()
        {
            var busBuilder = new BusBuilder()
                .RegisterEventHandler<Event, EventHandler>();

            var container = new ServiceCollection().RegisterMicroBus(busBuilder).BuildServiceProvider();

            var bus = container.GetRequiredService<IMicroBus>();

            var @event = new Event();
            await bus.PublishAsync(@event);
            
            @event.Tally.Should().Be(1);
        }

        [Fact]
        public async Task TestMultipleEvents()
        {
            var busBuilder = new BusBuilder()
                .RegisterEventHandler<Event, EventHandler>()
                .RegisterEventHandler<Event, EventHandler2>();

            var container = new ServiceCollection().RegisterMicroBus(busBuilder).BuildServiceProvider();

            var bus = container.GetRequiredService<IMicroBus>();

            var @event = new Event();
            await bus.PublishAsync(@event);

            @event.Tally.Should().Be(2);
        }
    }
}
