# MediatR

Fork of popular MediatR library licensed under Apache 2.0 license.

## License Information

[Licensed under the Apache License, Version 2.0 (the "License")](./LICENSE))

This product includes software developed by Original Author Jimmy Bogard.
Portions licensed under the Apache License, Version 2.0.

Code is based on the last version of MediatR library with Apache 2.0 license.
Forked maded from Commit SHA 8a64581e2570e7410c5f44835d9ea112beccbe6b.

All trademarks mentioned herein are the property of their respective owners.
All development after fork is licensed under the Apache License, Version 2.0.

**Can be used as drop-in replacement for MediatR library**

## Testing
- [Testing coverage](https://github.com/anttieskola/MediatR_Reports/blob/main/code_coverage_md/Summary.md)

## Changes after fork

### 2025-10-25 - Simplification and cleanup
- No changes to actuall library code, except removed MediatR.Contracts project/namespace as it only contained
  interfaces, which were moved to MediatR namespace
- Removed many 3rd party dependencies and examples (Autofac, Dryloc, Lamar, LightInject, Stashbox, Windsor)
- Refactored unit tests to not use 3rd party libraries (like Lamar) but instead .NET CORE libraries only
- Added global build properties
- Added central package versions
- Removed logos, workflows (non essential files)
- Update to .NET 9

### 2025-11-23 - Updated to .NET 10
- Updated to .NET 10

### 2025-11-24 - Sonerqube build
- Added sonarqube build script and adjusted test projects to generate code coverage reports for sonarqube

### 2025-11-29 - Report repository
- Created separate repository for reports (code coverage etc)
- Added it as submodule to this repository

### 2025-11-30 - Language version update
- Updated C# language version to latest (12)
- Took advantage of new features to simplify code where applicable

## Dotnet tools used
```bash
dotnet tool install --global dotnet-sonarscanner
dotnet tool install --global dotnet-coverage
dotnet tool install --global dotnet-reportgenerator-globaltool
```

## Performance benchmarks

### 2025-11-30
```
BenchmarkDotNet v0.13.10, Windows 11 (10.0.26100.7171)
AMD Ryzen 5 5600G with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2

```
| Method                  | Mean      | Error    | StdDev   |
|------------------------ |----------:|---------:|---------:|
| SendingRequests         | 131.35 ns | 2.559 ns | 3.417 ns |
| PublishingNotifications |  82.75 ns | 1.520 ns | 1.347 ns |
