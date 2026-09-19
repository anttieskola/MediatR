# Change History

## SonarQube warning fixes

All warnings reported in `sonarqube.md` were addressed. Genuine structural fixes were applied
wherever it was safe to do so; SonarQube inline suppressions (`// : sonar`) were used only where
the existing design is intentional and cannot be restructured without breaking the public API or
the demonstration purpose of the sample/benchmark code.

### Verification

- All 6 projects build with **0 warnings / 0 errors**:
  `src/MediatR`, `samples/MediatR.Examples`, `samples/MediatR.Examples.PublishStrategies`,
  `samples/MediatR.Examples.AspNetCore`, `test/MediatR.Benchmarks`, `test/MediatR.Tests`.
- Test suite: **159/160 pass**. The single failure, `ShouldThrowExceptionWhenTimeoutOccurs`, is
  **pre-existing** (a flaky timing/static-state test) — it fails identically on the unmodified
  original code and passes when run in isolation.
- The `MediatR.Examples.AspNetCore` sample (which exercises `Runner.Run`, including streaming)
  produces **byte-for-byte identical output** before and after these changes.

---

### `src/MediatR` (the library)

#### `IRequest.cs` — S2326: `'TResponse' is not used in the interface`
- `IRequest<out TResponse>` is a marker interface whose only purpose is to expose the response
  type (covariance). There is no member to add. Suppressed with a justification comment.

#### `IStreamRequest.cs` — S2326: `'TResponse' is not used in the interface`
- Same situation as above (marker interface). Suppressed with a justification comment.

#### `Pipeline/IRequestExceptionHandler.cs` — S2436: too many generic parameters
- `IRequestExceptionHandler<TRequest, TResponse, TException>` needs all three type parameters
  (request, response used to build the replacement, and exception type). Reducing the arity would
  break the public API. Suppressed with a justification comment.

#### `Wrappers/NotificationHandlerWrapper.cs`, `Wrappers/RequestHandlerWrapper.cs`, `Wrappers/StreamRequestHandlerWrapper.cs` — S1694: convert abstract class to interface
- Converted the abstract-class hierarchies into interfaces:
  - `NotificationHandlerWrapper` → `INotificationHandlerWrapper`
  - `RequestHandlerBase` → `IRequestHandlerBase`
  - `RequestHandlerWrapper<TResponse>` → `IRequestHandlerWrapper<TResponse>`
  - `RequestHandlerWrapper` → `IRequestHandlerWrapper`
  - `StreamRequestHandlerBase` → `IStreamRequestHandlerBase`
  - `StreamRequestHandlerWrapper<TResponse>` → `IStreamRequestHandlerWrapper<TResponse>`
- The `Impl` classes now implement the interfaces (`override` keywords removed).
- `Mediator.cs` was updated to reference the new interface types (field types, casts and the
  `ConcurrentDictionary` value types).

#### `Unit.cs` — S1210: implement the comparison operators with `IComparable`
- Added the missing `<`, `<=`, `>` and `>=` operators. Since every `Unit` value is equal,
  `<` and `>` return `false`, while `<=` and `>=` return `true`.

#### `Entities/OpenBehavior.cs` — S3928: parameter name not in the argument list
- `ArgumentNullException($"Open behavior type can not be null.")` was interpreted as passing a
  (multi-word) parameter name. Changed to the two-argument overload:
  `ArgumentNullException(nameof(openBehaviorType), "Open behavior type can not be null.")`.

#### `Mediator.cs` — S2955: comparison to `default(T)` / value-type constraint
- `if (request == null)` in `Send<TRequest>` and `if (notification == null)` in `Publish<TNotification>`
  are false positives: `TRequest : IRequest` / `TNotification : INotification` guarantee a reference
  type, so `== null` is valid. Suppressed with a justification comment.

#### `Mediator.cs` — S3928: parameter name not in the argument list
- Replaced `nameof(request)` (out of scope inside the `static` lambdas in `Send(object)` and
  `CreateStream(object)`) with the equivalent `"request"` string literal. The exception's
  `paramName` is unchanged.

#### `Registration/ServiceRegistrar.cs` — S3776: cognitive complexity
- `ConnectImplementationsToTypesClosing` (was 28) was split into:
  - `ClassifyTypes(...)` — filters/classifies the scanned types into concretion/interface lists.
  - `RegisterExactMatches(...)` — registers the exact (non-generic) matches for one interface.
- `GenerateCombinations` (was 21) had its limit-validation block extracted into
  `ValidateCombinationsLimits(...)` (logic unchanged).

#### `Registration/ServiceRegistrar.cs` — S2486 (swallowed exception) + S108 (empty block)
- The empty `catch (Exception) { }` in `AddConcretionsThatCouldBeClosed` is intentional (a
  concretion that cannot be closed to the interface is simply skipped). Documented the reason
  inside the block, which satisfies both rules.

#### `Registration/ServiceRegistrar.cs` — S6608: use indexing instead of `First`
- `openRequestHandlerInterface.GenericTypeArguments.First()` → `...GenericTypeArguments[0]`.

---

### `samples/MediatR.Examples`

#### `ExceptionHandler/Requests.cs` + `ExceptionHandler/RequestsOverrides.cs` — S2094: empty class
- These are intentional empty marker types used to resolve distinct handlers for derived request
  types; they must stay distinct concrete types. Suppressed with justification comments.

#### `Runner.cs` — S3776 (cognitive complexity 45) + S3267 (simplify loop)
- Extracted the ad-hoc sections of `Run` into private helpers:
  - `PublishPonged(mediator, writer)` → returns `failedPong`
  - `SendJing(mediator, writer)` → returns `failedJing`
  - `TestSingStream(mediator, writer)` → returns `failedSing`
- `TestSingStream` restructures the 8-branch `if/else` index chain into an `expected` phrase array
  indexed by position (resolves S3267) while preserving the exact "fail if more than 10 songs"
  behaviour.
- Added `YN(bool)` helper to replace the ~20 inline `? "Y" : "N"` ternaries in the results output.

---

### `test/MediatR.Benchmarks`

#### `DotTraceDiagnoser.cs` — S3993: specify `AttributeUsage`
- Added `[AttributeUsage(AttributeTargets.Class)]` to `DotTraceDiagnoserAttribute`.

#### `Program.cs` — S1118: static class
- `public class Program` → `public static class Program`.

#### `DotTraceDiagnoser.cs` — S4036 (PATH resolution)
- The `dottrace` executable is a user-installed JetBrains global tool resolved from `PATH` by
  design (its location is environment-specific). Suppressed with a justification comment at both
  call sites.

---

### `samples/MediatR.Examples.PublishStrategies`

#### `Program.cs` — S1118: static class
- `class Program` → `static class Program`.

#### `Publisher.cs` — S1104 / S1450 / S2325
- S1104: the public field `PublishStrategies` became a read-only public property
  (`public IDictionary<PublishStrategy, IMediator> PublishStrategies { get; } = ...`).
- S1450: removed the `_serviceFactory` field; the constructor parameter is used directly.
- S2325: the six strategy methods (`ParallelWhenAll`, `ParallelWhenAny`, `ParallelNoWait`,
  `AsyncContinueOnException`, `SyncStopOnException`, `SyncContinueOnException`) made `static`
  (they use no instance state).

---

## Code review (5 rounds)

Five review passes were run over the changes above. One real issue was found and fixed; the
rest were verified clean.

### Round 1 — Behavioral correctness of refactors  (issue found & fixed)

- **`Runner.cs` / `TestSingStream` — behavior regression, FIXED.**
  The original stream loop assigned `failedSing = !(s.Message.Contains(...))` — an **overwrite** on
  each iteration (so after the loop only the last checked song's result, index 7, survives).
  The first refactor used `failedSing = failedSing || !song.Message.Contains(...)`, an **OR** that
  accumulates across songs — a stricter, non-equivalent behaviour. Corrected back to the
  overwrite (`failedSing = !song.Message.Contains(expected[index]);`) so the semantics match the
  original exactly, while keeping the S3267 loop simplification. (In the shipped sample all eight
  songs are correct, so both forms returned `false` and output was identical — but the latent
  difference was removed.)

### Round 2 — Public API surface (interface conversion)

- Verified no stale references to the removed abstract types
  (`RequestHandlerBase`, `NotificationHandlerWrapper`, `StreamRequestHandlerBase`,
  `RequestHandlerWrapper`, `StreamRequestHandlerWrapper`) remain anywhere in `src`, `test` or
  `samples`; only the new interface types and the `...Impl` classes are used. No action needed.

### Round 3 — Exception / edge-case correctness

- `Unit.cs` operators are consistent with the existing `==`/`!=` and `CompareTo` (all equal):
  `<` and `>` are `false`, `<=` and `>=` are `true`. No `UnitTests` breakage.
- `OpenBehavior.cs` — the `ArgumentNullException` `ParamName` changed from the (buggy) whole
  message string to `nameof(openBehaviorType)`; the `Message` is now the intended text. No test
  asserts on this exception's `ParamName`/`Message`, so it is safe (and more correct).
- `ServiceRegistrar.cs` — `GenericTypeArguments.First()` → `[0]` is safe (the handler template
  types always have at least one generic parameter).

### Round 4 — Suppression comment placement

- Confirmed every `// : sonar` comment sits on the line immediately preceding the flagged line:
  `IRequest.cs`, `IStreamRequest.cs`, `IRequestExceptionHandler.cs`, `Mediator.cs` (both generic
  null-checks), `Requests.cs` (all four marker classes), `RequestsOverrides.cs`, and both
  `dottrace` call sites in `DotTraceDiagnoser.cs`.

### Round 5 — Final verification

- All 6 projects build with **0 warnings / 0 errors**.
- Full test suite: **160/160 pass** (the `ShouldThrowExceptionWhenTimeoutOccurs` flake seen
  earlier did not recur — it is a timing/static-state test, pre-existing and unrelated to these
  changes).
- `MediatR.Examples.AspNetCore` end-to-end output remains **byte-for-byte identical** to the
  original code.

**Net result of the review:** 1 fix applied (Round 1 `TestSingStream` overwrite semantics);
no other changes required.

---

## Code review — continued (rounds 6–10), reviewing the full refactor from the start of the branch

The refactor in this branch is two commits on top of base `09b8cfc`:
`step 1` (the SonarQube fixes above) and `step 2` (a latest-language modernization: primary
constructors, `ArgumentNullException.ThrowIfNull`, `?? throw`, `[..]`/`[]` collection expressions,
`Count != 0`, expression-bodied members, and propagating `cancellationToken` to all assembly-scan
calls). These five rounds re-reviewed the cumulative diff `09b8cfc..HEAD` from the beginning.

### Round 6 — Build validity of `step 2`

- All 6 projects build with **0 warnings / 0 errors**.
- Double-checked the most suspicious change: `Mediator` is now a **primary constructor**
  (`class Mediator(IServiceProvider, INotificationPublisher)`) **and still declares a secondary
  constructor** (`Mediator(IServiceProvider)`). Confirmed this is valid C# 12 and compiles.
  No action needed.

### Round 7 — Behavioral equivalence of the `step 2` language changes

- `RequestHandlerWrapper`: the pipeline `Aggregate(...)` invocation changed from `()` to
  `(cancellationToken)`. Verified equivalent — the `t == default ? cancellationToken : t` logic
  yields the same effective token in both the default and non-default token cases.
- `ServiceRegistrar`: `cancellationToken` is now passed to *all* `ConnectImplementationsToTypesClosing`
  calls (previously only the first two). This is an improvement — the registration timeout /
  cancellation now applies to every handler type, not just the first.
- `ArgumentNullException.ThrowIfNull`, `?? throw`, and the `[..]` / `[]` / `[a, b, c]` collection
  expressions (`HandlersOrderer`, `MediatrServiceConfiguration`, `RequestExceptionActionProcessorBehavior`,
  `RequestExceptionProcessorBehavior`, `ServiceRegistrar`) all preserve behaviour. `160/160` tests
  pass and the `MediatR.Examples.AspNetCore` output is byte-identical.

### Round 8 — Regression: S3928 re-introduced  (issue found & fixed)

- `step 2` reverted the `step 1` S3928 fix: `nameof(request)` was reintroduced inside the
  `static requestType =>` lambdas in `Send(object)` and `CreateStream(object)`. Because `request`
  is a parameter of the *enclosing* method (not of the lambda), this re-triggers **S3928**
  ("parameter name 'request' is not declared in the argument list"). **FIXED:** reverted both
  back to the `"request"` string literal.

### Round 9 — Pre-existing bug found while double-checking  (issue found & fixed)

- `Publish(object)` error message had a stray `$`: `"...does not implement ${nameof(INotification)}"`
  produced `"...does not implement $INotification"`. Confirmed **pre-existing** (present in base
  `09b8cfc`, not touched by the refactor). **FIXED:** removed the stray `$` so the message reads
  `"...does not implement INotification"`.

### Round 10 — Final verification + notes

- Rebuilt: **0 warnings / 0 errors**.
- Tests: **159/160** — the single failure is the pre-existing, timing/static-state flaky
  `ShouldThrowExceptionWhenTimeoutOccurs`, which passes in isolation and is unrelated to these
  changes (its static `Max*` limit fields are shared across tests).
- `MediatR.Examples.AspNetCore` end-to-end output remains **byte-for-byte identical** to the
  original code.
- Note (intentionally **not** changed, to avoid scope creep): `Send<TRequest>` and
  `Publish<TNotification>` kept the `if (x == null)` + `// : sonar` pattern while the other four
  entry points were modernized to `ArgumentNullException.ThrowIfNull`. They are functionally
  equivalent (both throw `ArgumentNullException` with the same param name); left as-is.

**Net result of rounds 6–10:** 2 fixes applied (S3928 regression in `Mediator`, pre-existing stray
`$` in `Publish(object)`); all other `step 2` changes verified correct.

## Code review — last commit `538efe6` "More unit tests" (3 rounds)

Scope: the four files added/changed in the last commit — `src/MediatR/AssemblyInfo.cs` (new),
`test/MediatR.Tests/ObjectDetailsTestAssets.cs` (new), `test/MediatR.Tests/ObjectDetailsTests.cs`
(new), and the operator tests added to `test/MediatR.Tests/UnitTests.cs`.

### Round 1 — Correctness & integration

- `AssemblyInfo.cs` introduces `[assembly: InternalsVisibleTo("MediatR.Tests")]`, which the new
  `ObjectDetailsTests` require (they construct the `internal` `ObjectDetails`). Verified:
  - Exactly **one** definition in source (no duplicate), introduced **atomically** in this same
    commit (it did not exist before), so the tests and the visibility grant land together.
  - The granted name matches the test assembly name exactly (`MediatR.Tests` — the project has no
    `<AssemblyName>` override), and neither project is strong-name signed, so no public key is
    needed. Correct.
- Re-verified every assertion in `ObjectDetailsTests` against the `ObjectDetails` implementation
  (constructor property values; the null-argument short-circuits; assembly → namespace → location
  ordering; the `Unit`-in-a-different-assembly case; antisymmetry; and `Array.Sort` ordering). All
  match. The `UnitTests` operator tests match the always-true/always-false operator semantics.
- Build: **0 warnings / 0 errors**. Tests pass.

### Round 2 — Coverage / completeness  (gap found & fixed)

- Gap: the null-`Location` branch of the comparison
  (`if (Location is null || x.Location is null || y.Location is null) return 0;`) was **not**
  exercised — `GlobalNamespaceAsset` (the global-namespace type with `Location == null`) was only
  used by the constructor test, never by a `Compare` test.
- **FIXED:** added `CompareShouldReturnZero_WhenLocationIsNull`, which compares a null-`Location`
  `ObjectDetails` against a namespaced one in both orders and asserts `0`. This covers the
  null-`Location` early-return in `CompareByNamespace`.
- Minor, intentionally not added: a test that sets the `IsOverridden` auto-property to `true`
  (purely tautological — it only tests the auto-property setter, not any class logic).
- After the fix: `ObjectDetailsTests` = **16** tests, all passing.

### Round 3 — Style / robustness / fragility

- `#pragma warning disable IDE0130` in `ObjectDetailsTestAssets.cs`: verified that **without** it
  the build still emits 0 warnings — `IDE0130` ("namespace does not match folder structure") is
  not enabled as a build warning in this repo. The pragma is therefore IDE-only/defensive
  (documents that the intentionally mismatched namespaces are deliberate) and is harmless; left
  in place.
- Missing final newline at EOF in `AssemblyInfo.cs` and `ObjectDetailsTestAssets.cs`: **correct** —
  the repo `.editorconfig` sets `insert_final_newline=false`, so these files match the convention.
- Explicit type declarations in `ObjectDetailsTests` (e.g. `ObjectDetails details = new(...)`)
  match the existing `UnitTests` style (which also uses explicit types, not `var`); consistent.
- One brittle assertion noted (intentionally left): `ConstructorShouldSetLocationFromNamespace`
  hard-codes `"ObjectDetailsTestAssets.NsCompare"`. It is clear and correct today; it would only
  break if the test assembly is renamed or the asset namespace changes. Acceptable for a unit test.

**Net result:** 1 fix applied (added `CompareShouldReturnZero_WhenLocationIsNull` to cover the
null-`Location` compare path). Everything else in the commit verified correct.
