using MediatR.Pipeline;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples;

public class GenericRequestPostProcessor<TRequest, TResponse>(TextWriter writer) : IRequestPostProcessor<TRequest, TResponse>
{
    private readonly TextWriter _writer = writer;

    public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
        => _writer.WriteLineAsync("- All Done");
}