using System.IO;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace MediatR.Tests;

public class NotificationHandlerTests
{
    public class Ping : INotification
    {
        public string? Message { get; set; }
    }

    public class PongChildHandler(TextWriter writer) : NotificationHandler<Ping>
    {
        protected override void Handle(Ping notification)
            => writer.WriteLine(notification.Message + " Pong");
    }

    [Fact]
    public async Task Should_call_abstract_handle_method()
    {
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);

        INotificationHandler<Ping> handler = new PongChildHandler(writer);

        await handler.Handle(
            new Ping() { Message = "Ping" },
            default
        );

        var result = builder.ToString();
        result.ShouldContain("Ping Pong");
    }
}