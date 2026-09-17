using MediatR.Pipeline;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples;

public class GenericRequestPreProcessor<TRequest>(TextWriter writer) : IRequestPreProcessor<TRequest>
{
    private readonly TextWriter _writer = writer;

    public Task Process(TRequest request, CancellationToken cancellationToken)
        => _writer.WriteLineAsync("- Starting Up");
}