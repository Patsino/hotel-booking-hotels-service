# Run Tests with Coverage Report
# This script runs all tests and generates an HTML coverage report

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Hotels Service Test Coverage Report" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to solution directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Coverage paths
$coverageDir = "Tests\coverage"
$coverageCobertura = "Tests\coverage\coverage.cobertura.xml"
$coverageReportDir = "Tests\coverage\report"

# Clean previous coverage reports
if (Test-Path $coverageDir) {
    Write-Host "Cleaning previous coverage data..." -ForegroundColor Yellow
    Remove-Item $coverageDir -Recurse -Force
}

Write-Host ""
Write-Host "Running tests with coverage..." -ForegroundColor Green
Write-Host ""

# Run tests with coverage
$testResult = dotnet test Tests\Tests.csproj `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=cobertura `
    --verbosity normal `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Tests failed! Please fix failing tests before generating coverage report." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "All tests passed!" -ForegroundColor Green
Write-Host ""

# Check if coverage report was generated
if (-not (Test-Path $coverageCobertura)) {
    Write-Host "Coverage report not found. Please check Coverlet configuration." -ForegroundColor Red
    exit 1
}

# Check if ReportGenerator is installed
Write-Host "Checking for ReportGenerator tool..." -ForegroundColor Cyan
$reportGenInstalled = dotnet tool list -g | Select-String "dotnet-reportgenerator-globaltool"

if (-not $reportGenInstalled) {
    Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-reportgenerator-globaltool
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install ReportGenerator." -ForegroundColor Red
        exit $LASTEXITCODE
    }
    
    Write-Host "ReportGenerator installed successfully!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Generating HTML coverage report..." -ForegroundColor Green

# Generate HTML report
reportgenerator `
    -reports:$coverageCobertura `
    -targetdir:$coverageReportDir `
    -reporttypes:"Html;Badges" `
    -verbosity:Warning

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to generate coverage report." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Coverage report generated successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Report location: Tests\coverage\report\index.html" -ForegroundColor Cyan
Write-Host ""

# Display coverage summary
$coverageIndex = "Tests\coverage\report\index.html"
if (Test-Path $coverageIndex) {
    Write-Host "Opening coverage report in browser..." -ForegroundColor Green
    Start-Process $coverageIndex
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Test Coverage Report Complete!" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
