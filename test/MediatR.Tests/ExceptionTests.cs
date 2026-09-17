using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class ExceptionTests
{
    private readonly IMediator _mediator;

    public class Ping : IRequest<Pong>;

    public class Pong;

    public class VoidPing : IRequest;

    public class Pinged : INotification;

    public class AsyncPing : IRequest<Pong>;

    public class AsyncVoidPing : IRequest;

    public class AsyncPinged : INotification;

    public class NullPing : IRequest<Pong>;

    public class VoidNullPing : IRequest;

    public class NullPinged : INotification;

    public class NullPingHandler : IRequestHandler<NullPing, Pong>
    {
        public Task<Pong> Handle(NullPing request, CancellationToken cancellationToken)
            => Task.FromResult(new Pong());
    }

    public class VoidNullPingHandler : IRequestHandler<VoidNullPing>
    {
        public Task Handle(VoidNullPing request, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    public ExceptionTests()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Should_throw_for_send()
        => await Should.ThrowAsync<InvalidOperationException>(async () => await _mediator.Send(new Ping()));

    [Fact]
    public async Task Should_throw_for_void_send()
        => await Should.ThrowAsync<InvalidOperationException>(async () => await _mediator.Send(new VoidPing()));

    [Fact]
    public async Task Should_not_throw_for_publish()
    {
        Exception ex = null!;
        try
        {
            await _mediator.Publish(new Pinged(), TestContext.Current.CancellationToken);
        }
        catch (Exception e)
        {
            ex = e;
        }
        ex.ShouldBeNull();
    }

    [Fact]
    public async Task Should_throw_for_async_send()
        => await Should.ThrowAsync<InvalidOperationException>(async () => await _mediator.Send(new AsyncPing()));

    [Fact]
    public async Task Should_throw_for_async_void_send()
        => await Should.ThrowAsync<InvalidOperationException>(async () => await _mediator.Send(new AsyncVoidPing()));

    [Fact]
    public async Task Should_not_throw_for_async_publish()
    {
        Exception ex = null!;
        try
        {
            await _mediator.Publish(new AsyncPinged(), TestContext.Current.CancellationToken);
        }
        catch (Exception e)
        {
            ex = e;
        }
        ex.ShouldBeNull();
    }

    [Fact]
    public async Task Should_throw_argument_exception_for_send_when_request_is_null()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IRequestHandler<NullPing, Pong>, NullPingHandler>();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        NullPing request = null!;

        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await mediator.Send(request));
    }

    [Fact]
    public async Task Should_throw_argument_exception_for_void_send_when_request_is_null()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IRequestHandler<VoidNullPing>, VoidNullPingHandler>();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        VoidNullPing request = null!;

        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await mediator.Send(request));
    }

    [Fact]
    public async Task Should_throw_argument_exception_for_publish_when_request_is_null()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        NullPinged notification = null!;

        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await mediator.Publish(notification));
    }

    [Fact]
    public async Task Should_throw_argument_exception_for_publish_when_request_is_null_object()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        object notification = null!;

        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await mediator.Publish(notification));
    }

    [Fact]
    public async Task Should_throw_argument_exception_for_publish_when_request_is_not_notification()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        object notification = "totally not notification";

        _ = await Should.ThrowAsync<ArgumentException>(async () => await mediator.Publish(notification));
    }

    public class PingException : IRequest
    {

    }

    public class PingExceptionHandler : IRequestHandler<PingException>
    {
        public Task Handle(PingException request, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    [Fact]
    public async Task Should_throw_exception_for_non_generic_send_when_exception_occurs()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IRequestHandler<PingException>, PingExceptionHandler>();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        object pingException = new PingException();

        _ = await Should.ThrowAsync<NotImplementedException>(async () => await mediator.Send(pingException));
    }

    [Fact]
    public async Task Should_throw_exception_for_non_request_send()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        object nonRequest = new NonRequest();

        ArgumentException argumentException = await Should.ThrowAsync<ArgumentException>(async () => await mediator.Send(nonRequest));
        Assert.StartsWith("NonRequest does not implement IRequest", argumentException.Message);
    }

    public class NonRequest
    {

    }

    [Fact]
    public async Task Should_throw_exception_for_generic_send_when_exception_occurs()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IRequestHandler<PingException>, PingExceptionHandler>();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        PingException pingException = new();

        _ = await Should.ThrowAsync<NotImplementedException>(async () => await mediator.Send(pingException));
    }
}