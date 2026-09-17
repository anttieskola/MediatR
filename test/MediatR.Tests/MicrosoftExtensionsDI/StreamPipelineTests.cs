using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests.MicrosoftExtensionsDI;

public class StreamPipelineTests
{
    public class OuterBehavior(Logger output) : IStreamPipelineBehavior<StreamPing, Pong>
    {
        public async IAsyncEnumerable<Pong> Handle(StreamPing request, StreamHandlerDelegate<Pong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Outer before");
            await foreach (Pong? response in next().WithCancellation(cancellationToken))
            {
                yield return response;
            }
            output.Messages.Add("Outer after");
        }
    }

    public class InnerBehavior(Logger output) : IStreamPipelineBehavior<StreamPing, Pong>
    {
        public async IAsyncEnumerable<Pong> Handle(StreamPing request, StreamHandlerDelegate<Pong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner before");
            await foreach (Pong? response in next().WithCancellation(cancellationToken))
            {
                yield return response;
            }
            output.Messages.Add("Inner after");
        }
    }

    [Fact]
    public async Task Should_wrap_with_behavior()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddTransient<IStreamPipelineBehavior<StreamPing, Pong>, OuterBehavior>();
        _ = services.AddTransient<IStreamPipelineBehavior<StreamPing, Pong>, InnerBehavior>();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly));
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        IAsyncEnumerable<Pong> stream = mediator.CreateStream(new StreamPing { Message = "Ping" }, TestContext.Current.CancellationToken);

        await foreach (Pong? response in stream)
        {
            response.Message.ShouldBe("Ping Pang");
        }

        output.Messages.ShouldBe(
        [
            "Outer before",
            "Inner before",
            "Handler",
            "Inner after",
            "Outer after"
        ]);
    }

    [Fact]
    public async Task Should_register_and_wrap_with_behavior()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            _ = cfg.AddStreamBehavior<IStreamPipelineBehavior<StreamPing, Pong>, OuterBehavior>();
            _ = cfg.AddStreamBehavior<IStreamPipelineBehavior<StreamPing, Pong>, InnerBehavior>();
        });
        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        IAsyncEnumerable<Pong> stream = mediator.CreateStream(new StreamPing { Message = "Ping" }, TestContext.Current.CancellationToken);

        await foreach (Pong? response in stream)
        {
            response.Message.ShouldBe("Ping Pang");
        }

        output.Messages.ShouldBe(
        [
            "Outer before",
            "Inner before",
            "Handler",
            "Inner after",
            "Outer after"
        ]);
    }

}