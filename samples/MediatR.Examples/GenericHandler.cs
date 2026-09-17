using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples;

public class GenericHandler(TextWriter writer) : INotificationHandler<INotification>
{
    private readonly TextWriter _writer = writer;

    public Task Handle(INotification notification, CancellationToken cancellationToken)
        => _writer.WriteLineAsync("Got notified.");
}