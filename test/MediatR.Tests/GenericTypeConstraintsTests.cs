using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class GenericTypeConstraintsTests
{
    public interface IGenericTypeRequestHandlerTestClass<TRequest> where TRequest : IBaseRequest
    {
        Type[] Handle(TRequest request);
    }

    public abstract class GenericTypeRequestHandlerTestClass<TRequest> : IGenericTypeRequestHandlerTestClass<TRequest>
        where TRequest : IBaseRequest
    {
        public bool IsIRequest { get; }


        public bool IsIRequestT { get; }

        public bool IsIBaseRequest { get; }

        public GenericTypeRequestHandlerTestClass()
        {
            IsIRequest = typeof(IRequest).IsAssignableFrom(typeof(TRequest));
            IsIRequestT = typeof(TRequest).GetInterfaces()
                .Any(x => x.GetTypeInfo().IsGenericType &&
                          x.GetGenericTypeDefinition() == typeof(IRequest<>));

            IsIBaseRequest = typeof(IBaseRequest).IsAssignableFrom(typeof(TRequest));
        }

        public Type[] Handle(TRequest request)
            => typeof(TRequest).GetInterfaces();
    }

    public class GenericTypeConstraintPing : GenericTypeRequestHandlerTestClass<Ping>
    {

    }

    public class GenericTypeConstraintJing : GenericTypeRequestHandlerTestClass<Jing>
    {

    }

    public class Jing : IRequest
    {
        public string? Message { get; set; }
    }

    public class JingHandler : IRequestHandler<Jing>
    {
        public Task Handle(Jing request, CancellationToken cancellationToken) =>
            // empty handle
            Task.CompletedTask;
    }

    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public class PingHandler : IRequestHandler<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
            => Task.FromResult(new Pong { Message = request.Message + " Pong" });
    }

    private readonly IMediator _mediator;

    public GenericTypeConstraintsTests()
    {
        ServiceCollection services = new();

        // Register the concrete request handlers used in these tests
        _ = services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddSingleton<IRequestHandler<Jing>, JingHandler>();

        // Mediator expects an IServiceProvider; register it so tests can resolve IMediator/ISender
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Should_Resolve_Void_Return_Request()
    {
        // Create Request
        Jing jing = new() { Message = "Jing" };

        // Test mediator still works sending request
        await _mediator.Send(jing, TestContext.Current.CancellationToken);

        // Create new instance of type constrained class
        GenericTypeConstraintJing genericTypeConstraintsVoidReturn = new();

        // Assert it is of type IRequest and IRequest<T>
        Assert.True(genericTypeConstraintsVoidReturn.IsIRequest);
        Assert.False(genericTypeConstraintsVoidReturn.IsIRequestT);
        Assert.True(genericTypeConstraintsVoidReturn.IsIBaseRequest);

        // Verify it is of IRequest and IBaseRequest
        var results = genericTypeConstraintsVoidReturn.Handle(jing);

        Assert.Equal(2, results.Length);

        results.ShouldNotContain(typeof(IRequest<Unit>));
        results.ShouldContain(typeof(IBaseRequest));
        results.ShouldContain(typeof(IRequest));
    }

    [Fact]
    public async Task Should_Resolve_Response_Return_Request()
    {
        // Create Request
        Ping ping = new() { Message = "Ping" };

        // Test mediator still works sending request and gets response
        var pingResponse = await _mediator.Send(ping, TestContext.Current.CancellationToken);
        pingResponse.Message.ShouldBe("Ping Pong");

        // Create new instance of type constrained class
        GenericTypeConstraintPing genericTypeConstraintsResponseReturn = new();

        // Assert it is of type IRequest<T> but not IRequest
        Assert.False(genericTypeConstraintsResponseReturn.IsIRequest);
        Assert.True(genericTypeConstraintsResponseReturn.IsIRequestT);
        Assert.True(genericTypeConstraintsResponseReturn.IsIBaseRequest);

        // Verify it is of IRequest<Pong> and IBaseRequest, but not IRequest
        var results = genericTypeConstraintsResponseReturn.Handle(ping);

        Assert.Equal(2, results.Length);

        results.ShouldContain(typeof(IRequest<Pong>));
        results.ShouldContain(typeof(IBaseRequest));
        results.ShouldNotContain(typeof(IRequest));
    }
}