# Request Pre/Post Processors 

Pre and post processors run automatically around request handlers: pre-processors **before** the handler, post-processors **after** it. They are implemented as regular pipeline behaviors that fan out to all registered processors.

[← Back to Functionality overview](../Functionality.md)

## Key Types

| Type | File | Role |
|------|------|------|
| `IRequestPreProcessor<TRequest>` | `src/MediatR/Pipeline/IRequestPreProcessor.cs` | Runs before the handler |
| `IRequestPostProcessor<TRequest, TResponse>` | `src/MediatR/Pipeline/IRequestPostProcessor.cs` | Runs after the handler (sees the response) |
| `RequestPreProcessorBehavior<TRequest, TResponse>` | `src/MediatR/Pipeline/RequestPreProcessorBehavior.cs` | Pipeline behavior executing all pre-processors |
| `RequestPostProcessorBehavior<TRequest, TResponse>` | `src/MediatR/Pipeline/RequestPostProcessorBehavior.cs` | Pipeline behavior executing all post-processors |

## Contracts

```csharp
public interface IRequestPreProcessor<in TRequest> where TRequest : notnull
{
    Task Process(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestPostProcessor<in TRequest, in TResponse> where TRequest : notnull
{
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}
```

Examples:

```csharp
public class ValidateUserPreProcessor : IRequestPreProcessor<SendMessage>
{
    public Task Process(SendMessage request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("UserId is required");
        return Task.CompletedTask;
    }
}

public class AuditPostProcessor : IRequestPostProcessor<SendMessage, MessageId>
{
    public Task Process(SendMessage request, MessageId response, CancellationToken cancellationToken)
    {
        // log request + response, publish an event, etc.
        return Task.CompletedTask;
    }
}
```

## How It Works

- `RequestPreProcessorBehavior<TRequest, TResponse>` is an `IPipelineBehavior<TRequest, TResponse>` that runs **every** registered `IRequestPreProcessor<TRequest>` in order, then calls `next()`.
- `RequestPostProcessorBehavior<TRequest, TResponse>` calls `next()` first, then runs **every** registered `IRequestPostProcessor<TRequest, TResponse>` in order, and finally returns the response.
- Because they are pipeline behaviors, they compose with your own behaviors and with the exception-handling behaviors — see [Pipeline Behaviors](Pipeline-Behaviors.md) and [Exception Handling](Exception-Handling.md).
- A pre-processor exception short-circuits the pipeline (the handler never runs). A post-processor exception replaces the successful result with a faulted task.

## Registration

`MediatRServiceConfiguration` provides fluent helpers (all defaulting to `ServiceLifetime.Transient`):

- `AddRequestPreProcessor<TImpl>()` / `AddRequestPreProcessor(Type serviceType, Type implementationType)` — closed registrations against all `IRequestPreProcessor<TRequest>` interfaces the type implements.
- `AddOpenRequestPreProcessor(Type)` — open-generic registration against `IRequestPreProcessor<>`.
- `AddRequestPostProcessor<TImpl>()` / `AddRequestPostProcessor(Type, Type)` — closed registrations against all `IRequestPostProcessor<TRequest, TResponse>` interfaces.
- `AddOpenRequestPostProcessor(Type)` — open-generic registration against `IRequestPostProcessor<,>`.

These add `ServiceDescriptor`s to `RequestPreProcessorsToRegister` / `RequestPostProcessorsToRegister`; processors execute in list order. When those lists are non-empty, `AddMediatR` automatically adds the corresponding `RequestPreProcessorBehavior<,>` / `RequestPostProcessorBehavior<,>` as open-generic pipeline behaviors (see [Dependency Injection](Dependency-Injection.md)).

Alternatively, set `AutoRegisterRequestProcessors = true` — the assembly scan then registers all `IRequestPreProcessor<>` / `IRequestPostProcessor<,>` implementations found in the scanned assemblies (open-generic implementations included).

> **Caveat:** the pipeline behavior that actually drives the processors is only registered when `RequestPreProcessorsToRegister` / `RequestPostProcessorsToRegister` are non-empty. With `AutoRegisterRequestProcessors = true` alone (no explicit `AddRequestPreProcessor`/`AddRequestPostProcessor` calls), the processor implementations are registered but the driving behavior is not, so the processors never run.
