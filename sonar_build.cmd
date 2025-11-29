@echo off
setlocal

REM Sonar build script
REM Requires environment variables:
REM SONAR_HOST - SonarQube server URL
REM SONAR_TOKEN - SonarQube authentication token
set "SONAR_PROJECT_KEY=MediatR"

REM Basic checks
if "%SONAR_HOST%"=="" (
  echo ERROR: SONAR_HOST not set
  exit /b 1
)
if "%SONAR_TOKEN%"=="" (
  echo ERROR: SONAR_TOKEN not set
  exit /b 1
)

REM Check dotnet
dotnet --version >nul 2>&1 || (
  echo "ERROR: dotnet not installed"
  exit /b 1
)

REM Check dotnet-coverage
dotnet-coverage --version >nul 2>&1 || (
  echo "ERROR: dotnet-coverage not installed"
  exit /b 1
)

REM Check dotnet-sonarscanner
dotnet sonarscanner >nul 2>&1 || (
  echo "ERROR: dotnet-sonarscanner not installed"
  exit /b 1
)

REM TODO: For some reason .sonarqube gets locked by dotnet process
REM Kill all dotnet processes
taskkill /f /im dotnet.exe

REM Prepare workspace - clean previous analysis data
if exist ".sonarqube" (
  echo "Removing .sonarqube"
  rmdir /s /q ".sonarqube"
)
if exist ".sonarqube" (
    echo "ERROR: Failed to remove .sonarqube"
    exit /b 1
)

REM Clean previous coverage
if exist "coverage.xml" (
  echo "Removing coverage.xml"
  del "coverage.xml"
)
if exist "coverage.xml" (
  echo "ERROR: Failed to remove coverage.xml"
  exit /b 1
)

REM Clean possible test results (used to generate reports)
rmdir /S/Q test\MediatR.Tests\TestResults

REM Begin Sonar
dotnet sonarscanner begin ^
  /k:"%SONAR_PROJECT_KEY%" ^
  /d:sonar.host.url="%SONAR_HOST%" ^
  /d:sonar.token="%SONAR_TOKEN%" ^
  /d:sonar.inclusions="**/src/**" ^
  /d:sonar.exclusions="**/samples/**,**/test/**" ^
  /d:sonar.coverage.exclusions="**/samples/**,**/test/**" ^
  /d:sonar.cs.vscoveragexml.reportsPaths=coverage.xml
if errorlevel 1 (
  echo Sonar begin failed
  exit /b 1
)

REM Build
dotnet build || (
  echo Build failed
  exit /b 1
)

REM Test
dotnet test --collect:"XPlat Code Coverage" --settings coverture.runsettings|| (
  echo Tests failed
  exit /b 1
)

REM Merge coverage
dotnet-coverage merge test/**/coverage.cobertura.xml -f cobertura -o coverage.xml|| (
  echo Coverage merge failed
  exit /b 1
)

REM End Sonar
dotnet sonarscanner end /d:sonar.token="%SONAR_TOKEN%" || (
  echo Sonar end failed
  exit /b 1
)

REM Generate html (uses TestResults folder created by dotnet test)
rmdir /S/Q submodules\MediatR_Reports\code_coverage_html
reportgenerator -reports:"test/**/*.cobertura.xml" -targetdir:"submodules\MediatR_Reports\code_coverage_html" -reporttypes:"HtmlInline_AzurePipelines_Dark"||(
  echo HTML report generation failed
  exit /b 1
)

REM Generate markdown (uses TestResults folder created by dotnet test)
rmdir /S/Q submodules\MediatR_Reports\code_coverage_md
reportgenerator -reports:"test/**/*.cobertura.xml" -targetdir:"submodules\MediatR_Reports\code_coverage_md" -reporttypes:"MarkdownSummary"||(
  echo Markdown report generation failed
  exit /b 1
)

endlocal
