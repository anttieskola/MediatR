using MediatR.Pipeline;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples;

public class ConstrainedRequestPostProcessor<TRequest, TResponse>(TextWriter writer)
    : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : Ping
{
    private readonly TextWriter _writer = writer;

    public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
        => _writer.WriteLineAsync("- All Done with Ping");
}