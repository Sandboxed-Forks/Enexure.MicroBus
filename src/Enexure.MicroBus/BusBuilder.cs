﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Enexure.MicroBus
{
    using System.Threading;

    public class BusBuilder
    {
        private readonly List<HandlerRegistration> registrations = new List<HandlerRegistration>();
        private readonly List<GlobalHandlerRegistration> globalHandlers =  new List<GlobalHandlerRegistration>();

        public List<HandlerRegistration> MessageHandlerRegistrations => registrations;

        public List<GlobalHandlerRegistration> GlobalHandlerRegistrations => globalHandlers;

        public BusBuilder RegisterCommandHandler<TCommand, TCommandHandler>()
            where TCommand : ICommand
            where TCommandHandler : ICommandHandler<TCommand>
        {
            registrations.Add(HandlerRegistration.New<TCommand, CommandHandlerShim<TCommand, TCommandHandler>>(new[] { typeof(TCommandHandler) }));
            return this;
        }

        public BusBuilder RegisterCancelableCommandHandler<TCommand, TCommandHandler>()
            where TCommand : ICommand
            where TCommandHandler : ICancelableCommandHandler<TCommand>
        {
            registrations.Add(HandlerRegistration.New<TCommand, CancelableCommandHandlerShim<TCommand, TCommandHandler>>(new[] { typeof(TCommandHandler) }));
            return this;
        }

        public BusBuilder RegisterEventHandler<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            registrations.Add(HandlerRegistration.New<TEvent, EventHandlerShim<TEvent, TEventHandler>>(new[] { typeof(TEventHandler) }));
            return this;
        }

        public BusBuilder RegisterCancelableEventHandler<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : ICancelableEventHandler<TEvent>
        {
            registrations.Add(HandlerRegistration.New<TEvent, CancelableEventHandlerShim<TEvent, TEventHandler>>(new[] { typeof(TEventHandler) }));
            return this;
        }

        public BusBuilder RegisterQueryHandler<TQuery, TResult, TQueryHandler>()
            where TQuery : IQuery<TQuery, TResult>
            where TQueryHandler : IQueryHandler<TQuery, TResult>
        {
            registrations.Add(HandlerRegistration.New<TQuery, QueryHandlerShim<TQuery, TResult, TQueryHandler>>(new[] { typeof(TQueryHandler) }));
            return this;
        }

        public BusBuilder RegisterCancelableQueryHandler<TQuery, TResult, TQueryHandler>()
            where TQuery : IQuery<TQuery, TResult>
            where TQueryHandler : ICancelableQueryHandler<TQuery, TResult>
        {
            registrations.Add(HandlerRegistration.New<TQuery, CancelableQueryHandlerShim<TQuery, TResult, TQueryHandler>>(new[] { typeof(TQueryHandler) }));
            return this;
        }

        public BusBuilder RegisterMessage(HandlerRegistration registration)
        {
            registrations.Add(registration);
            return this;
        }

        public BusBuilder RegisterHandler<TMessage, TMessageHandler>()
            where TMessageHandler : IMessageHandler<TMessage, Unit>
        {
            registrations.Add(HandlerRegistration.New<TMessage, TMessageHandler>());
            return this;
        }

        public BusBuilder RegisterHandler<TMessage, TResult, TMessageHandler>()
            where TMessageHandler : IMessageHandler<TMessage, TResult>
        {
            registrations.Add(HandlerRegistration.New<TMessage, TMessageHandler>());
            return this;
        }

        public BusBuilder RegisterCancelableHandler<TMessage, TMessageHandler>()
            where TMessageHandler : ICancelableMessageHandler<TMessage, Unit>
        {
            registrations.Add(HandlerRegistration.New<TMessage, TMessageHandler>());
            return this;
        }

        public BusBuilder RegisterCancelableHandler<TMessage, TResult, TMessageHandler>()
            where TMessageHandler : ICancelableMessageHandler<TMessage, TResult>
        {
            registrations.Add(HandlerRegistration.New<TMessage, TMessageHandler>());
            return this;
        }

        public BusBuilder RegisterHandlers(Assembly assembly)
        {
            return RegisterHandlers(x => true, (IEnumerable<Assembly>)new[] { assembly });
        }

        public BusBuilder RegisterHandlers(IEnumerable<Assembly> assemblies)
        {
            return RegisterHandlers(x => true, assemblies);
        }

        public BusBuilder RegisterHandlers(params Assembly[] assemblies)
        {
            return RegisterHandlers(x => true, (IEnumerable<Assembly>)assemblies);
        }

        public BusBuilder RegisterHandlers(Func<Type, bool> predicate, params Assembly[] assemblies)
        {
            return RegisterHandlers(predicate, (IEnumerable<Assembly>)assemblies);
        }

        public BusBuilder RegisterHandlers(Func<Type, bool> predicate, IEnumerable<Assembly> assemblies)
        {
            var possibleTypes = assemblies
                .SelectMany(AllTheTypes)
                .Where(x => predicate(x.AsType()));

            return RegisterHandlers(possibleTypes);
        }

        public BusBuilder RegisterHandlers(IEnumerable<TypeInfo> types)
        {
            var handlerRegistrations = types
                .SelectMany(HandlersOnTheTypes)
                .Select(HandlersAsRegistrations);

            registrations.AddRange(handlerRegistrations);
            return this;
        }

        [Obsolete("Use RegisterGlobalHandler instead")]
        public BusBuilder RegisterPipelineHandler<T>()
        {
            globalHandlers.Add(new GlobalHandlerRegistration(typeof(PipelineHandlerToDelegatingHandlerConverter<>).MakeGenericType(typeof(T)), new[] { typeof(T) }));
            return this;
        }

        private HandlerRegistration HandlersAsRegistrations(GenericMatch match)
        {
            return new HandlerRegistration(match.MessageType, match.HandlerType);
        }

        private IEnumerable<GenericMatch> HandlersOnTheTypes(TypeInfo type)
        {
            var types = new[] {
                type.GetGenericMatches(typeof(ICommandHandler<>)),
                type.GetGenericMatches(typeof(IEventHandler<>)),
                type.GetGenericMatches(typeof(IQueryHandler<,>)),
                type.GetGenericMatches(typeof(ICancelableCommandHandler<>)),
                type.GetGenericMatches(typeof(ICancelableEventHandler<>)),
                type.GetGenericMatches(typeof(ICancelableQueryHandler<,>)),
                type.GetGenericMatches(typeof(IMessageHandler<,>)),
                type.GetGenericMatches(typeof(ICancelableMessageHandler<,>))
            };
            return types.SelectMany(x => x);
        }

        private IEnumerable<TypeInfo> AllTheTypes(Assembly assembly)
        {
            return assembly.DefinedTypes;
        }

        public BusBuilder RegisterGlobalHandler<THandler>()
            where THandler : IDelegatingHandler
        {
            globalHandlers.Add(new GlobalHandlerRegistration(typeof(THandler)));
            return this;
        }

        public BusBuilder RegisterCancelableGlobalHandler<THandler>()
            where THandler : ICancelableDelegatingHandler
        {
            globalHandlers.Add(new GlobalHandlerRegistration(typeof(THandler)));
            return this;
        }
    }

    internal class PipelineHandlerToDelegatingHandlerConverter<T> : IDelegatingHandler
        where T : IPipelineHandler
    {
        private readonly IPipelineHandler handler;

        public PipelineHandlerToDelegatingHandlerConverter(T handler)
        {
            this.handler = handler;
        }

        public Task<object> Handle(INextHandler next, object message)
        {
            return handler.Handle(async x => await next.Handle(x), message as IMessage);
        }
    }

    internal class CommandHandlerShim<TCommand, TCommandHandler> : IMessageHandler<TCommand, Unit>
        where TCommandHandler : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly TCommandHandler handler;

        public CommandHandlerShim(TCommandHandler handler)
        {
            this.handler = handler;
        }

        public async Task<Unit> Handle(TCommand message)
        {
            var task = handler.Handle(message);
            if (task == null) {
                throw new NullReferenceException($"The command handler {typeof(TCommandHandler).ToString()} returned null, expected a Task");
            } else {
                await task;
            }
            return Unit.Unit;
        }
    }

    internal class EventHandlerShim<TEvent, TEventHandler> : IMessageHandler<TEvent, Unit>
        where TEventHandler : IEventHandler<TEvent>
        where TEvent : IEvent
    {
        private readonly TEventHandler handler;

        public EventHandlerShim(TEventHandler handler)
        {
            this.handler = handler;
        }

        public async Task<Unit> Handle(TEvent message)
        {
            var task = handler.Handle(message);
            if (task == null) {
                throw new NullReferenceException($"The event handler {typeof(TEventHandler).ToString()} returned null, expected a Task");
            } else {
                await task;
            }

            return Unit.Unit;
        }
    }

    internal class QueryHandlerShim<TQuery, TResult, TQueryHandler> : IMessageHandler<TQuery, TResult>
        where TQueryHandler : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TQuery, TResult>
    {
        private readonly TQueryHandler handler;

        public QueryHandlerShim(TQueryHandler handler)
        {
            this.handler = handler;
        }

        public async Task<TResult> Handle(TQuery query)
        {
            var task = handler.Handle(query);
            if (task == null) {
                throw new NullReferenceException($"The query handler {typeof(TQueryHandler).ToString()} returned null, expected a Task");
            } else {
                return await task;
            }
        }
    }

    internal class CancelableCommandHandlerShim<TCommand, TCommandHandler> : ICancelableMessageHandler<TCommand, Unit>
    where TCommandHandler : ICancelableCommandHandler<TCommand>
    where TCommand : ICommand
    {
        private readonly TCommandHandler handler;

        public CancelableCommandHandlerShim(TCommandHandler handler)
        {
            this.handler = handler;
        }

        public async Task<Unit> Handle(TCommand message, CancellationToken cancellation)
        {
            var task = handler.Handle(message, cancellation);
            if (task == null) {
                throw new NullReferenceException($"The command handler {typeof(TCommandHandler).ToString()} returned null, expected a Task");
            } else {
                await task;
            }
            return Unit.Unit;
        }
    }

    internal class CancelableEventHandlerShim<TEvent, TEventHandler> : ICancelableMessageHandler<TEvent, Unit>
        where TEventHandler : ICancelableEventHandler<TEvent>
        where TEvent : IEvent
    {
        private readonly TEventHandler handler;

        public CancelableEventHandlerShim(TEventHandler handler)
        {
            this.handler = handler;
        }

        public async Task<Unit> Handle(TEvent message, CancellationToken cancellation)
        {
            var task = handler.Handle(message, cancellation);
            if (task == null) {
                throw new NullReferenceException($"The event handler {typeof(TEventHandler).ToString()} returned null, expected a Task");
            } else {
                await task;
            }
            return Unit.Unit;
        }
    }

    internal class CancelableQueryHandlerShim<TQuery, TResult, TQueryHandler> : ICancelableMessageHandler<TQuery, TResult>
        where TQueryHandler : ICancelableQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TQuery, TResult>
    {
        private readonly TQueryHandler handler;

        public CancelableQueryHandlerShim(TQueryHandler handler)
        {
            this.handler = handler;
        }

        public async Task<TResult> Handle(TQuery query, CancellationToken cancellation)
        {
            var task = handler.Handle(query, cancellation);
            if (task == null) {
                throw new NullReferenceException($"The query handler {typeof(TQueryHandler).ToString()} returned null, expected a Task");
            } else {
                return await task;
            }
        }
    }
}
