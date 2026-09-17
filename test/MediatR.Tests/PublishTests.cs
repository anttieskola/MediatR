using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class PublishTests
{
    public class Ping : INotification
    {
        public string? Message { get; set; }
    }

    public class PongHandler(TextWriter writer) : INotificationHandler<Ping>
    {
        public Task Handle(Ping notification, CancellationToken cancellationToken)
            => writer.WriteLineAsync(notification.Message + " Pong");
    }

    public class PungHandler(TextWriter writer) : INotificationHandler<Ping>
    {
        public Task Handle(Ping notification, CancellationToken cancellationToken)
            => writer.WriteLineAsync(notification.Message + " Pung");
    }

    [Fact]
    public async Task Should_resolve_main_handler()
    {
        StringBuilder builder = new();
        StringWriter writer = new(builder);

        ServiceCollection services = new();
        _ = services.AddSingleton<TextWriter>(writer);
        _ = services.AddSingleton<INotificationHandler<Ping>, PongHandler>();
        _ = services.AddSingleton<INotificationHandler<Ping>, PungHandler>();
        _ = services.AddSingleton<IMediator>(sp =>
        {
            INotificationPublisher? pub = sp.GetService<INotificationPublisher>();
            return pub is null ? new Mediator(sp) : new Mediator(sp, pub);
        });
        _ = services.AddSingleton<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        var result = builder.ToString().Split([Environment.NewLine], StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
    }

    [Fact]
    public async Task Should_resolve_main_handler_when_object_is_passed()
    {
        StringBuilder builder = new();
        StringWriter writer = new(builder);

        ServiceCollection services = new();
        _ = services.AddSingleton<TextWriter>(writer);
        _ = services.AddSingleton<INotificationHandler<Ping>, PongHandler>();
        _ = services.AddSingleton<INotificationHandler<Ping>, PungHandler>();
        _ = services.AddSingleton<IMediator>(sp =>
        {
            INotificationPublisher? pub = sp.GetService<INotificationPublisher>();
            return pub is null ? new Mediator(sp) : new Mediator(sp, pub);
        });
        _ = services.AddSingleton<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        object message = new Ping { Message = "Ping" };
        await mediator.Publish(message, TestContext.Current.CancellationToken);

        var result = builder.ToString().Split([Environment.NewLine], StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
    }

    public class SequentialMediator(IServiceProvider serviceProvider)
        : Mediator(serviceProvider)
    {
        protected override async Task PublishCore(IEnumerable<NotificationHandlerExecutor> allHandlers, INotification notification, CancellationToken cancellationToken)
        {
            foreach (NotificationHandlerExecutor handler in allHandlers)
            {
                await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public class SequentialPublisher : INotificationPublisher
    {
        public int CallCount { get; set; }

        public async Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
        {
            foreach (NotificationHandlerExecutor handler in handlerExecutors)
            {
                await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
                CallCount++;
            }
        }
    }

    [Fact]
    public async Task Should_override_with_sequential_firing()
    {
        StringBuilder builder = new();
        StringWriter writer = new(builder);

        ServiceCollection services = new();
        _ = services.AddSingleton<TextWriter>(writer);
        _ = services.AddSingleton<INotificationHandler<Ping>, PongHandler>();
        _ = services.AddSingleton<INotificationHandler<Ping>, PungHandler>();
        _ = services.AddSingleton<IMediator>(sp => new SequentialMediator(sp));
        _ = services.AddSingleton<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        var result = builder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
    }

    [Fact]
    public async Task Should_override_with_sequential_firing_through_injection()
    {
        StringBuilder builder = new();
        StringWriter writer = new(builder);
        SequentialPublisher publisher = new();

        ServiceCollection services = new();
        _ = services.AddSingleton<TextWriter>(writer);
        _ = services.AddSingleton<INotificationHandler<Ping>, PongHandler>();
        _ = services.AddSingleton<INotificationHandler<Ping>, PungHandler>();
        _ = services.AddSingleton<INotificationPublisher>(publisher);
        _ = services.AddSingleton<IMediator>(sp =>
        {
            INotificationPublisher? pub = sp.GetService<INotificationPublisher>();
            return pub is null ? new Mediator(sp) : new Mediator(sp, pub);
        });
        _ = services.AddSingleton<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        var result = builder.ToString().Split([Environment.NewLine], StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
        publisher.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_resolve_handlers_given_interface()
    {
        StringBuilder builder = new();
        StringWriter writer = new(builder);

        ServiceCollection services = new();
        _ = services.AddSingleton<TextWriter>(writer);
        _ = services.AddSingleton<INotificationHandler<Ping>, PongHandler>();
        _ = services.AddSingleton<INotificationHandler<Ping>, PungHandler>();
        _ = services.AddSingleton<IMediator>(sp => new SequentialMediator(sp));
        _ = services.AddSingleton<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        // wrap notifications in an array, so this test won't break on a 'replace with var' refactoring
        INotification[] notifications = new INotification[] { new Ping { Message = "Ping" } };
        await mediator.Publish(notifications[0], TestContext.Current.CancellationToken);

        var result = builder.ToString().Split([Environment.NewLine], StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
    }

    [Fact]
    public async Task Should_resolve_main_handler_by_specific_interface()
    {
        StringBuilder builder = new();
        StringWriter writer = new(builder);

        ServiceCollection services = new();
        _ = services.AddSingleton<TextWriter>(writer);
        _ = services.AddSingleton<INotificationHandler<Ping>, PongHandler>();
        _ = services.AddSingleton<INotificationHandler<Ping>, PungHandler>();
        _ = services.AddSingleton<IMediator>(sp =>
        {
            INotificationPublisher? pub = sp.GetService<INotificationPublisher>();
            return pub is null ? new Mediator(sp) : new Mediator(sp, pub);
        });
        _ = services.AddSingleton<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();
        IPublisher mediator = provider.GetRequiredService<IPublisher>();

        await mediator.Publish(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        var result = builder.ToString().Split([Environment.NewLine], StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
    }
}