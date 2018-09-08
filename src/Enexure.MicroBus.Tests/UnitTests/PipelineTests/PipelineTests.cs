﻿using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Enexure.MicroBus.Tests.UnitTests.PipelineTests
{
    public class PipelineTests
    {
        //[Fact(Skip = "Not finished")]
        public void PipelinesRunInTheCorrectOrderTest()
        {

        }

        private class Command : ICommand
        {
            public int CallerId { get; set; }
        }

        private class CommandHandler : ICommandHandler<Command>
        {
            public Task Handle(Command command)
            {
                return Task.FromResult(0);
            }
        }

        private class PipelineHandlerA : IPipelineHandler
        {
            public async Task<object> Handle(Func<IMessage, Task<object>> next, IMessage message)
            {
                var command = (Command)message;

                command.CallerId.Should().Be(0);
                command.CallerId += 1;

                var result = await next(message);

                command.CallerId.Should().Be(1);
                command.CallerId -= 1;

                return result;
            }
        }

        private class PipelineHandlerB : IPipelineHandler
        {
            public async Task<object> Handle(Func<IMessage, Task<object>> next, IMessage message)
            {
                var command = (Command)message;

                command.CallerId.Should().Be(1);
                command.CallerId += 1;

                var result = await next(message);

                command.CallerId.Should().Be(2);
                command.CallerId -= 1;

                return result;
            }
        }
    }
}
