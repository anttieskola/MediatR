using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests.MicrosoftExtensionsDI;

public class PipelineTests
{
    public class OuterBehavior(Logger output) : IPipelineBehavior<Ping, Pong>
    {
        public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Outer before");
            Pong response = await next(cancellationToken);
            output.Messages.Add("Outer after");

            return response;
        }
    }

    public class InnerBehavior(Logger output) : IPipelineBehavior<Ping, Pong>
    {
        public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner before");
            Pong response = await next(cancellationToken);
            output.Messages.Add("Inner after");

            return response;
        }
    }

    public class OuterStreamBehavior(Logger output) : IStreamPipelineBehavior<Ping, Pong>
    {
        public async IAsyncEnumerable<Pong> Handle(Ping request, StreamHandlerDelegate<Pong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Outer before");
            await foreach (Pong? item in next().WithCancellation(cancellationToken))
            {
                yield return item;
            }
            output.Messages.Add("Outer after");
        }
    }

    public class InnerStreamBehavior(Logger output) : IStreamPipelineBehavior<Ping, Pong>
    {
        public async IAsyncEnumerable<Pong> Handle(Ping request, StreamHandlerDelegate<Pong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner before");
            await foreach (Pong? item in next().WithCancellation(cancellationToken))
            {
                yield return item;
            }
            output.Messages.Add("Inner after");
        }
    }

    public class InnerBehavior<TRequest, TResponse>(Logger output) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner generic before");
            TResponse? response = await next(cancellationToken);
            output.Messages.Add("Inner generic after");

            return response;
        }
    }

    public class OuterBehavior<TRequest, TResponse>(Logger output) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Outer generic before");
            TResponse? response = await next(cancellationToken);
            output.Messages.Add("Outer generic after");

            return response;
        }
    }

    public class ConstrainedBehavior<TRequest, TResponse>(Logger output) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
        where TResponse : Pong
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Constrained before");
            TResponse response = await next(cancellationToken);
            output.Messages.Add("Constrained after");

            return response;
        }
    }

    public class FirstPreProcessor<TRequest>(Logger output) : IRequestPreProcessor<TRequest> where TRequest : notnull
    {
        public Task Process(TRequest request, CancellationToken cancellationToken)
        {
            output.Messages.Add("First pre processor");
            return Task.FromResult(0);
        }
    }

    public class FirstConcretePreProcessor(Logger output) : IRequestPreProcessor<Ping>
    {
        public Task Process(Ping request, CancellationToken cancellationToken)
        {
            output.Messages.Add("First concrete pre processor");
            return Task.FromResult(0);
        }
    }

    public class NextPreProcessor<TRequest>(Logger output) : IRequestPreProcessor<TRequest> where TRequest : notnull
    {
        public Task Process(TRequest request, CancellationToken cancellationToken)
        {
            output.Messages.Add("Next pre processor");
            return Task.FromResult(0);
        }
    }

    public class NextConcretePreProcessor(Logger output) : IRequestPreProcessor<Ping>
    {
        public Task Process(Ping request, CancellationToken cancellationToken)
        {
            output.Messages.Add("Next concrete pre processor");
            return Task.FromResult(0);
        }
    }

    public class FirstPostProcessor<TRequest, TResponse>(Logger output) : IRequestPostProcessor<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
        {
            output.Messages.Add("First post processor");
            return Task.FromResult(0);
        }
    }

    public class FirstConcretePostProcessor(Logger output) : IRequestPostProcessor<Ping, Pong>
    {
        public Task Process(Ping request, Pong response, CancellationToken cancellationToken)
        {
            output.Messages.Add("First concrete post processor");
            return Task.FromResult(0);
        }
    }

    public class NextPostProcessor<TRequest, TResponse>(Logger output) : IRequestPostProcessor<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly Logger _output = output;

        public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Next post processor");
            return Task.FromResult(0);
        }
    }

    public class NextConcretePostProcessor(Logger output) : IRequestPostProcessor<Ping, Pong>
    {
        public Task Process(Ping request, Pong response, CancellationToken cancellationToken)
        {
            output.Messages.Add("Next concrete post processor");
            return Task.FromResult(0);
        }
    }

    public class PingPongGenericExceptionAction(Logger output) : IRequestExceptionAction<Ping, Exception>
    {
        public Task Execute(Ping request, Exception exception, CancellationToken cancellationToken)
        {
            output.Messages.Add("Logging generic exception");

            return Task.CompletedTask;
        }
    }

    public class PingPongApplicationExceptionAction(Logger output) : IRequestExceptionAction<Ping, ApplicationException>
    {
        public Task Execute(Ping request, ApplicationException exception, CancellationToken cancellationToken)
        {
            output.Messages.Add("Logging ApplicationException exception");

            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionActionForType1(Logger output) : IRequestExceptionAction<Ping, SystemException>
    {
        public Task Execute(Ping request, SystemException exception, CancellationToken cancellationToken)
        {
            output.Messages.Add("Logging exception 1");

            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionActionForType2(Logger output) : IRequestExceptionAction<Ping, SystemException>
    {
        public Task Execute(Ping request, SystemException exception, CancellationToken cancellationToken)
        {
            output.Messages.Add("Logging exception 2");

            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionHandlerForType : IRequestExceptionHandler<Ping, Pong, ApplicationException>
    {
        public Task Handle(Ping request, ApplicationException exception, RequestExceptionHandlerState<Pong> state, CancellationToken cancellationToken)
        {
            state.SetHandled(new Pong { Message = exception.Message + " Handled by Specific Type" });

            return Task.CompletedTask;
        }
    }

    public class PingPongGenericExceptionHandler(Logger output) : IRequestExceptionHandler<Ping, Pong, Exception>
    {
        public Task Handle(Ping request, Exception exception, RequestExceptionHandlerState<Pong> state, CancellationToken cancellationToken)
        {
            output.Messages.Add(exception.Message + " Logged by Generic Type");

            return Task.CompletedTask;
        }
    }

    public class NotAnOpenBehavior : IPipelineBehavior<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken) => next();
    }

    public class ThrowingBehavior : IPipelineBehavior<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken) => throw new Exception(request.Message);
    }

    public class NotAnOpenStreamBehavior : IStreamPipelineBehavior<Ping, Pong>
    {
        public IAsyncEnumerable<Pong> Handle(Ping request, StreamHandlerDelegate<Pong> next, CancellationToken cancellationToken) => next();
    }

    public class OpenBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
    }

    public class OpenStreamBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
    }

    public class MultiOpenBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();

        public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
    }

    [Fact]
    public async Task Should_wrap_with_behavior()
    {
        Logger output = new();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            _ = cfg.AddBehavior<IPipelineBehavior<Ping, Pong>, OuterBehavior>();
            _ = cfg.AddBehavior<IPipelineBehavior<Ping, Pong>, InnerBehavior>();
        });
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        Pong response = await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong");

        output.Messages.ShouldBe(
        [
            "Outer before",
            "Inner before",
            "Handler",
            "Inner after",
            "Outer after"
        ]);
    }

    [Fact]
    public async Task Should_wrap_generics_with_behavior()
    {
        Logger output = new();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg =>
        {
            // Call these registration methods multiple times to prove we don't register a service if it is already registered
            for (int i = 0; i < 3; i++)
            {
                _ = cfg.AddOpenBehavior(typeof(OuterBehavior<,>));
                _ = cfg.AddOpenBehavior(typeof(InnerBehavior<,>));
                _ = cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            }
        });
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        Pong response = await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong");

        output.Messages.ShouldBe(
        [
            "Outer generic before",
            "Inner generic before",
            "Handler",
            "Inner generic after",
            "Outer generic after",
        ]);
    }

    [Fact]
    public async Task Should_register_pre_and_post_processors()
    {
        Logger output = new();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            _ = cfg.AddRequestPreProcessor<IRequestPreProcessor<Ping>, FirstConcretePreProcessor>();
            _ = cfg.AddRequestPreProcessor<IRequestPreProcessor<Ping>, NextConcretePreProcessor>();
            _ = cfg.AddOpenRequestPreProcessor(typeof(FirstPreProcessor<>));
            _ = cfg.AddOpenRequestPreProcessor(typeof(NextPreProcessor<>));
            _ = cfg.AddRequestPostProcessor<IRequestPostProcessor<Ping, Pong>, FirstConcretePostProcessor>();
            _ = cfg.AddRequestPostProcessor<IRequestPostProcessor<Ping, Pong>, NextConcretePostProcessor>();
            _ = cfg.AddOpenRequestPostProcessor(typeof(FirstPostProcessor<,>));
            _ = cfg.AddOpenRequestPostProcessor(typeof(NextPostProcessor<,>));
        });
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        Pong response = await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong");

        output.Messages.ShouldBe(
        [
            "First concrete pre processor",
            "Next concrete pre processor",
            "First pre processor",
            "Next pre processor",
            "Handler",
            "First concrete post processor",
            "Next concrete post processor",
            "First post processor",
            "Next post processor",
        ]);
    }

    [Fact]
    public async Task Should_pick_up_specific_exception_behaviors()
    {
        Logger output = new();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly));
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        Pong response = await mediator.Send(new Ping { Message = "Ping", ThrowAction = msg => throw new ApplicationException(msg.Message + " Thrown") }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Thrown Handled by Specific Type");
        output.Messages.ShouldNotContain("Logging ApplicationException exception");
    }

    [Fact]
    public void Should_pick_up_base_exception_behaviors()
    {
        Logger output = new();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly));
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        _ = Should.Throw<Exception>(async () => await mediator.Send(new Ping { Message = "Ping", ThrowAction = msg => throw new Exception(msg.Message + " Thrown") }));

        output.Messages.ShouldContain("Ping Thrown Logged by Generic Type");
        output.Messages.ShouldContain("Logging generic exception");
    }

    [Fact]
    public void Should_handle_exceptions_from_behaviors()
    {
        Logger output = new();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            _ = cfg.AddBehavior<ThrowingBehavior>();
        });
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        _ = Should.Throw<Exception>(async () => await mediator.Send(new Ping { Message = "Ping" }));

        output.Messages.ShouldContain("Ping Logged by Generic Type");
        output.Messages.ShouldContain("Logging generic exception");
    }

    [Fact]
    public void Should_pick_up_exception_actions()
    {
        Logger output = new();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly));
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        _ = Should.Throw<SystemException>(async () => await mediator.Send(new Ping { Message = "Ping", ThrowAction = msg => throw new SystemException(msg.Message + " Thrown") }));

        output.Messages.ShouldContain("Logging exception 1");
        output.Messages.ShouldContain("Logging exception 2");
    }

    [Fact]
    public async Task Should_handle_constrained_generics()
    {
        Logger output = new();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            _ = cfg.AddOpenBehavior(typeof(OuterBehavior<,>));
            _ = cfg.AddOpenBehavior(typeof(InnerBehavior<,>));
            _ = cfg.AddOpenBehavior(typeof(ConstrainedBehavior<,>));
            _ = cfg.AddRequestPreProcessor<IRequestPreProcessor<Ping>, FirstConcretePreProcessor>();
            _ = cfg.AddRequestPreProcessor<IRequestPreProcessor<Ping>, NextConcretePreProcessor>();
            _ = cfg.AddOpenRequestPreProcessor(typeof(FirstPreProcessor<>));
            _ = cfg.AddOpenRequestPreProcessor(typeof(NextPreProcessor<>));
            _ = cfg.AddRequestPostProcessor<IRequestPostProcessor<Ping, Pong>, FirstConcretePostProcessor>();
            _ = cfg.AddRequestPostProcessor<IRequestPostProcessor<Ping, Pong>, NextConcretePostProcessor>();
            _ = cfg.AddOpenRequestPostProcessor(typeof(FirstPostProcessor<,>));
            _ = cfg.AddOpenRequestPostProcessor(typeof(NextPostProcessor<,>));
        });
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        Pong response = await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong");

        output.Messages.ShouldBe(
        [
            "First concrete pre processor",
            "Next concrete pre processor",
            "First pre processor",
            "Next pre processor",
            "Outer generic before",
            "Inner generic before",
            "Constrained before",
            "Handler",
            "Constrained after",
            "Inner generic after",
            "Outer generic after",
            "First concrete post processor",
            "Next concrete post processor",
            "First post processor",
            "Next post processor"
        ]);

        output.Messages.Clear();

        Zong zingResponse = await mediator.Send(new Zing { Message = "Zing" }, TestContext.Current.CancellationToken);

        zingResponse.Message.ShouldBe("Zing Zong");

        output.Messages.ShouldBe(
        [
            "First pre processor",
            "Next pre processor",
            "Outer generic before",
            "Inner generic before",
            "Handler",
            "Inner generic after",
            "Outer generic after",
            "First post processor",
            "Next post processor"
        ]);
    }

    [Fact]
    public void Should_throw_when_adding_non_open_behavior()
        => Should.Throw<InvalidOperationException>(() => new MediatRServiceConfiguration().AddOpenBehavior(typeof(NotAnOpenBehavior)));

    [Fact]
    public void Should_throw_when_adding_non_open_stream_behavior()
        => Should.Throw<InvalidOperationException>(() => new MediatRServiceConfiguration().AddOpenBehavior(typeof(NotAnOpenStreamBehavior)));

    [Fact]
    public void Should_throw_when_adding_random_generic_type_as_open_behavior()
        => Should.Throw<InvalidOperationException>(() => new MediatRServiceConfiguration().AddOpenBehavior(typeof(List<string>)));

    [Fact]
    public void Should_handle_open_behavior_registration()
    {
        MediatRServiceConfiguration cfg = new();
        _ = cfg.AddOpenBehavior(typeof(OpenBehavior<,>));
        _ = cfg.AddOpenStreamBehavior(typeof(OpenStreamBehavior<,>));

        cfg.BehaviorsToRegister.Count.ShouldBe(1);
        cfg.StreamBehaviorsToRegister.Count.ShouldBe(1);

        cfg.BehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IPipelineBehavior<,>));
        cfg.BehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(OpenBehavior<,>));
        cfg.BehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.BehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.BehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);

        cfg.StreamBehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IStreamPipelineBehavior<,>));
        cfg.StreamBehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(OpenStreamBehavior<,>));
        cfg.StreamBehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);

        ServiceCollection services = new();

        _ = cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            _ = services.AddMediatR(cfg);
            _ = services.BuildServiceProvider();
        });
    }

    [Fact]
    public void Should_handle_inferred_behavior_registration()
    {
        MediatRServiceConfiguration cfg = new();
        _ = cfg.AddBehavior<InnerBehavior>();
        _ = cfg.AddBehavior(typeof(OuterBehavior));

        cfg.BehaviorsToRegister.Count.ShouldBe(2);

        cfg.BehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IPipelineBehavior<Ping, Pong>));
        cfg.BehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(InnerBehavior));
        cfg.BehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.BehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.BehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);
        cfg.BehaviorsToRegister[1].ServiceType.ShouldBe(typeof(IPipelineBehavior<Ping, Pong>));
        cfg.BehaviorsToRegister[1].ImplementationType.ShouldBe(typeof(OuterBehavior));
        cfg.BehaviorsToRegister[1].ImplementationFactory.ShouldBeNull();
        cfg.BehaviorsToRegister[1].ImplementationInstance.ShouldBeNull();
        cfg.BehaviorsToRegister[1].Lifetime.ShouldBe(ServiceLifetime.Transient);

        ServiceCollection services = new();

        _ = cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            _ = services.AddMediatR(cfg);
            _ = services.BuildServiceProvider();
        });
    }


    [Fact]
    public void Should_handle_inferred_stream_behavior_registration()
    {
        MediatRServiceConfiguration cfg = new();
        _ = cfg.AddStreamBehavior<InnerStreamBehavior>();
        _ = cfg.AddStreamBehavior(typeof(OuterStreamBehavior));

        cfg.StreamBehaviorsToRegister.Count.ShouldBe(2);

        cfg.StreamBehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IStreamPipelineBehavior<Ping, Pong>));
        cfg.StreamBehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(InnerStreamBehavior));
        cfg.StreamBehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);
        cfg.StreamBehaviorsToRegister[1].ServiceType.ShouldBe(typeof(IStreamPipelineBehavior<Ping, Pong>));
        cfg.StreamBehaviorsToRegister[1].ImplementationType.ShouldBe(typeof(OuterStreamBehavior));
        cfg.StreamBehaviorsToRegister[1].ImplementationFactory.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[1].ImplementationInstance.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[1].Lifetime.ShouldBe(ServiceLifetime.Transient);

        ServiceCollection services = new();

        _ = cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            _ = services.AddMediatR(cfg);
            _ = services.BuildServiceProvider();
        });
    }

    [Fact]
    public void Should_handle_inferred_pre_processor_registration()
    {
        MediatRServiceConfiguration cfg = new();
        _ = cfg.AddRequestPreProcessor<FirstConcretePreProcessor>();
        _ = cfg.AddRequestPreProcessor(typeof(NextConcretePreProcessor));

        cfg.RequestPreProcessorsToRegister.Count.ShouldBe(2);

        cfg.RequestPreProcessorsToRegister[0].ServiceType.ShouldBe(typeof(IRequestPreProcessor<Ping>));
        cfg.RequestPreProcessorsToRegister[0].ImplementationType.ShouldBe(typeof(FirstConcretePreProcessor));
        cfg.RequestPreProcessorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.RequestPreProcessorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.RequestPreProcessorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);
        cfg.RequestPreProcessorsToRegister[1].ServiceType.ShouldBe(typeof(IRequestPreProcessor<Ping>));
        cfg.RequestPreProcessorsToRegister[1].ImplementationType.ShouldBe(typeof(NextConcretePreProcessor));
        cfg.RequestPreProcessorsToRegister[1].ImplementationFactory.ShouldBeNull();
        cfg.RequestPreProcessorsToRegister[1].ImplementationInstance.ShouldBeNull();
        cfg.RequestPreProcessorsToRegister[1].Lifetime.ShouldBe(ServiceLifetime.Transient);

        ServiceCollection services = new();

        _ = cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            _ = services.AddMediatR(cfg);
            _ = services.BuildServiceProvider();
        });
    }

    [Fact]
    public void Should_handle_inferred_post_processor_registration()
    {
        MediatRServiceConfiguration cfg = new();
        _ = cfg.AddRequestPostProcessor<FirstConcretePostProcessor>();
        _ = cfg.AddRequestPostProcessor(typeof(NextConcretePostProcessor));

        cfg.RequestPostProcessorsToRegister.Count.ShouldBe(2);

        cfg.RequestPostProcessorsToRegister[0].ServiceType.ShouldBe(typeof(IRequestPostProcessor<Ping, Pong>));
        cfg.RequestPostProcessorsToRegister[0].ImplementationType.ShouldBe(typeof(FirstConcretePostProcessor));
        cfg.RequestPostProcessorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.RequestPostProcessorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.RequestPostProcessorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);
        cfg.RequestPostProcessorsToRegister[1].ServiceType.ShouldBe(typeof(IRequestPostProcessor<Ping, Pong>));
        cfg.RequestPostProcessorsToRegister[1].ImplementationType.ShouldBe(typeof(NextConcretePostProcessor));
        cfg.RequestPostProcessorsToRegister[1].ImplementationFactory.ShouldBeNull();
        cfg.RequestPostProcessorsToRegister[1].ImplementationInstance.ShouldBeNull();
        cfg.RequestPostProcessorsToRegister[1].Lifetime.ShouldBe(ServiceLifetime.Transient);

        ServiceCollection services = new();

        _ = cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            _ = services.AddMediatR(cfg);
            _ = services.BuildServiceProvider();
        });
    }

    [Fact]
    public void Should_handle_open_behaviors_registration_from_a_single_type()
    {
        MediatRServiceConfiguration cfg = new();
        _ = cfg.AddOpenBehavior(typeof(MultiOpenBehavior<,>), ServiceLifetime.Singleton);
        _ = cfg.AddOpenStreamBehavior(typeof(MultiOpenBehavior<,>), ServiceLifetime.Singleton);

        cfg.BehaviorsToRegister.Count.ShouldBe(1);
        cfg.StreamBehaviorsToRegister.Count.ShouldBe(1);

        cfg.BehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IPipelineBehavior<,>));
        cfg.BehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(MultiOpenBehavior<,>));
        cfg.BehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.BehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.BehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Singleton);

        cfg.StreamBehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IStreamPipelineBehavior<,>));
        cfg.StreamBehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(MultiOpenBehavior<,>));
        cfg.StreamBehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Singleton);

        ServiceCollection services = new();

        _ = cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            _ = services.AddMediatR(cfg);
            _ = services.BuildServiceProvider();
        });
    }

    [Fact]
    public void Should_auto_register_processors_when_configured_including_all_concrete_types()
    {
        MediatRServiceConfiguration cfg = new()
        {
            AutoRegisterRequestProcessors = true
        };

        Logger output = new();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);

        _ = cfg.RegisterServicesFromAssemblyContaining<Ping>();

        _ = services.AddMediatR(cfg);

        ServiceProvider provider = services.BuildServiceProvider();

        List<object?> preProcessors = provider.GetServices(typeof(IRequestPreProcessor<Ping>)).ToList();
        preProcessors.Count.ShouldBeGreaterThan(0);
        preProcessors.ShouldContain(p => p != null && p.GetType() == typeof(FirstConcretePreProcessor));
        preProcessors.ShouldContain(p => p != null && p.GetType() == typeof(NextConcretePreProcessor));

        List<object?> postProcessors = provider.GetServices(typeof(IRequestPostProcessor<Ping, Pong>)).ToList();
        postProcessors.Count.ShouldBeGreaterThan(0);
        postProcessors.ShouldContain(p => p != null && p.GetType() == typeof(FirstConcretePostProcessor));
        postProcessors.ShouldContain(p => p != null && p.GetType() == typeof(NextConcretePostProcessor));
    }


    public sealed record FooRequest : IRequest;

    public interface IBlogger<T>
    {
        IList<string> Messages { get; }
    }

    public class Blogger<T> : IBlogger<T>
    {
        private readonly Logger _logger;

        public Blogger(Logger logger) => _logger = logger;

        public IList<string> Messages => _logger.Messages;
    }

    public sealed class FooRequestHandler(PipelineTests.IBlogger<PipelineTests.FooRequestHandler> logger) : IRequestHandler<FooRequest>
    {
        public Task Handle(FooRequest request, CancellationToken cancellationToken)
        {
            logger.Messages.Add("Invoked Handler");
            return Task.CompletedTask;
        }
    }

    private sealed class ClosedBehavior(PipelineTests.IBlogger<PipelineTests.ClosedBehavior> logger) : IPipelineBehavior<FooRequest, Unit>
    {
        public Task<Unit> Handle(FooRequest request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
        {
            logger.Messages.Add("Invoked Closed Behavior");
            return next();
        }
    }

    private sealed class Open2Behavior<TRequest, TResponse>(PipelineTests.IBlogger<PipelineTests.Open2Behavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            logger.Messages.Add("Invoked Open Behavior");
            return next(cancellationToken);
        }
    }
    [Fact]
    public async Task Should_register_correctly()
    {
        ServiceCollection services = new();
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblyContaining<FooRequest>();
            _ = cfg.AddBehavior<ClosedBehavior>();
            _ = cfg.AddOpenBehavior(typeof(Open2Behavior<,>));
        });
        Logger logger = new();
        _ = services.AddSingleton(logger);
        _ = services.AddSingleton(new MediatR.Tests.PipelineTests.Logger());
        _ = services.AddSingleton(new MediatR.Tests.StreamPipelineTests.Logger());
        _ = services.AddSingleton(new MediatR.Tests.SendTests.Dependency());
        _ = services.AddSingleton<System.IO.TextWriter>(new System.IO.StringWriter());
        _ = services.AddTransient(typeof(IBlogger<>), typeof(Blogger<>));
        ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true
        });

        IMediator mediator = provider.GetRequiredService<IMediator>();
        FooRequest request = new();
        await mediator.Send(request, TestContext.Current.CancellationToken);

        logger.Messages.ShouldBe(
        [
            "Invoked Closed Behavior",
            "Invoked Open Behavior",
            "Invoked Handler",
        ]);
    }


    #region OpenBehaviorsForMultipleRegistration
    private sealed class OpenBehaviorMultipleRegistration0<TRequest, TResponse>(PipelineTests.IBlogger<PipelineTests.OpenBehaviorMultipleRegistration0<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            logger.Messages.Add("Invoked OpenBehaviorMultipleRegistration0");
            return next(cancellationToken);
        }
    }
    private sealed class OpenBehaviorMultipleRegistration1<TRequest, TResponse>(PipelineTests.IBlogger<PipelineTests.OpenBehaviorMultipleRegistration1<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            logger.Messages.Add("Invoked OpenBehaviorMultipleRegistration1");
            return next(cancellationToken);
        }
    }
    private sealed class OpenBehaviorMultipleRegistration2<TRequest, TResponse>(PipelineTests.IBlogger<PipelineTests.OpenBehaviorMultipleRegistration2<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            logger.Messages.Add("Invoked OpenBehaviorMultipleRegistration2");
            return next(cancellationToken);
        }
    }
    #endregion OpenBehaviorsForMultipleRegistration

    [Fact]
    public async Task Should_register_open_behaviors_correctly()
    {
        List<Type> behaviorTypeList =
        [
            typeof(OpenBehaviorMultipleRegistration0<,>),
            typeof(OpenBehaviorMultipleRegistration1<,>),
            typeof(OpenBehaviorMultipleRegistration2<,>)
        ];
        ServiceCollection services = new();
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblyContaining<FooRequest>();
            _ = cfg.AddOpenBehaviors(behaviorTypeList);
        });
        Logger logger = new();
        _ = services.AddSingleton(logger);
        _ = services.AddSingleton(new Tests.PipelineTests.Logger());
        _ = services.AddSingleton(new Tests.StreamPipelineTests.Logger());
        _ = services.AddSingleton(new SendTests.Dependency());
        _ = services.AddSingleton<System.IO.TextWriter>(new System.IO.StringWriter());
        _ = services.AddTransient(typeof(IBlogger<>), typeof(Blogger<>));
        ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true
        });

        IMediator mediator = provider.GetRequiredService<IMediator>();
        FooRequest request = new();
        await mediator.Send(request, TestContext.Current.CancellationToken);

        logger.Messages.ShouldBe(
        [
            "Invoked OpenBehaviorMultipleRegistration0",
            "Invoked OpenBehaviorMultipleRegistration1",
            "Invoked OpenBehaviorMultipleRegistration2",
            "Invoked Handler",
        ]);
    }
}