using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class PipelineTests
{
    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public class VoidPing : IRequest
    {
        public string? Message { get; set; }
    }

    public class Zing : IRequest<Zong>
    {
        public string? Message { get; set; }
    }

    public class Zong
    {
        public string? Message { get; set; }
    }

    public class PingHandler(PipelineTests.Logger output) : IRequestHandler<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            output.Messages.Add("Handler");
            return Task.FromResult(new Pong { Message = request.Message + " Pong" });
        }
    }

    public class VoidPingHandler(PipelineTests.Logger output) : IRequestHandler<VoidPing>
    {
        public Task Handle(VoidPing request, CancellationToken cancellationToken)
        {
            output.Messages.Add("Handler");
            return Task.CompletedTask;
        }
    }

    public class ZingHandler(PipelineTests.Logger output) : IRequestHandler<Zing, Zong>
    {
        public Task<Zong> Handle(Zing request, CancellationToken cancellationToken)
        {
            output.Messages.Add("Handler");
            return Task.FromResult(new Zong { Message = request.Message + " Zong" });
        }
    }

    public class OuterBehavior(PipelineTests.Logger output) : IPipelineBehavior<Ping, Pong>
    {
        public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Outer before");
            Pong response = await next(cancellationToken);
            output.Messages.Add("Outer after");

            return response;
        }
    }

    public class OuterVoidBehavior(PipelineTests.Logger output) : IPipelineBehavior<VoidPing, Unit>
    {
        public async Task<Unit> Handle(VoidPing request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Outer before");
            Unit response = await next(cancellationToken);
            output.Messages.Add("Outer after");

            return response;
        }
    }

    public class InnerBehavior(PipelineTests.Logger output) : IPipelineBehavior<Ping, Pong>
    {
        public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner before");
            Pong response = await next(cancellationToken);
            output.Messages.Add("Inner after");

            return response;
        }
    }

    public class InnerVoidBehavior(PipelineTests.Logger output) : IPipelineBehavior<VoidPing, Unit>
    {
        public async Task<Unit> Handle(VoidPing request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner before");
            Unit response = await next(cancellationToken);
            output.Messages.Add("Inner after");

            return response;
        }
    }

    public class InnerBehavior<TRequest, TResponse>(PipelineTests.Logger output) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner generic before");
            TResponse? response = await next(cancellationToken);
            output.Messages.Add("Inner generic after");

            return response;
        }
    }

    public class OuterBehavior<TRequest, TResponse>(PipelineTests.Logger output)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly Logger _output = output;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Outer generic before");
            TResponse? response = await next(cancellationToken);
            _output.Messages.Add("Outer generic after");

            return response;
        }
    }

    public class ConstrainedBehavior<TRequest, TResponse>(PipelineTests.Logger output)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : Ping
        where TResponse : Pong
    {
        private readonly Logger _output = output;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Constrained before");
            TResponse response = await next(cancellationToken);
            _output.Messages.Add("Constrained after");

            return response;
        }
    }

    public class ConcreteBehavior(PipelineTests.Logger output) : IPipelineBehavior<Ping, Pong>
    {
        public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Concrete before");
            Pong response = await next(cancellationToken);
            output.Messages.Add("Concrete after");

            return response;
        }
    }

    public class Logger
    {
        public IList<string> Messages { get; } = [];
    }

    [Fact]
    public async Task Should_wrap_with_behavior()
    {
        Logger output = new();

        ServiceCollection services = new();
        _ = services.AddSingleton(output);
        _ = services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddSingleton<IPipelineBehavior<Ping, Pong>, OuterBehavior>();
        _ = services.AddSingleton<IPipelineBehavior<Ping, Pong>, InnerBehavior>();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        Pong response = await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong");

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
    public async Task Should_wrap_void_with_behavior()
    {
        Logger output = new();

        ServiceCollection services = new();
        _ = services.AddSingleton(output);
        _ = services.AddSingleton<IRequestHandler<VoidPing>, VoidPingHandler>();
        _ = services.AddSingleton<IPipelineBehavior<VoidPing, Unit>, OuterVoidBehavior>();
        _ = services.AddSingleton<IPipelineBehavior<VoidPing, Unit>, InnerVoidBehavior>();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new VoidPing { Message = "Ping" }, TestContext.Current.CancellationToken);

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
        Logger output = new();

        ServiceCollection services = new();
        _ = services.AddSingleton(output);
        _ = services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();

        _ = services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(OuterBehavior<,>));
        _ = services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(InnerBehavior<,>));

        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        Pong response = await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong");

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
    public async Task Should_wrap_void_generics_with_behavior()
    {
        Logger output = new();

        ServiceCollection services = new();
        _ = services.AddSingleton(output);
        _ = services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddSingleton<IRequestHandler<VoidPing>, VoidPingHandler>();

        _ = services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(OuterBehavior<,>));
        _ = services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(InnerBehavior<,>));

        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new VoidPing { Message = "Ping" }, TestContext.Current.CancellationToken);

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
        Logger output = new();

        ServiceCollection services = new();
        _ = services.AddSingleton(output);
        _ = services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddSingleton<IRequestHandler<Zing, Zong>, ZingHandler>();

        _ = services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(OuterBehavior<,>));
        _ = services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(InnerBehavior<,>));
        _ = services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ConstrainedBehavior<,>));

        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();

        // force resolution like original test did
        _ = provider.GetServices<IPipelineBehavior<Ping, Pong>>();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        Pong response = await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong");

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

        Zong zingResponse = await mediator.Send(new Zing { Message = "Zing" }, TestContext.Current.CancellationToken);

        zingResponse.Message.ShouldBe("Zing Zong");

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
        Logger output = new();

        ServiceCollection services = new();
        _ = services.AddSingleton(output);
        _ = services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();
        _ = services.AddSingleton<IRequestHandler<Zing, Zong>, ZingHandler>();

        _ = services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(OuterBehavior<,>));
        _ = services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(InnerBehavior<,>));
        _ = services.AddSingleton<IPipelineBehavior<Ping, Pong>, ConcreteBehavior>();

        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider provider = services.BuildServiceProvider();

        // force resolution like original test did
        _ = provider.GetServices<IPipelineBehavior<Ping, Pong>>();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        Pong response = await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        response.Message.ShouldBe("Ping Pong");

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

        Zong zingResponse = await mediator.Send(new Zing { Message = "Zing" }, TestContext.Current.CancellationToken);

        zingResponse.Message.ShouldBe("Zing Zong");

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