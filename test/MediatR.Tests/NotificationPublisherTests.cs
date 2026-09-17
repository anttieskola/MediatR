using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class NotificationPublisherTests
{
    public class Notification : INotification;

    public class FirstHandler : INotificationHandler<Notification>
    {
        public async Task Handle(Notification notification, CancellationToken cancellationToken)
            => await Task.Delay(500, cancellationToken);
    }
    public class SecondHandler : INotificationHandler<Notification>
    {
        public async Task Handle(Notification notification, CancellationToken cancellationToken)
            => await Task.Delay(250, cancellationToken);
    }

    [Fact]
    public async Task Should_handle_sequentially_by_default()
    {
        ServiceCollection services = new();
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblyContaining<Notification>();
        });
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        Stopwatch timer = new();
        timer.Start();

        await mediator.Publish(new Notification(), TestContext.Current.CancellationToken);

        timer.Stop();

        var sequentialElapsed = timer.ElapsedMilliseconds;

        services = new ServiceCollection();
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblyContaining<Notification>();
            cfg.NotificationPublisherType = typeof(TaskWhenAllPublisher);
        });
        serviceProvider = services.BuildServiceProvider();

        mediator = serviceProvider.GetRequiredService<IMediator>();

        timer.Restart();

        await mediator.Publish(new Notification(), TestContext.Current.CancellationToken);

        timer.Stop();

        var parallelElapsed = timer.ElapsedMilliseconds;

        sequentialElapsed.ShouldBeGreaterThan(parallelElapsed);
    }
}