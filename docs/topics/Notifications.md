# Notification Pattern

The notification pattern: a publisher sends an `INotification` and **all** matching handlers are invoked (fire and forget style, no response).

[← Back to Functionality overview](../Functionality.md)

## Key Types

| Type | File | Role |
|------|------|------|
| `INotification` | `src/MediatR/INotification.cs` | Marker interface for notifications |
| `INotificationHandler<TNotification>` | `src/MediatR/INotificationHandler.cs` | Async handler contract |
| `NotificationHandler<TNotification>` | `src/MediatR/INotificationHandler.cs` | Abstract base for synchronous handlers |
| `IPublisher` | `src/MediatR/IPublisher.cs` | Interface exposing `Publish` |
| `NotificationHandlerExecutor` | `src/MediatR/NotificationHandlerExecutor.cs` | Record pairing a handler instance with its callback |
| `INotificationPublisher` | `src/MediatR/INotificationPublisher.cs` | Pluggable dispatch strategy (see [Notification Publishing Strategies](Notification-Publishing-Strategies.md)) |

## Defining a Notification and Handlers

```csharp
public record UserRegistered(User User) : INotification;

public class SendWelcomeEmailHandler : INotificationHandler<UserRegistered>
{
    public Task Handle(UserRegistered notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

For synchronous handler logic, derive from `NotificationHandler<TNotification>` and override the `void Handle(TNotification)` method:

```csharp
public class AuditLogHandler : NotificationHandler<UserRegistered>
{
    protected override void Handle(UserRegistered notification)
    {
        // synchronous work
    }
}
```

## Publishing

```csharp
await mediator.Publish(new UserRegistered(user));
```

### How `Mediator.Publish` Works

- The mediator caches a `NotificationHandlerWrapperImpl<TNotification>` per notification type (see `Wrappers/NotificationHandlerWrapper.cs`).
- The wrapper resolves **all** registered `INotificationHandler<TNotification>` instances and wraps each in a `NotificationHandlerExecutor` record.
- It then hands the executors to the configured `INotificationPublisher` (via the protected virtual `PublishCore` method, which can be overridden in derived mediator classes).
- The default publisher awaits handlers sequentially; a concurrent `Task.WhenAll` publisher is also provided — see [Notification Publishing Strategies](Notification-Publishing-Strategies.md).

## Notes

- Unlike requests, notifications have **no response** and may have **zero or many** handlers.
- Notification handlers do not participate in the request pipeline (`IPipelineBehavior` does not apply).
- Generic handler classes are supported: an open-generic `INotificationHandler<>` implementation is registered as an open generic, so a single generic handler class can serve multiple notification types (see [Dependency Injection](Dependency-Injection.md)).
