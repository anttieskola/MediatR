# Notification Publishing Strategies 

Controls **how** the mediator awaits the set of notification handlers: one by one, or concurrently. The strategy is pluggable through `INotificationPublisher`.

[← Back to Functionality overview](../Functionality.md)

## Key Types

| Type | File | Role |
|------|------|------|
| `INotificationPublisher` | `src/MediatR/INotificationPublisher.cs` | Strategy contract |
| `ForeachAwaitPublisher` | `src/MediatR/NotificationPublishers/ForeachAwaitPublisher.cs` | Sequential, await-one-by-one strategy (default) |
| `TaskWhenAllPublisher` | `src/MediatR/NotificationPublishers/TaskWhenAllPublisher.cs` | Concurrent `Task.WhenAll` strategy |
| `NotificationHandlerExecutor` | `src/MediatR/NotificationHandlerExecutor.cs` | Handler instance + callback passed to the strategy |

## The Contract

```csharp
public interface INotificationPublisher
{
    Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification,
        CancellationToken cancellationToken);
}
```

Each `NotificationHandlerExecutor` is a record pairing a handler instance with a `Func<INotification, CancellationToken, Task>` callback. A strategy decides when and how those callbacks are awaited.

## Built-in Strategies

### `ForeachAwaitPublisher` (default)

Awaits each handler in a `foreach` loop — handlers run **sequentially**, one completes before the next starts:

```csharp
foreach (var handler in handlerExecutors)
{
    await handler.HandlerCallback(notification, cancellationToken);
}
```

Properties:

- Deterministic, easy to debug; exceptions from an earlier handler prevent later handlers from running.
- Total time = sum of handler times.

### `TaskWhenAllPublisher`

Starts every handler and awaits them with `Task.WhenAll` — handlers run **concurrently**:

```csharp
var tasks = handlerExecutors
    .Select(handler => handler.HandlerCallback(notification, cancellationToken))
    .ToArray();

return Task.WhenAll(tasks);
```

Properties:

- Total time ≈ slowest handler.
- All handlers start before any completes; a failure does not stop the other in-flight handlers — they keep running, and the returned task faults when the first handler faults.

## Selecting a Strategy

The strategy is configured in DI — see [Dependency Injection](Dependency-Injection.md):

```csharp
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<Program>();
    // Option 1: configure the publisher instance
    config.NotificationPublisher = new TaskWhenAllPublisher();
    // Option 2: register your own INotificationPublisher implementation (takes precedence)
    // config.NotificationPublisherType = typeof(MyCustomPublisher);
});
```

- `MediatRServiceConfiguration.NotificationPublisher` sets a concrete instance; `NotificationPublisherType` (when set) overrides it with a type resolved from the container.
- The `INotificationPublisher` service is registered with `TryAdd`, so you can also pre-register your own implementation before calling `AddMediatR`.
- Alternatively, construct `Mediator` directly with a publisher: `new Mediator(serviceProvider, new TaskWhenAllPublisher())`.
- `Mediator.PublishCore` is `protected virtual`, so a custom `Mediator` subclass can override publishing behavior entirely.

## Custom Strategy Example

```csharp
public class RetryForeachPublisher : INotificationPublisher
{
    public async Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification, CancellationToken cancellationToken)
    {
        foreach (var handler in handlerExecutors)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    await handler.HandlerCallback(notification, cancellationToken);
                    break;
                }
                catch when (attempt < 2) { /* retry */ }
            }
        }
    }
}
```
