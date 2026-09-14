# TODO — Issues Found During Documentation Review

Issues discovered while documenting the code in `src/MediatR`. Ordered by severity.
Nothing listed here has been changed in the code yet — each chapter is an open item.

---

## 1. `AutoRegisterRequestProcessors` does not register the driving pipeline behaviors (likely bug)

**Severity:** High — feature silently does nothing when used as documented.

**Files:**
- `src/MediatR/Registration/ServiceRegistrar.cs` — `AddMediatRClasses`, `AddRequiredServices`
- `src/MediatR/MicrosoftExtensionsDI/MediatrServiceConfiguration.cs` — `AutoRegisterRequestProcessors`

**Problem:**
Setting `AutoRegisterRequestProcessors = true` makes the assembly scan register all
`IRequestPreProcessor<>` / `IRequestPostProcessor<,>` implementations found in the scanned
assemblies. However, `AddRequiredServices` only adds the behaviors that actually drive
those processors when the *configuration lists* are non-empty:

```csharp
if (serviceConfiguration.RequestPreProcessorsToRegister.Any())   // populated only via AddRequestPreProcessor(...)
{
    services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>), Transient));
    ...
}
```

The scan path (`ConnectImplementationsToTypesClosing`) registers processor services directly
on `IServiceCollection` and does **not** populate `RequestPreProcessorsToRegister`.
Result: with `AutoRegisterRequestProcessors = true` alone (no explicit
`AddRequestPreProcessor`/`AddRequestPostProcessor` calls), processors are registered but
`RequestPreProcessorBehavior<,>` / `RequestPostProcessorBehavior<,>` are never registered,
so processors are never executed. No error is thrown — it fails silently.

**Suggested direction:**
- In `AddRequiredServices`, also register the driving behaviors when
  `serviceConfiguration.AutoRegisterRequestProcessors` is `true`, or
- Have the scan path add the found processors to
  `RequestPreProcessorsToRegister`/`RequestPostProcessorsToRegister` instead of (or in
  addition to) registering them directly, or
- At minimum, document the requirement to pair the flag with the `AddRequest*Processor`
  helpers (already noted as a caveat in `docs/topics/Request-Processors.md`).

**Verification:** add a unit test (see `test/MediatR.Tests`) that enables
`AutoRegisterRequestProcessors`, registers no explicit processors, sends a request, and
asserts the pre/post processors ran.

---

## 2. Single-implementation-type `Add*` overloads are broken for open-generic implementation types (verified bug)

**Severity:** High — container build fails at runtime when using these overloads with open-generic types.

**Files:**
- `src/MediatR/MicrosoftExtensionsDI/MediatrServiceConfiguration.cs` — `AddBehavior(Type implementationType)`, `AddStreamBehavior(Type implementationType)`, `AddRequestPreProcessor(Type implementationType)`, `AddRequestPostProcessor(Type implementationType)` (and their generic `Add*<TImpl>()` overloads)
- `src/MediatR/Registration/ServiceRegistrar.cs` — `FindInterfacesThatClose` / `FindInterfacesThatClosesCore`

**Problem (reproduced by execution, see verification round 5):**
These overloads derive the service type via `FindInterfacesThatClose`. For an open-generic
implementation type (e.g. `MyBehavior<,>`), `GetInterfaces()` returns the interface closed
with the class's *type parameters* (`IPipelineBehavior<TRequest,TResponse>`), which is a
`Type` that is **not** a generic type definition. The resulting `ServiceDescriptor` therefore
has a bogus "closed" service type, and Microsoft DI fails at `BuildServiceProvider()` with:

```
ArgumentException: Cannot instantiate implementation type 'MyBehavior`2[TRequest,TResponse]'
for service type 'MediatR.IPipelineBehavior`2[TRequest,TResponse]'.
```

Reproduced for `AddBehavior(typeof(MyBehavior<,>))` and `AddRequestPreProcessor(typeof(MyPreProcessor<>))`.
The same code path affects `AddStreamBehavior` / `AddRequestPostProcessor` single-type overloads.

**Working alternatives (verified):**
- `AddOpenBehavior(Type)` / `AddOpenStreamBehavior(Type)` / `AddOpenRequestPreProcessor(Type)` /
  `AddOpenRequestPostProcessor(Type)` — these use `GetGenericTypeDefinition()` and produce a
correct open-generic service type (container builds, behavior resolves).
- `AddBehavior(Type serviceType, Type implementationType)` with explicitly closed types.
- Closed (non-generic) implementation types work fine with the single-type overloads.

**Suggested direction:** make the single-implementation-type overloads map open-generic
implementations to the open-generic interface (as `AddOpen*` does), or document/throw for
that case. Add unit tests covering all four overloads with open-generic types.

---

## 3. Multiple handlers for the same request type: first-found silently wins

**Severity:** Medium — surprising behavior, easy to miss in applications.

**Files:**
- `src/MediatR/Registration/ServiceRegistrar.cs` — `ConnectImplementationsToTypesClosing`
  (`addIfAlreadyExists: false` path → `TryAddTransient`), `AddConcretionsThatCouldBeClosed`

**Problem:**
Request handlers (`IRequestHandler<,>` / `IRequestHandler<>`) and stream request handlers
(`IStreamRequestHandler<,>`) are registered with `TryAddTransient`. If two or more handler
classes implement the same request type, the first one encountered during assembly scanning
is registered and the rest are silently ignored. There is no warning, log, or exception,
so a "missing" handler is hard to diagnose (scan order depends on assembly/type order).

Notification handlers, exception handlers/actions, and processors use `AddTransient`
(all registered), so this only affects request/stream handlers.

**Suggested direction:**
- Consider logging a warning (or throwing in debug/DI-validation mode) when a second
  implementation for an already-registered request handler service type is encountered, and/or
- Document the behavior prominently (already documented in
  `docs/topics/Request-Response.md`).

---

## 4. `RegistrationTimeout` XML doc is misleading

**Severity:** Low — documentation accuracy in the public API.

**Files:**
- `src/MediatR/MicrosoftExtensionsDI/MediatrServiceConfiguration.cs` — `RegistrationTimeout`
- `src/MediatR/Registration/ServiceRegistrar.cs` — `AddMediatRClassesWithTimeout`

**Problem:**
The property doc says: *"Configure the Timeout in Milliseconds that the GenericHandler
Registration Process will exit with error."* In reality the `CancellationTokenSource`
created from this value covers the **entire** `AddMediatRClasses` scan (all handler
kinds, not just generic handlers), and on expiry a `TimeoutException` is thrown
("The generic handler registration process timed out." — also mentions only generic
handlers).

**Suggested direction:** align the property doc and the `TimeoutException` message with
the actual scope ("handler registration scan").

---

## 5. `ISender.CreateStream` overloads have empty/missing XML doc comments

**Severity:** Low — public API documentation.

**Files:**
- `src/MediatR/ISender.cs`

**Problem:**
The two `CreateStream` members have incomplete doc comments:
`/// <typeparam name="TResponse"></typeparam>`, empty `<param>` and `<returns>` tags,
and the object-based overload has no `<summary>` at all. This is the only part of
`ISender` without proper docs.

**Suggested direction:** write proper summaries/params/returns (the behavior is
documented in `docs/topics/Streaming-Requests.md` and can be reused).

---

## 6. `RequestExceptionActionProcessorStrategy` lives in a DI namespace

**Severity:** Low — design/namespacing observation.

**Files:**
- `src/MediatR/MicrosoftExtensionsDI/RequestExceptionActionProcessorStrategy.cs`
  (namespace `Microsoft.Extensions.DependencyInjection`)

**Problem:**
The enum is a core library concept (it selects which built-in behaviors are registered)
but is placed in the `Microsoft.Extensions.DependencyInjection` namespace alongside DI
extension code. Users who don't use that DI integration still need to reference the
namespace to configure it.

**Suggested direction:** consider moving it to the `MediatR` namespace (keep a type
forward or obsolete alias for compatibility) — or keep as-is if the namespace placement
is intentional for drop-in compatibility.

---

## 7. Minor repository file-name typos

**Severity:** Trivial — consistency.

- `generate_coverare_report.cmd` → probably should be `generate_coverage_report.cmd`
- `coverture.runsettings` → probably should be `coverlet.runsettings` (standard name)
- `Directory.Packages.Props` → unconventional casing for `Directory.Packages.props`

Check whether any scripts/CI reference these exact names before renaming.

---

## 8. Documentation corrections applied during review (for the record)

**Severity:** Informational — these were documentation errors found while writing
`docs/` and have **already been corrected** in the docs (no code involved). Listed here
so the review trail is complete:

- `docs/topics/Request-Response.md` — removed false claim that handler-ordering picks
  "the most specific" request handler (see issue #3 for the real behavior).
- `docs/topics/Request-Processors.md` — registration section rewritten: added the
  `AddRequestPreProcessor`/`AddOpenRequestPreProcessor`/`AddRequestPostProcessor`/
  `AddOpenRequestPostProcessor` helpers; added caveat about issue #1.
- `docs/topics/Dependency-Injection.md` — added the fluent registration helpers section;
  fixed `RegistrationTimeout` description (see issue #4); documented TryAdd vs Add
  registration modes (see issue #3); corrected the condition under which pre/post
  processor behaviors are auto-registered.
- `docs/topics/Exception-Handling.md` — corrected exception-type matching description
  (a handler for type `E` matches any thrown exception assignable to `E`).
- `docs/topics/Handler-Ordering.md` — corrected namespace comparison criteria 2/3
  (the request's namespace is the reference point).
- `docs/topics/Notifications.md` — fixed "generic request type parameters" wording.
- `docs/topics/Streaming-Requests.md` — added `AddStreamBehavior`/`AddOpenStreamBehavior`
  to the registration guidance.
- `docs/topics/Notification-Publishing-Strategies.md` — clarified `Task.WhenAll`
  failure semantics.

Fifth verification round (execution-based; all 14 runtime assertions passed):

- Built a scratch harness referencing `src/MediatR` and executed the documented behaviors:
  basic/void `Send`, behavior ordering (first registered = outermost), pre/post processor
  order + response visibility, exception handler response replacement, exception action +
  rethrow, base-type exception matching, the todo #1 caveat (auto-registered processor does
  not run), first-found-wins for duplicate request handlers, stream items + stream behavior,
  and sequential-vs-concurrent publishers (timed). All matched the documentation.
- This round **discovered issue #2** (open-generic single-type `Add*` overloads produce a
  bogus service type and break `BuildServiceProvider`); the DI topic page has been
  corrected accordingly.

Fourth verification pass (found and fixed in the docs only; all mermaid diagrams
validated with the actual mermaid parser — all 8 blocks parse OK):

- `docs/Functionality.md` — `RequestExceptionActionProcessorStrategy` listed under the
  wrong namespace (`MediatR.MicrosoftExtensionsDI`); actual namespace is
  `Microsoft.Extensions.DependencyInjection`.
- `docs/topics/Streaming-Requests.md` — "inner handler is invoked with
  `[EnumeratorCancellation]"` was inaccurate; the attribute is on the wrapper's
  enumeration methods, not on the user handler invocation.

Third verification pass (found and fixed in the docs only):

- `docs/Functionality.md` — samples table under-described `MediatR.Examples` (it also contains generic
  handlers, pre/post processors, exception handlers/actions and streaming — verified against
  `samples/MediatR.Examples/`); "Handler Ordering" one-liner and `RequestExceptionHandlerState`
  role rephrased; Quick Tour "wraps every handler" scoped to request handling.

Second verification pass (found and fixed in the docs only):

- `docs/topics/Exception-Handling.md` — the `ApplyForAllExceptions` row wrongly said the
  action behavior "wraps" the handler behavior; it is registered *after* it, i.e. *inside*
  it (closer to the handler).
- `docs/topics/Handler-Ordering.md` — comparison criterion 3 rephrased: it applies when
  neither handler is in the request's namespace subtree.
- `docs/Functionality.md` — Quick Tour wrongly implied assembly scanning finds pipeline
  behaviors (it does not); pre/post processing bullet now says "when registered".
- `docs/topics/Pipeline-Behaviors.md` — "registered automatically when matching
  implementations exist" was imprecise for the processor behaviors (they depend on the
  configuration lists, not on found implementations); made precise.
