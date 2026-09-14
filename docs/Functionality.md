# MediatR (Fork) — Functionality Overview

Documentation of all functionality implemented in this MediatR fork, based on the code in [`src/MediatR`](../src/MediatR).

> This is a fork of the original [MediatR](https://github.com/jbogard/MediatR) library, usable as a drop-in replacement for the original library. See [README](../README.md) for licensing and fork history.

## At a Glance

| # | Topic | Description | Details |
|---|-------|-------------|---------|
| 1 | Request/Response | The core `IRequest<T>` / `IRequestHandler` pattern and `Mediator.Send` | [Request-Response](topics/Request-Response.md) |
| 2 | Notifications | `INotification` / `INotificationHandler` and `Mediator.Publish` | [Notifications](topics/Notifications.md) |
| 3 | Pipeline Behaviors | `IPipelineBehavior<TRequest, TResponse>` and the pipeline chain | [Pipeline-Behaviors](topics/Pipeline-Behaviors.md) |
| 4 | Streaming Requests | `IStreamRequest<T>` handlers returning `IAsyncEnumerable<T>` via `Mediator.CreateStream` | [Streaming-Requests](topics/Streaming-Requests.md) |
| 5 | Notification Publishing Strategies | `INotificationPublisher` with sequential and concurrent implementations | [Notification-Publishing-Strategies](topics/Notification-Publishing-Strategies.md) |
| 6 | Request Pre/Post Processors | `IRequestPreProcessor<T>` / `IRequestPostProcessor<T, TR>` with auto-generated behaviors | [Request-Processors](topics/Request-Processors.md) |
| 7 | Exception Handling | `IRequestExceptionHandler`, `IRequestExceptionAction` and processor strategies | [Exception-Handling](topics/Exception-Handling.md) |
| 8 | Dependency Injection & Registration | `AddMediatR()`, `MediatRServiceConfiguration`, assembly scanning | [Dependency-Injection](topics/Dependency-Injection.md) |
| 9 | Handler Ordering | Internal logic that orders several matching handlers and decides which runs first | [Handler-Ordering](topics/Handler-Ordering.md) |


## Quick Tour

- **Sending requests**: implement `IRequest<TResponse>`, handle it with `IRequestHandler<TRequest, TResponse>`, then `await mediator.Send(request)`.
- **Publishing notifications**: implement `INotification`, handle it with `INotificationHandler<TNotification>`, then `await mediator.Publish(notification)`.
- **Streaming responses**: implement `IStreamRequest<TResponse>` + `IStreamRequestHandler<TRequest, TResponse>` and consume `await foreach (var item in mediator.CreateStream(request))`.
- **Cross-cutting logic**: implement `IPipelineBehavior<TRequest, TResponse>` and register it — it wraps request handling in the pipeline.
- **Pre/post processing**: implement `IRequestPreProcessor<TRequest>` or `IRequestPostProcessor<TRequest, TResponse>` — executed around the handler automatically (when registered, see topic page).
- **Exception handling**: implement `IRequestExceptionHandler<TRequest, TResponse, TException>` (can replace the response) or `IRequestExceptionAction<TRequest, TException>` (side effects such as logging).
- **Registration**: call `services.AddMediatR(config => { ... })` with a `MediatRServiceConfiguration` — assembly scanning finds all handlers (and pre/post processors when enabled); pipeline behaviors are registered separately.

## Key Types Reference

| Type | Namespace | Role |
|------|-----------|------|
| `IMediator`, `Mediator` | `MediatR` | Entry point: `Send`, `Publish`, `CreateStream` |
| `IRequest<TResponse>` | `MediatR` | Request marker interface |
| `IRequestHandler<TRequest, TResponse>` | `MediatR` | Request handler contract |
| `ISender`, `IPublisher` | `MediatR` | Split interfaces behind `IMediator` |
| `INotification`, `INotificationHandler<T>` | `MediatR` | Notification pattern |
| `IPipelineBehavior<TRequest, TResponse>` | `MediatR` | Pipeline wrapper contract |
| `Unit` | `MediatR` | Empty response type for requests without a response |
| `IStreamRequest<TResponse>` | `MediatR` | Streaming request marker |
| `IStreamRequestHandler<TRequest, TResponse>` | `MediatR` | Streaming handler contract |
| `IStreamPipelineBehavior<TRequest, TResponse>` | `MediatR` | Streaming pipeline wrapper |
| `INotificationPublisher` | `MediatR` | Pluggable notification dispatch strategy |
| `ForeachAwaitPublisher`, `TaskWhenAllPublisher` | `MediatR.NotificationPublishers` | Built-in publish strategies |
| `IRequestPreProcessor<TRequest>` | `MediatR.Pipeline` | Runs before the handler |
| `IRequestPostProcessor<TRequest, TResponse>` | `MediatR.Pipeline` | Runs after the handler |
| `IRequestExceptionHandler<TRequest, TResponse, TException>` | `MediatR.Pipeline` | Replaceable exception handling |
| `IRequestExceptionAction<TRequest, TException>` | `MediatR.Pipeline` | Exception side-effect hook |
| `RequestExceptionHandlerState<TResponse>` | `MediatR.Pipeline` | Exception handler state (handled flag + replacement response) |
| `RequestExceptionActionProcessorStrategy` | `Microsoft.Extensions.DependencyInjection` | Strategy for when exception actions run |
| `MediatRServiceConfiguration` | `Microsoft.Extensions.DependencyInjection` | Fluent DI configuration object |
| `OpenBehavior` | `MediatR.Entities` | Behavior registration entity with lifetime |
| `ServiceRegistrar` | `MediatR.Registration` | Assembly scanning / handler registration |
| `HandlersOrderer`, `ObjectDetails` | `MediatR.Internal` | Handler prioritization (internal) |

## Project Layout

```
src/MediatR/
├── Entities/              # OpenBehavior (behavior registration entity)
├── Internal/              # Handler ordering & prioritization
├── MicrosoftExtensionsDI/ # AddMediatR extensions, configuration, exception action strategy
├── NotificationPublishers/# ForeachAwaitPublisher, TaskWhenAllPublisher
├── Pipeline/              # Pre/post processor behaviors, exception handler behaviors
├── Registration/          # ServiceRegistrar (assembly scanning)
├── Wrappers/              # Handler wrappers building the pipeline chain
├── IMediator.cs / Mediator.cs / IRequest.cs / ...  # Core contracts & mediator
```

## Samples

| Sample | Shows |
|--------|-------|
| [`samples/MediatR.Examples`](../samples/MediatR.Examples) | Requests, notifications, generic handlers, pipeline behaviors, pre/post processors, exception handlers/actions, streaming |
| [`samples/MediatR.Examples.AspNetCore`](../samples/MediatR.Examples.AspNetCore) | Usage inside an ASP.NET Core app |
| [`samples/MediatR.Examples.PublishStrategies`](../samples/MediatR.Examples.PublishStrategies) | Notification publishing strategies |

## Target Frameworks

`netstandard2.0` and `net10.0`. See [`src/MediatR/MediatR.csproj`](../src/MediatR/MediatR.csproj).
