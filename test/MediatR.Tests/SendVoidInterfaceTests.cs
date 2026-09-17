using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests;

public class SendVoidInterfaceTests
{
    public class Ping : IRequest
    {
        public string? Message { get; set; }
    }

    public class PingHandler(TextWriter writer) : IRequestHandler<Ping>
    {
        public Task Handle(Ping request, CancellationToken cancellationToken)
            => writer.WriteAsync(request.Message + " Pong");
    }

    [Fact]
    public async Task Should_resolve_main_void_handler()
    {
        StringBuilder builder = new();
        StringWriter writer = new(builder);

        ServiceCollection services = new();
        _ = services.AddSingleton<TextWriter>(writer);
        _ = services.AddSingleton<IRequestHandler<Ping>, PingHandler>();
        _ = services.AddSingleton<IMediator>(sp => new Mediator(sp));
        _ = services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new Ping { Message = "Ping" }, TestContext.Current.CancellationToken);

        builder.ToString().ShouldBe("Ping Pong");
    }
}