using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class StreamPipelineTests
{
    public class Ping : IStreamRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public class Zing : IStreamRequest<Zong>
    {
        public string? Message { get; set; }
    }

    public class Zong
    {
        public string? Message { get; set; }
    }

    public class PingHandler : IStreamRequestHandler<Ping, Pong>
    {
        private readonly Logger _output;

        public PingHandler(Logger output) => _output = output;

        public async IAsyncEnumerable<Pong> Handle(Ping request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _output.Messages.Add("Handler");
            yield return await Task.FromResult(new Pong { Message = request.Message + " Pong" });
        }
    }

    public class ZingHandler(StreamPipelineTests.Logger output) : IStreamRequestHandler<Zing, Zong>
    {
        public async IAsyncEnumerable<Zong> Handle(Zing request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Handler");
            yield return await Task.FromResult(new Zong { Message = request.Message + " Zong" });
        }
    }

    public class OuterBehavior(StreamPipelineTests.Logger output) : IStreamPipelineBehavior<Ping, Pong>
    {
        public async IAsyncEnumerable<Pong> Handle(Ping request, StreamHandlerDelegate<Pong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Outer before");
            await foreach (Pong result in next())
            {
                yield return result;
            }
            output.Messages.Add("Outer after");
        }
    }

    public class InnerBehavior(StreamPipelineTests.Logger output) : IStreamPipelineBehavior<Ping, Pong>
    {
        public async IAsyncEnumerable<Pong> Handle(Ping request, StreamHandlerDelegate<Pong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner before");
            await foreach (Pong result in next())
            {
                yield return result;
            }
            output.Messages.Add("Inner after");
        }
    }

    public class InnerBehavior<TRequest, TResponse>(StreamPipelineTests.Logger output) : IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner generic before");
            await foreach (TResponse? result in next())
            {
                yield return result;
            }
            output.Messages.Add("Inner generic after");
        }
    }

    public class OuterBehavior<TRequest, TResponse>(StreamPipelineTests.Logger output) : IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Outer generic before");
            await foreach (TResponse? result in next())
            {
                yield return result;
            }
            output.Messages.Add("Outer generic after");
        }
    }

    public class ConstrainedBehavior<TRequest, TResponse>(StreamPipelineTests.Logger output) : IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : Ping, IStreamRequest<TResponse>
        where TResponse : Pong
    {
        public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Constrained before");
            await foreach (TResponse result in next())
            {
                yield return result;
            }
            output.Messages.Add("Constrained after");
        }
    }

    public class ConcreteBehavior(StreamPipelineTests.Logger output) : IStreamPipelineBehavior<Ping, Pong>
    {
        public async IAsyncEnumerable<Pong> Handle(Ping request, StreamHandlerDelegate<Pong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            output.Messages.Add("Concrete before");
            await foreach (Pong result in next())
            {
                yield return result;
            }
            output.Messages.Add("Concrete after");
        }
    }

    public class Logger
    {
        public IList<string> Messages { get; } = [];
    }

    [Fact]
    public async Task Should_wrap_with_behavior()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(PublishTests).Assembly);
            _ = cfg.AddStreamBehavior<OuterBehavior>();
            _ = cfg.AddStreamBehavior<InnerBehavior>();
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        await foreach (Pong? response in mediator.CreateStream(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken))
        {
            response.Message.ShouldBe("Ping Pong");
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
    public async Task Should_wrap_generics_with_behavior()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(PublishTests).Assembly);

            _ = cfg.AddOpenStreamBehavior(typeof(OuterBehavior<,>));
            _ = cfg.AddOpenStreamBehavior(typeof(InnerBehavior<,>));
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        await foreach (Pong? response in mediator.CreateStream(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken))
        {
            response.Message.ShouldBe("Ping Pong");
        }

        output.Messages.ShouldBe(
        [
            "Outer generic before",
            "Inner generic before",
            "Handler",
            "Inner generic after",
            "Outer generic after",
        ]);
    }

    [Fact]
    public async Task Should_handle_constrained_generics()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(PublishTests).Assembly);

            _ = cfg.AddOpenStreamBehavior(typeof(OuterBehavior<,>));
            _ = cfg.AddOpenStreamBehavior(typeof(InnerBehavior<,>));
            _ = cfg.AddOpenStreamBehavior(typeof(ConstrainedBehavior<,>));
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        await foreach (Pong? response in mediator.CreateStream(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken))
        {
            response.Message.ShouldBe("Ping Pong");
        }

        output.Messages.ShouldBe(
        [
            "Outer generic before",
            "Inner generic before",
            "Constrained before",
            "Handler",
            "Constrained after",
            "Inner generic after",
            "Outer generic after",
        ]);

        output.Messages.Clear();

        await foreach (Zong? response in mediator.CreateStream(new Zing { Message = "Zing" }, TestContext.Current.CancellationToken))
        {
            response.Message.ShouldBe("Zing Zong");
        }

        output.Messages.ShouldBe(
        [
            "Outer generic before",
            "Inner generic before",
            "Handler",
            "Inner generic after",
            "Outer generic after",
        ]);
    }

    [Fact]
    public async Task Should_handle_concrete_and_open_generics()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(output);
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(typeof(PublishTests).Assembly);

            _ = cfg.AddOpenStreamBehavior(typeof(OuterBehavior<,>));
            _ = cfg.AddOpenStreamBehavior(typeof(InnerBehavior<,>));
            _ = cfg.AddStreamBehavior<ConcreteBehavior>();
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        await foreach (Pong? response in mediator.CreateStream(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken))
        {
            response.Message.ShouldBe("Ping Pong");
        }

        output.Messages.ShouldBe(
        [
            "Outer generic before",
            "Inner generic before",
            "Concrete before",
            "Handler",
            "Concrete after",
            "Inner generic after",
            "Outer generic after",
        ]);

        output.Messages.Clear();

        await foreach (Zong? response in mediator.CreateStream(new Zing { Message = "Zing" }, TestContext.Current.CancellationToken))
        {
            response.Message.ShouldBe("Zing Zong");
        }

        output.Messages.ShouldBe(
        [
            "Outer generic before",
            "Inner generic before",
            "Handler",
            "Inner generic after",
            "Outer generic after",
        ]);
    }
}