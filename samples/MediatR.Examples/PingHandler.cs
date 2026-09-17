using System.IO;
using System.Threading;

using System.Threading.Tasks;

namespace MediatR.Examples;

public class PingHandler(TextWriter writer) : IRequestHandler<Ping, Pong>
{
    private readonly TextWriter _writer = writer;

    public async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
    {
        await _writer.WriteLineAsync($"--- Handled Ping: {request.Message}");
        return new Pong { Message = request.Message + " Pong" };
    }
}