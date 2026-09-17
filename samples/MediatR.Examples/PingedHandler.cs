using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples;

public class PingedHandler(TextWriter writer) : INotificationHandler<Pinged>
{
    private readonly TextWriter _writer = writer;

    public Task Handle(Pinged notification, CancellationToken cancellationToken)
        => _writer.WriteLineAsync("Got pinged async.");
}

public class PongedHandler(TextWriter writer) : INotificationHandler<Ponged>
{
    private readonly TextWriter _writer = writer;

    public Task Handle(Ponged notification, CancellationToken cancellationToken)
        => _writer.WriteLineAsync("Got ponged async.");
}

public class ConstrainedPingedHandler<TNotification>(TextWriter writer) : INotificationHandler<TNotification>
    where TNotification : Pinged
{
    private readonly TextWriter _writer = writer;

    public Task Handle(TNotification notification, CancellationToken cancellationToken)
        => _writer.WriteLineAsync("Got pinged constrained async.");
}

public class PingedAlsoHandler(TextWriter writer) : INotificationHandler<Pinged>
{
    private readonly TextWriter _writer = writer;

    public Task Handle(Pinged notification, CancellationToken cancellationToken)
        => _writer.WriteLineAsync("Got pinged also async.");
}