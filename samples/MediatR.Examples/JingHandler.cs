using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples;

public class JingHandler(TextWriter writer) : IRequestHandler<Jing>
{
    private readonly TextWriter _writer = writer;

    public Task Handle(Jing request, CancellationToken cancellationToken)
        => _writer.WriteLineAsync($"--- Handled Jing: {request.Message}, no Jong");
}