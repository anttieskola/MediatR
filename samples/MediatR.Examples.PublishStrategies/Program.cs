using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace MediatR.Examples.PublishStrategies;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        ServiceCollection services = new();

        _ = services.AddSingleton<Publisher>();

        _ = services.AddTransient<INotificationHandler<Pinged>>(sp => new SyncPingedHandler("1"));
        _ = services.AddTransient<INotificationHandler<Pinged>>(sp => new AsyncPingedHandler("2"));
        _ = services.AddTransient<INotificationHandler<Pinged>>(sp => new AsyncPingedHandler("3"));
        _ = services.AddTransient<INotificationHandler<Pinged>>(sp => new SyncPingedHandler("4"));

        var provider = services.BuildServiceProvider();

        var publisher = provider.GetRequiredService<Publisher>();

        Pinged pinged = new();

        foreach (PublishStrategy strategy in Enum.GetValues<PublishStrategy>())
        {
            Console.WriteLine($"Strategy: {strategy}");
            Console.WriteLine("----------");

            try
            {
                await publisher.Publish(pinged, strategy);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.GetType()}: {ex.Message}");
            }

            await Task.Delay(1000);
            Console.WriteLine("----------");
        }

        Console.WriteLine("done");
    }
}