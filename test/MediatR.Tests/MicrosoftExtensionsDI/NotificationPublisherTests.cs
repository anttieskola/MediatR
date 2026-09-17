using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests.MicrosoftExtensionsDI;

public class NotificationPublisherTests
{
    public class MockPublisher : INotificationPublisher
    {
        public int CallCount { get; set; }

        public async Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
        {
            foreach (NotificationHandlerExecutor handlerExecutor in handlerExecutors)
            {
                await handlerExecutor.HandlerCallback(notification, cancellationToken);
                CallCount++;
            }
        }
    }

    [Fact]
    public void ShouldResolveDefaultPublisher()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton(new Logger());
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests));
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator? mediator = provider.GetService<IMediator>();

        _ = mediator.ShouldNotBeNull();

        INotificationPublisher? publisher = provider.GetService<INotificationPublisher>();

        _ = publisher.ShouldNotBeNull();
    }

    [Fact]
    public async Task ShouldSubstitutePublisherInstance()
    {
        MockPublisher publisher = new();
        ServiceCollection services = new();
        _ = services.AddSingleton(new Logger());
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblyContaining<CustomMediatorTests>();
            cfg.NotificationPublisher = publisher;
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator? mediator = provider.GetService<IMediator>();

        _ = mediator.ShouldNotBeNull();

        await mediator.Publish(new Pinged(), TestContext.Current.CancellationToken);

        publisher.CallCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ShouldSubstitutePublisherServiceType()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton(new Logger());
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests));
            cfg.NotificationPublisherType = typeof(MockPublisher);
            cfg.Lifetime = ServiceLifetime.Singleton;
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator? mediator = provider.GetService<IMediator>();
        INotificationPublisher? publisher = provider.GetService<INotificationPublisher>();

        _ = mediator.ShouldNotBeNull();
        _ = publisher.ShouldNotBeNull();

        await mediator.Publish(new Pinged(), TestContext.Current.CancellationToken);

        MockPublisher mock = publisher.ShouldBeOfType<MockPublisher>();

        mock.CallCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ShouldSubstitutePublisherServiceTypeWithWhenAll()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton(new Logger());
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblyContaining<CustomMediatorTests>();
            cfg.NotificationPublisherType = typeof(TaskWhenAllPublisher);
            cfg.Lifetime = ServiceLifetime.Singleton;
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator? mediator = provider.GetService<IMediator>();
        INotificationPublisher? publisher = provider.GetService<INotificationPublisher>();

        _ = mediator.ShouldNotBeNull();
        _ = publisher.ShouldNotBeNull();

        await Should.NotThrowAsync(mediator.Publish(new Pinged(), TestContext.Current.CancellationToken));

        _ = publisher.ShouldBeOfType<TaskWhenAllPublisher>();
    }
}