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
            => Task.FromResult(new Pong { Message = request.Message + " Pong" });
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
        ServiceCollection services = new();

        // Register handler and post-processor
        _ = services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddTransient<IRequestPostProcessor<Ping, Pong>, PingPongPostProcessor>();

        // Register the pipeline behavior (post-processor behavior)
        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));

        // Register MediatR components required by Mediator
        _ = services.AddTransient<INotificationPublisher, ForeachAwaitPublisher>();
        _ = services.AddTransient<IMediator, Mediator>();

        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        Pong response = await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong Ping");
    }

}