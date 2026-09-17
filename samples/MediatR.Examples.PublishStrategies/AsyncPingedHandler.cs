using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Examples.PublishStrategies;

public class AsyncPingedHandler(string name) : INotificationHandler<Pinged>
{
    public string Name { get; set; } = name;

    public async Task Handle(Pinged notification, CancellationToken cancellationToken)
    {
        if (Name == "2")
        {
            throw new ArgumentException("Name cannot be '2'");
        }

        Console.WriteLine($"[AsyncPingedHandler {Name}] {DateTime.Now:HH:mm:ss.fff} : Pinged");
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"[AsyncPingedHandler {Name}] {DateTime.Now:HH:mm:ss.fff} : After pinged");
    }
}