using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatR.Tests.Pipeline.Streams;

public class StreamPipelineBehaviorTests
{
    public class Sing : IStreamRequest<Song>
    {
        public string? Message { get; set; }
    }

    public class Song
    {
        public string? Message { get; set; }
    }

    public class SingHandler : IStreamRequestHandler<Sing, Song>
    {
        public async IAsyncEnumerable<Song> Handle(Sing request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return await Task.Run(() => new Song { Message = request.Message + " Song" });
            yield return await Task.Run(() => new Song { Message = request.Message + " Sang" });
            yield return await Task.Run(() => new Song { Message = request.Message + " Seng" });
        }
    }

    public class SingSongPipelineBehavior : IStreamPipelineBehavior<Sing, Song>
    {
        public async IAsyncEnumerable<Song> Handle(Sing request, StreamHandlerDelegate<Song> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new Song { Message = "Start behaving..." };

            await foreach (Song? item in next().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }

            yield return new Song { Message = "...Ready behaving" };
        }
    }

    [Fact]
    public async Task Should_run_pipeline_behavior()
    {
        var services = new ServiceCollection();

        // Register the stream handler and the pipeline behavior
        _ = services.AddTransient<IStreamRequestHandler<Sing, Song>, SingHandler>();
        _ = services.AddTransient<IStreamPipelineBehavior<Sing, Song>, SingSongPipelineBehavior>();

        // Register Mediator
        _ = services.AddTransient<IMediator, Mediator>();

        ServiceProvider provider = services.BuildServiceProvider();

        IMediator mediator = provider.GetRequiredService<IMediator>();

        IAsyncEnumerable<Song> responses = mediator.CreateStream(new Sing { Message = "Sing" }, TestContext.Current.CancellationToken);

        int i = 0;
        await foreach (Song? response in responses)
        {
            if (i == 0)
            {
                response.Message.ShouldBe("Start behaving...");
            }
            else if (i == 1)
            {
                response.Message.ShouldBe("Sing Song");
            }
            else if (i == 2)
            {
                response.Message.ShouldBe("Sing Sang");
            }
            else if (i == 3)
            {
                response.Message.ShouldBe("Sing Seng");
            }
            else if (i == 4)
            {
                response.Message.ShouldBe("...Ready behaving");
            }

            (++i).ShouldBeLessThanOrEqualTo(5);
        }
    }

}