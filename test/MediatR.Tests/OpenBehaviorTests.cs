using MediatR.Entities;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class OpenBehaviorTests
{
    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    private sealed class ClosedBehavior : IPipelineBehavior<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
            => next(cancellationToken);
    }

    private sealed class GenericBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            => next(cancellationToken);
    }

    private abstract class AbstractBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public abstract Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
    }

    // Implements IPipelineBehavior<,> only through an inherited generic base class.
    private sealed class DerivedBehavior : AbstractBehavior<Ping, Pong>
    {
        public override Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
            => next(cancellationToken);
    }

    private sealed class NotABehavior
    {
    }

    public static TheoryData<Type> ValidBehaviorTypes() =>
    [
        typeof(ClosedBehavior),
        typeof(GenericBehavior<,>),
        typeof(DerivedBehavior),
    ];

    public static TheoryData<Type> NonBehaviorTypes() =>
    [
        typeof(NotABehavior),
        typeof(string),
    ];

    public static TheoryData<ServiceLifetime> Lifetimes() =>
    [
        ServiceLifetime.Transient,
        ServiceLifetime.Scoped,
        ServiceLifetime.Singleton,
    ];

    [Theory]
    [MemberData(nameof(ValidBehaviorTypes))]
    public void ShouldStoreTypeAndLifetime_WhenTypeImplementsPipelineBehavior(Type behaviorType)
    {
        OpenBehavior behavior = new(behaviorType, ServiceLifetime.Scoped);

        behavior.OpenBehaviorType.ShouldBe(behaviorType);
        behavior.ServiceLifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void ShouldDefaultServiceLifetimeToTransient()
    {
        OpenBehavior behavior = new(typeof(ClosedBehavior));

        behavior.ServiceLifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Theory]
    [MemberData(nameof(Lifetimes))]
    public void ShouldStoreRequestedLifetime(ServiceLifetime serviceLifetime)
    {
        OpenBehavior behavior = new(typeof(ClosedBehavior), serviceLifetime);

        behavior.ServiceLifetime.ShouldBe(serviceLifetime);
    }

    [Fact]
    public void ShouldThrowArgumentNullException_WhenTypeIsNull()
    {
        static OpenBehavior act()
        {
            return new(null!);
        }

        ArgumentNullException exception = Should.Throw<ArgumentNullException>((Func<OpenBehavior>) act);

        exception.ParamName.ShouldBe("openBehaviorType");
        exception.Message.ShouldContain("Open behavior type can not be null.");
    }

    [Theory]
    [MemberData(nameof(NonBehaviorTypes))]
    public void ShouldThrowInvalidOperationException_WhenTypeDoesNotImplementPipelineBehavior(Type type)
    {
        OpenBehavior act()
        {
            return new(type);
        }

        InvalidOperationException exception = Should.Throw<InvalidOperationException>((Func<OpenBehavior>) act);

        exception.Message.ShouldContain($"The type \"{type.Name}\" must implement IPipelineBehavior<,> interface.");
    }
}
