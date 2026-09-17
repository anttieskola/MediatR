using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class SendTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dependency _dependency;
    private readonly IMediator _mediator;

    public SendTests()
    {
        _dependency = new Dependency();
        ServiceCollection services = new();
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblies(typeof(Ping).Assembly);
            _ = cfg.AddOpenBehavior(typeof(TimeoutBehavior<,>), ServiceLifetime.Transient);
            cfg.RegisterGenericHandlers = true;
        });
        _ = services.AddSingleton(_dependency);
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetService<IMediator>()!;
    }

    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class VoidPing : IRequest;

    public class Pong
    {
        public string? Message { get; set; }
    }

    public class PingHandler : IRequestHandler<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
            => Task.FromResult(new Pong { Message = request.Message + " Pong" });
    }

    public class Dependency
    {
        public bool Called { get; set; }
        public bool CalledSpecific { get; set; }
    }

    public class VoidPingHandler(SendTests.Dependency dependency) : IRequestHandler<VoidPing>
    {
        public Task Handle(VoidPing request, CancellationToken cancellationToken)
        {
            dependency.Called = true;

            return Task.CompletedTask;
        }
    }

    public class GenericPing<T> : IRequest<T>
        where T : Pong
    {
        public T? Pong { get; set; }
    }

    public class GenericPingHandler<T>(SendTests.Dependency dependency) : IRequestHandler<GenericPing<T>, T>
        where T : Pong
    {
        public Task<T> Handle(GenericPing<T> request, CancellationToken cancellationToken)
        {
            dependency.Called = true;
            request.Pong!.Message += " Pong";
            return Task.FromResult(request.Pong!);
        }
    }

    public class VoidGenericPing<T> : IRequest
        where T : Pong;

    public class VoidGenericPingHandler<T>(SendTests.Dependency dependency) : IRequestHandler<VoidGenericPing<T>>
        where T : Pong
    {
        public Task Handle(VoidGenericPing<T> request, CancellationToken cancellationToken)
        {
            dependency.Called = true;

            return Task.CompletedTask;
        }
    }

    public class PongExtension : Pong;

    public class TestClass1PingRequestHandler(SendTests.Dependency dependency) : IRequestHandler<VoidGenericPing<PongExtension>>
    {
        public Task Handle(VoidGenericPing<PongExtension> request, CancellationToken cancellationToken)
        {
            dependency.CalledSpecific = true;
            return Task.CompletedTask;
        }
    }

    public interface ITestInterface1 { }
    public interface ITestInterface2 { }
    public interface ITestInterface3 { }

    public class TestClass1 : ITestInterface1 { }
    public class TestClass2 : ITestInterface2 { }
    public class TestClass3 : ITestInterface3 { }

    public class MultipleGenericTypeParameterRequest<T1, T2, T3> : IRequest<int>
       where T1 : ITestInterface1
       where T2 : ITestInterface2
       where T3 : ITestInterface3
    {
        public int Foo { get; set; }
    }

    public class MultipleGenericTypeParameterRequestHandler<T1, T2, T3>(SendTests.Dependency dependency)
        : IRequestHandler<MultipleGenericTypeParameterRequest<T1, T2, T3>, int>
        where T1 : ITestInterface1
        where T2 : ITestInterface2
        where T3 : ITestInterface3
    {
        public Task<int> Handle(MultipleGenericTypeParameterRequest<T1, T2, T3> request, CancellationToken cancellationToken)
        {
            dependency.Called = true;
            return Task.FromResult(1);
        }
    }

    public class TimeoutBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            using CancellationTokenSource cts = new(500);
            return await next(cts.Token);
        }
    }

    public class TimeoutRequest : IRequest;

    public class TimeoutRequest2 : IRequest<int>;

    public class TimeoutRequestHandler(SendTests.Dependency dependency) : IRequestHandler<TimeoutRequest>
    {
        public async Task Handle(TimeoutRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(2000, cancellationToken);

            dependency.Called = true;
        }
    }

    public class TimeoutRequest2Handler(SendTests.Dependency dependency) : IRequestHandler<TimeoutRequest2, int>
    {
        public async Task<int> Handle(TimeoutRequest2 request, CancellationToken cancellationToken)
        {
            await Task.Delay(2000, cancellationToken);

            dependency.Called = true;
            return 1;
        }
    }

    [Fact]
    public async Task Should_resolve_main_handler()
    {
        Pong response = await _mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong");
    }

    [Fact]
    public async Task Should_resolve_main_void_handler()
    {
        await _mediator.Send(new VoidPing(), TestContext.Current.CancellationToken);

        _dependency.Called.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_resolve_main_handler_via_dynamic_dispatch()
    {
        object request = new Ping { Message = "Ping" };
        object? response = await _mediator.Send(request, TestContext.Current.CancellationToken);

        Pong pong = response.ShouldBeOfType<Pong>();
        pong.Message.ShouldBe("Ping Pong");
    }

    [Fact]
    public async Task Should_resolve_main_void_handler_via_dynamic_dispatch()
    {
        object request = new VoidPing();
        object? response = await _mediator.Send(request, TestContext.Current.CancellationToken);

        _ = response.ShouldBeOfType<Unit>();

        _dependency.Called.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_resolve_main_handler_by_specific_interface()
    {
        Pong response = await _mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong");
    }

    [Fact]
    public async Task Should_resolve_main_handler_by_given_interface()
    {
        // wrap requests in an array, so this test won't break on a 'replace with var' refactoring
        IRequest[] requests = new IRequest[] { new VoidPing() };
        await _mediator.Send(requests[0], TestContext.Current.CancellationToken);

        _dependency.Called.ShouldBeTrue();
    }

    [Fact]
    public Task Should_raise_execption_on_null_request() => Should.ThrowAsync<ArgumentNullException>(async () => await _mediator.Send(default!));

    [Fact]
    public async Task Should_resolve_generic_handler()
    {
        GenericPing<Pong> request = new()
        { Pong = new Pong { Message = "Ping" } };
        Pong result = await _mediator.Send(request, TestContext.Current.CancellationToken);

        Pong pong = result.ShouldBeOfType<Pong>();
        pong.Message.ShouldBe("Ping Pong");

        _dependency.Called.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_resolve_generic_void_handler()
    {
        VoidGenericPing<Pong> request = new();
        await _mediator.Send(request, TestContext.Current.CancellationToken);

        _dependency.Called.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_resolve_multiple_type_parameter_generic_handler()
    {
        MultipleGenericTypeParameterRequest<TestClass1, TestClass2, TestClass3> request = new();
        _ = await _mediator.Send(request, TestContext.Current.CancellationToken);

        _dependency.Called.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_resolve_closed_handler_if_defined()
    {
        Dependency dependency = new();
        ServiceCollection services = new();
        _ = services.AddSingleton(dependency);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
            cfg.RegisterGenericHandlers = true;
        });

        _ = services.AddTransient<IRequestHandler<VoidGenericPing<PongExtension>>, TestClass1PingRequestHandler>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMediator mediator = serviceProvider.GetService<IMediator>()!;

        VoidGenericPing<PongExtension> request = new();
        await mediator.Send(request, TestContext.Current.CancellationToken);

        dependency.Called.ShouldBeFalse();
        dependency.CalledSpecific.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_resolve_open_handler_if_not_defined()
    {
        Dependency dependency = new();
        ServiceCollection services = new();
        _ = services.AddSingleton(dependency);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
            cfg.RegisterGenericHandlers = true;
        });
        _ = services.AddTransient<IRequestHandler<VoidGenericPing<PongExtension>>, TestClass1PingRequestHandler>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMediator mediator = serviceProvider.GetService<IMediator>()!;

        VoidGenericPing<Pong> request = new();
        await mediator.Send(request, TestContext.Current.CancellationToken);

        dependency.Called.ShouldBeTrue();
        dependency.CalledSpecific.ShouldBeFalse();
    }

    [Fact]
    public async Task TimeoutBehavior_Void_Should_Cancel_Long_Running_Task_And_Throw_Exception()
    {
        TimeoutRequest request = new();

        TaskCanceledException exception = await Should.ThrowAsync<TaskCanceledException>(() => _mediator.Send(request));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeAssignableTo<TaskCanceledException>();
        _dependency.Called.ShouldBeFalse();
    }

    [Fact]
    public async Task TimeoutBehavior_NonVoid_Should_Cancel_Long_Running_Task_And_Throw_Exception()
    {
        TimeoutRequest2 request = new();
        int result = 0;

        TaskCanceledException exception = await Should.ThrowAsync<TaskCanceledException>(async () => { result = await _mediator.Send(request); });

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeAssignableTo<TaskCanceledException>();
        _dependency.Called.ShouldBeFalse();
        result.ShouldBe(0);
    }
}