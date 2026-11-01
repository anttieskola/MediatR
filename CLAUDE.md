# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Apache 2.0 fork of MediatR, targeting `net10.0` only. It must stay a **drop-in replacement** for the original library, so keep public type names, namespaces and signatures unchanged. That includes the public `MediatR.Wrappers` classes and the `Microsoft.Extensions.DependencyInjection` namespace on the registration extensions; both look like candidates for cleanup but are intentional.

## Commands

```powershell
dotnet build MediatR.slnx

# All tests (the test project uses Microsoft.Testing.Platform, set in global.json)
dotnet test --project test/MediatR.Tests/MediatR.Tests.csproj

# A subset: filters are passed directly, with no `--` separator. Wildcards work.
dotnet test --project test/MediatR.Tests/MediatR.Tests.csproj --filter-class "MediatR.Tests.SendTests"
dotnet test --project test/MediatR.Tests/MediatR.Tests.csproj --filter-method "MediatR.Tests.SendTests.Should_resolve_main_handler"

# Benchmarks (BenchmarkSwitcher)
dotnet run -c Release --project test/MediatR.Benchmarks -- --filter "*"

# Fetch the reports submodule
git submodule update --init --recursive
```

`sonar_build.cmd` is the full analysis and coverage run. It needs `SONAR_HOST` and `SONAR_TOKEN`, **kills every `dotnet.exe` on the machine**, and regenerates the coverage reports in `submodules/MediatR_Reports`, which is a separate repository. Don't run it just to check a change.

## Build rules

- `TreatWarningsAsErrors` is on for every project, and `LangVersion` is `latest` (`Directory.Build.props`).
- Package versions are managed centrally: add the version to `Directory.Packages.props` and leave `Version` off the `PackageReference`.
- Tests use xunit v3, Shouldly and Microsoft DI only. Third-party containers were deliberately removed.
- `MediatR.Tests` can see internals through `InternalsVisibleTo`.

## Architecture

All library code is in `src/MediatR`. The docs in `docs/topics/` cover each feature in depth; the points below are the ones that span several files.

**Dispatch.** `Mediator` can't call generic handlers directly from a runtime request type, so it creates a closed `*WrapperImpl<...>` from `Wrappers/` with `MakeGenericType` and calls that through a non-generic base class. The wrapper resolves the handler and behaviors from `IServiceProvider` on every call.

Wrappers are cached in **static** dictionaries keyed by request type, so the cache is shared by all `Mediator` instances and must hold no per-container state. The same goes for the registration limits, which `ServiceRegistrar` keeps in static fields and overwrites on each `AddMediatR` call.

**Pipeline order.** Wrappers reverse the `IPipelineBehavior` registrations and fold them around the handler, so the first one registered is the outermost. `ServiceRegistrar.AddRequiredServices` therefore sets the order through its registration sequence:

1. Exception behaviors. The default `ApplyForUnhandledExceptions` strategy puts the action behavior outside the handler behavior, so actions only see exceptions no handler dealt with.
2. Pre-processor behavior
3. Post-processor behavior
4. User behaviors from `BehaviorsToRegister`

**Conditional built-in behaviors.** These are easy to trip over when a test unexpectedly doesn't run one:

- The exception behaviors are added only if an implementation registered by type exists once scanning finishes, either from the scan or registered before `AddMediatR`. Factory and instance registrations aren't detected, and neither is anything registered after `AddMediatR`.
- The pre- and post-processor behaviors are added only when `RequestPreProcessorsToRegister` / `RequestPostProcessorsToRegister` are non-empty. `AutoRegisterRequestProcessors` registers the scanned processors but doesn't add these behaviors.

**Registration scanning.** `AddMediatRClasses` scans assemblies and registers everything as transient. Request and stream handlers use `TryAdd`, so the first wins; notification handlers, exception handlers, exception actions and processors are additive. Closing open generic handlers over concrete types is opt-in (`RegisterGenericHandlers`) and limited by the `Max*` settings and a timeout, because the number of combinations can grow very quickly.

## Conventions

- Library code in `src/` uses file-scoped namespaces and explicit local types rather than `var`. Follow it there even though `.editorconfig` still asks for block-scoped namespaces and `var`; `test/` and `samples/` are mixed.
- Sonar findings that can't be fixed without changing the original API are suppressed in place with `#pragma warning disable S…` or `// NOSONAR`. `IDE0079` is turned off so these suppressions don't get flagged.
- The README's "Changes after fork" section is a dated changelog. Add an entry for notable changes. The README also ships in the NuGet package.
