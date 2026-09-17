using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests.Pipeline;

public class RequestExceptionActionTests
{
    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public abstract class PingPongException(string? message) : Exception(message + " Thrown")
    {
    }

    public class PingException(string? message) : PingPongException(message)
    {
    }

    public class PongException(string message) : PingPongException(message)
    {
    }

    public class PingHandler : IRequestHandler<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
            => throw new PingException(request.Message);
    }

    public class GenericExceptionAction<TRequest> : IRequestExceptionAction<TRequest, Exception> where TRequest : notnull
    {
        public int ExecutionCount { get; private set; }

        public Task Execute(TRequest request, Exception exception, CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionAction<TRequest> : IRequestExceptionAction<TRequest, PingPongException> where TRequest : notnull
    {
        public bool Executed { get; private set; }

        public Task Execute(TRequest request, PingPongException exception, CancellationToken cancellationToken)
        {
            Executed = true;
            return Task.CompletedTask;
        }
    }

    public class PingExceptionAction : IRequestExceptionAction<Ping, PingException>
    {
        public bool Executed { get; private set; }

        public Task Execute(Ping request, PingException exception, CancellationToken cancellationToken)
        {
            Executed = true;
            return Task.CompletedTask;
        }
    }

    public class PongExceptionAction : IRequestExceptionAction<Ping, PongException>
    {
        public bool Executed { get; private set; }

        public Task Execute(Ping request, PongException exception, CancellationToken cancellationToken)
        {
            Executed = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Should_run_all_exception_actions_that_match_base_type()
    {
        PingExceptionAction pingExceptionAction = new();
        PongExceptionAction pongExceptionAction = new();
        PingPongExceptionAction<Ping> pingPongExceptionAction = new();

        ServiceCollection services = new();

        // Register MediatR core services and (optionally) assembly scanning
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly));

        // Register handler and the exception action instances
        _ = services.AddSingleton<IRequestExceptionAction<Ping, PingException>>(pingExceptionAction);
        _ = services.AddSingleton<IRequestExceptionAction<Ping, PongException>>(pongExceptionAction);
        _ = services.AddSingleton<IRequestExceptionAction<Ping, PingPongException>>(pingPongExceptionAction);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Ping request = new() { Message = "Ping!" };
        _ = await Assert.ThrowsAsync<PingException>(() => mediator.Send(request, TestContext.Current.CancellationToken));

        pingExceptionAction.Executed.ShouldBeTrue();
        pingPongExceptionAction.Executed.ShouldBeTrue();
        pongExceptionAction.Executed.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_run_matching_exception_actions_only_once()
    {
        GenericExceptionAction<Ping> genericExceptionAction = new();

        ServiceCollection services = new();

        _ = services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddSingleton<IRequestExceptionAction<Ping, Exception>>(genericExceptionAction);

        // Ensure the RequestExceptionActionProcessorBehavior is registered in the DI container
        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionActionProcessorBehavior<,>));

        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Ping request = new() { Message = "Ping!" };
        _ = await Assert.ThrowsAsync<PingException>(() => mediator.Send(request, TestContext.Current.CancellationToken));

        genericExceptionAction.ExecutionCount.ShouldBe(1);
    }
}