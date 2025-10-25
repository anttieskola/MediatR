using System.Threading;
using MediatR.NotificationPublishers;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests.Pipeline;

public class RequestPreProcessorTests
{
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
        {
            return Task.FromResult(new Pong { Message = request.Message + " Pong" });
        }
    }

    public class PingPreProcessor : IRequestPreProcessor<Ping>
    {
        public Task Process(Ping request, CancellationToken cancellationToken)
        {
            request.Message = request.Message + " Ping";

            return Task.FromResult(0);
        }
    }

    [Fact]
    public async Task Should_run_preprocessors()
    {
        var services = new ServiceCollection();
        // Register handlers and preprocessor (register by interface)
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddTransient<IRequestPreProcessor<Ping>, PingPreProcessor>();
        // Register the pipeline behavior (preprocessor behavior)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>));

        // Register MediatR components.
        // Note: The mediator's constructor requires an IServiceProvider and a notification publisher.
        // In this example, we register a basic publisher implementation.
        services.AddTransient<INotificationPublisher, ForeachAwaitPublisher>();
        services.AddTransient<IMediator, Mediator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new Ping { Message = "Ping" });
        response.Message.ShouldBe("Ping Ping Pong");
    }

}