@echo off
REM Generates an HTML code coverage report from coverage.xml
setlocal
if not exist "coverage.xml" (
    echo "ERROR: coverage.xml does not exist"
    exit /b 1
)

reportgenerator "-reports:coverage.xml" "-targetdir:coverage" -reporttypes:Html;MarkdownSummary|| (
  echo "ERROR: Report generation failed"
  exit /b 1
)
