using System.Threading;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);

        var services = new ServiceCollection();
        services.AddSingleton<TextWriter>(writer);
        services.AddSingleton<IRequestHandler<Ping>, PingHandler>();
        services.AddSingleton<IMediator>(sp => new Mediator(sp));
        services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());

        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new Ping { Message = "Ping" });

        builder.ToString().ShouldBe("Ping Pong");
    }
}