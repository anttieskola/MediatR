using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples;

public class GenericPipelineBehavior<TRequest, TResponse>(TextWriter writer) : IPipelineBehavior<TRequest, TResponse>
{
    private readonly TextWriter _writer = writer;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        await _writer.WriteLineAsync("-- Handling Request");
        TResponse response = await next();
        await _writer.WriteLineAsync("-- Finished Request");
        return response;
    }
}