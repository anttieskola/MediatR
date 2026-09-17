using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class CreateStreamTests
{

    public class Ping : IStreamRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public class PingStreamHandler : IStreamRequestHandler<Ping, Pong>
    {
        public async IAsyncEnumerable<Pong> Handle(Ping request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return await Task.Run(() => new Pong { Message = request.Message + " Pang" });
        }
    }

    [Fact]
    public async Task Should_resolve_main_handler()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IStreamRequestHandler<Ping, Pong>, PingStreamHandler>();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = mediator.CreateStream(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);
        int i = 0;
        await foreach (Pong result in response)
        {
            if (i == 0)
            {
                result.Message.ShouldBe("Ping Pang");
            }

            i++;
        }

        i.ShouldBe(1);
    }

    [Fact]
    public async Task Should_resolve_main_handler_via_dynamic_dispatch()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IStreamRequestHandler<Ping, Pong>, PingStreamHandler>();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        object request = new Ping { Message = "Ping" };
        var response = mediator.CreateStream(request, TestContext.Current.CancellationToken);
        int i = 0;
        await foreach (Pong? result in response)
        {
            if (i == 0)
            {
                result!.Message.ShouldBe("Ping Pang");
            }

            i++;
        }

        i.ShouldBe(1);
    }

    [Fact]
    public async Task Should_resolve_main_handler_by_specific_interface()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IStreamRequestHandler<Ping, Pong>, PingStreamHandler>();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISender>();

        var response = mediator.CreateStream(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);
        int i = 0;
        await foreach (Pong result in response)
        {
            if (i == 0)
            {
                result.Message.ShouldBe("Ping Pang");
            }

            i++;
        }

        i.ShouldBe(1);
    }

    [Fact]
    public void Should_raise_execption_on_null_request()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        _ = Should.Throw<ArgumentNullException>(() => mediator.CreateStream((Ping) null!));
    }

    [Fact]
    public void Should_raise_execption_on_null_request_via_dynamic_dispatch()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        _ = Should.Throw<ArgumentNullException>(() => mediator.CreateStream(null!));
    }
}