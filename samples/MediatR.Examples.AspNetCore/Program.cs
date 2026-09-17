using MediatR.Examples.Streams;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediatR.Examples.AspNetCore;

public static class Program
{
    public static Task Main(string[] args)
    {
        WrappingWriter writer = new(Console.Out);
        IMediator mediator = BuildMediator(writer);
        return Runner.Run(mediator, writer, "ASP.NET Core DI", testStreams: true);
    }

    private static IMediator BuildMediator(WrappingWriter writer)
    {
        ServiceCollection services = new();

        _ = services.AddSingleton<TextWriter>(writer);

        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblies(typeof(Ping).Assembly, typeof(Sing).Assembly);
        });

        _ = services.AddScoped<IStreamRequestHandler<Sing, Song>, SingHandler>();

        _ = services.AddScoped(typeof(IPipelineBehavior<,>), typeof(GenericPipelineBehavior<,>));
        _ = services.AddScoped(typeof(IRequestPreProcessor<>), typeof(GenericRequestPreProcessor<>));
        _ = services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(GenericRequestPostProcessor<,>));
        _ = services.AddScoped(typeof(IStreamPipelineBehavior<,>), typeof(GenericStreamPipelineBehavior<,>));

        ServiceProvider provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IMediator>();
    }
}