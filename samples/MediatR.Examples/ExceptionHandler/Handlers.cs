using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples.ExceptionHandler;

public class PingResourceHandler(TextWriter writer) : IRequestHandler<PingResource, Pong>
{
    private readonly TextWriter _writer = writer;

    public Task<Pong> Handle(PingResource request, CancellationToken cancellationToken)
        => throw new ResourceNotFoundException();
}

public class PingNewResourceHandler(TextWriter writer) : IRequestHandler<PingNewResource, Pong>
{
    private readonly TextWriter _writer = writer;

    public Task<Pong> Handle(PingNewResource request, CancellationToken cancellationToken)
        => throw new ServerException();
}

public class PingResourceTimeoutHandler(TextWriter writer) : IRequestHandler<PingResourceTimeout, Pong>
{
    private readonly TextWriter _writer = writer;

    public Task<Pong> Handle(PingResourceTimeout request, CancellationToken cancellationToken)
        => throw new TaskCanceledException();
}

public class PingResourceTimeoutOverrideHandler(TextWriter writer) : IRequestHandler<Overrides.PingResourceTimeout, Pong>
{
    private readonly TextWriter _writer = writer;

    public Task<Pong> Handle(Overrides.PingResourceTimeout request, CancellationToken cancellationToken)
        => throw new TaskCanceledException();
}

public class PingProtectedResourceHandler(TextWriter writer) : IRequestHandler<PingProtectedResource, Pong>
{
    private readonly TextWriter _writer = writer;

    public Task<Pong> Handle(PingProtectedResource request, CancellationToken cancellationToken)
        => throw new ForbiddenException();
}