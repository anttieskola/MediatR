using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
            var response = await next();
            output.Messages.Add("Outer after");

            return response;
        }
    }

    public class OuterVoidBehavior(PipelineTests.Logger output) : IPipelineBehavior<VoidPing, Unit>
    {
        public async Task<Unit> Handle(VoidPing request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Outer before");
            var response = await next();
            output.Messages.Add("Outer after");

            return response;
        }
    }

    public class InnerBehavior(PipelineTests.Logger output) : IPipelineBehavior<Ping, Pong>
    {
        public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner before");
            var response = await next();
            output.Messages.Add("Inner after");

            return response;
        }
    }

    public class InnerVoidBehavior(PipelineTests.Logger output) : IPipelineBehavior<VoidPing, Unit>
    {
        public async Task<Unit> Handle(VoidPing request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Inner before");
            var response = await next();
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
            var response = await next();
            output.Messages.Add("Inner generic after");

            return response;
        }
    }

    public class OuterBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly Logger _output;

        public OuterBehavior(Logger output)
        {
            _output = output;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Outer generic before");
            var response = await next();
            _output.Messages.Add("Outer generic after");

            return response;
        }
    }

    public class ConstrainedBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : Ping
        where TResponse : Pong
    {
        private readonly Logger _output;

        public ConstrainedBehavior(Logger output)
        {
            _output = output;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Constrained before");
            var response = await next();
            _output.Messages.Add("Constrained after");

            return response;
        }
    }

    public class ConcreteBehavior(PipelineTests.Logger output) : IPipelineBehavior<Ping, Pong>
    {
        public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
        {
            output.Messages.Add("Concrete before");
            var response = await next();
            output.Messages.Add("Concrete after");

            return response;
        }
    }

    public class Logger
    {
        public IList<string> Messages { get; } = new List<string>();
    }

    [Fact]
    public async Task Should_wrap_with_behavior()
    {
        var output = new Logger();

        var services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddSingleton<IPipelineBehavior<Ping, Pong>, OuterBehavior>();
        services.AddSingleton<IPipelineBehavior<Ping, Pong>, InnerBehavior>();
        services.AddSingleton<IMediator>(sp => new Mediator(sp));
        services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

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
        var output = new Logger();

        var services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddSingleton<IRequestHandler<VoidPing>, VoidPingHandler>();
        services.AddSingleton<IPipelineBehavior<VoidPing, Unit>, OuterVoidBehavior>();
        services.AddSingleton<IPipelineBehavior<VoidPing, Unit>, InnerVoidBehavior>();
        services.AddSingleton<IMediator>(sp => new Mediator(sp));
        services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new VoidPing { Message = "Ping" });

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

        var services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();

        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(OuterBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(InnerBehavior<,>));

        services.AddSingleton<IMediator>(sp => new Mediator(sp));
        services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

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
        var output = new Logger();

        var services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddSingleton<IRequestHandler<VoidPing>, VoidPingHandler>();

        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(OuterBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(InnerBehavior<,>));

        services.AddSingleton<IMediator>(sp => new Mediator(sp));
        services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new VoidPing { Message = "Ping" });

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

        var services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddSingleton<IRequestHandler<Zing, Zong>, ZingHandler>();

        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(OuterBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(InnerBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ConstrainedBehavior<,>));

        services.AddSingleton<IMediator>(sp => new Mediator(sp));
        services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();

        // force resolution like original test did
        provider.GetServices<IPipelineBehavior<Ping, Pong>>();

        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

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

        var zingResponse = await mediator.Send(new Zing { Message = "Zing" });

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
        var output = new Logger();

        var services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddSingleton<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddSingleton<IRequestHandler<Zing, Zong>, ZingHandler>();

        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(OuterBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(InnerBehavior<,>));
        services.AddSingleton<IPipelineBehavior<Ping, Pong>, ConcreteBehavior>();

        services.AddSingleton<IMediator>(sp => new Mediator(sp));
        services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();

        // force resolution like original test did
        provider.GetServices<IPipelineBehavior<Ping, Pong>>();

        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

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

        var zingResponse = await mediator.Send(new Zing { Message = "Zing" });

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