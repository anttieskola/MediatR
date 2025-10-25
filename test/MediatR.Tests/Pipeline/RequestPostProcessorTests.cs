using MediatR.NotificationPublishers;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests.Pipeline;

public class RequestPostProcessorTests
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

    public class PingPongPostProcessor : IRequestPostProcessor<Ping, Pong>
    {
        public Task Process(Ping request, Pong response, CancellationToken cancellationToken)
        {
            response.Message = response.Message + " " + request.Message;

            return Task.FromResult(0);
        }
    }

    [Fact]
    public async Task Should_run_postprocessors()
    {
        var services = new ServiceCollection();

        // Register handler and post-processor
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddTransient<IRequestPostProcessor<Ping, Pong>, PingPongPostProcessor>();

        // Register the pipeline behavior (post-processor behavior)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));

        // Register MediatR components required by Mediator
        services.AddTransient<INotificationPublisher, ForeachAwaitPublisher>();
        services.AddTransient<IMediator, Mediator>();

        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

        response.Message.ShouldBe("Ping Pong Ping");
    }

}