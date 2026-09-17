using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests.Pipeline;

public class RequestExceptionHandlerTests
{
    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public class PingException(string? message) : Exception(message + " Thrown")
    {
    }

    public class PingHandler : IRequestHandler<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
            => throw new PingException(request.Message);
    }

    public class GenericPingExceptionHandler : IRequestExceptionHandler<Ping, Pong, Exception>
    {
        public int ExecutionCount { get; private set; }

        public Task Handle(Ping request, Exception exception, RequestExceptionHandlerState<Pong> state, CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionHandlerForType : IRequestExceptionHandler<Ping, Pong, PingException>
    {
        public Task Handle(Ping request, PingException exception, RequestExceptionHandlerState<Pong> state, CancellationToken cancellationToken)
        {
            state.SetHandled(new Pong() { Message = exception.Message + " Handled by Type" });

            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionHandler : IRequestExceptionHandler<Ping, Pong, Exception>
    {
        public Task Handle(Ping request, Exception exception, RequestExceptionHandlerState<Pong> state, CancellationToken token)
        {
            state.SetHandled(new Pong() { Message = exception.Message + " Handled" });

            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionHandlerNotHandled : IRequestExceptionHandler<Ping, Pong, Exception>
    {
        public Task Handle(Ping request, Exception exception, RequestExceptionHandlerState<Pong> state, CancellationToken token)
        {
            request.Message = exception.Message + " Not Handled";

            return Task.CompletedTask;
        }
    }

    public class PingPongThrowingExceptionHandler : IRequestExceptionHandler<Ping, Pong, Exception>
    {
        public Task Handle(Ping request, Exception exception, RequestExceptionHandlerState<Pong> state, CancellationToken token)
            => throw new ApplicationException("Surprise!");
    }

    [Fact]
    public async Task Should_run_exception_handler_and_allow_for_exception_not_to_throw()
    {
        ServiceCollection services = new();

        // Register handler and exception handlers
        _ = services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddTransient<IRequestExceptionHandler<Ping, Pong, Exception>, PingPongExceptionHandler>();
        _ = services.AddTransient<IRequestExceptionHandler<Ping, Pong, PingException>, PingPongExceptionHandlerForType>();

        // Register the request-exception processor behavior (open generic)
        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));

        // Register mediator
        _ = services.AddTransient<IMediator, Mediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Thrown Handled by Type");
    }

    [Fact]
    public async Task Should_run_exception_handler_and_allow_for_exception_to_be_still_thrown()
    {
        ServiceCollection services = new();

        _ = services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddTransient<IRequestExceptionHandler<Ping, Pong, Exception>, PingPongExceptionHandlerNotHandled>();

        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));
        _ = services.AddTransient<IMediator, Mediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Ping request = new() { Message = "Ping" };
        _ = await Should.ThrowAsync<PingException>(async () =>
        {
            _ = await mediator.Send(request);
        });

        request.Message.ShouldBe("Ping Thrown Not Handled");
    }

    [Fact]
    public async Task Should_run_exception_handler_and_unwrap_expections_thrown_in_the_handler()
    {
        ServiceCollection services = new();

        _ = services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddTransient<IRequestExceptionHandler<Ping, Pong, Exception>, PingPongThrowingExceptionHandler>();

        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));
        _ = services.AddTransient<IMediator, Mediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Ping request = new() { Message = "Ping" };
        _ = await Should.ThrowAsync<ApplicationException>(async () =>
        {
            _ = await mediator.Send(request);
        });
    }

    [Fact]
    public async Task Should_run_matching_exception_handlers_only_once()
    {
        GenericPingExceptionHandler genericPingExceptionHandler = new();

        ServiceCollection services = new();

        _ = services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddSingleton<IRequestExceptionHandler<Ping, Pong, Exception>>(genericPingExceptionHandler);

        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));
        _ = services.AddTransient<IMediator, Mediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Ping request = new() { Message = "Ping" };
        _ = await Should.ThrowAsync<PingException>(async () =>
        {
            _ = await mediator.Send(request);
        });

        genericPingExceptionHandler.ExecutionCount.ShouldBe(1);
    }
}