$ErrorActionPreference = "Stop"

$outputPath = Join-Path ".out" "smoke-result.json"
$env:CONTROL_PLANE_OUTPUT_PATH = $outputPath

try {
    dotnet run --project .\MSPAccountManagementReport\MSPAccountManagementReport.csproj -- .\samples\job-input.json

    if (-not (Test-Path -LiteralPath $outputPath)) {
        throw "Expected smoke output was not written: $outputPath"
    }

    $result = Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json

    if ($result.status -ne "Succeeded") {
        throw "Expected status 'Succeeded' but received '$($result.status)'."
    }

    if ($null -eq $result.metrics) {
        throw "Smoke output is missing metrics."
    }

    if ($null -eq $result.report) {
        throw "Smoke output is missing report."
    }

    if ($null -eq $result.report.licenseSummary -or $result.report.licenseSummary.items.Count -lt 1) {
        throw "Smoke output is missing license summary items."
    }

    if ($null -eq $result.report.userLicenses -or $result.report.userLicenses.items.Count -lt 1) {
        throw "Smoke output is missing user license items."
    }

    if ($null -eq $result.report.recommendations -or $result.report.recommendations.Count -lt 1) {
        throw "Smoke output is missing recommendations."
    }

    if ($null -eq $result.report.renderedReport -or [string]::IsNullOrWhiteSpace($result.report.renderedReport.content)) {
        throw "Smoke output is missing rendered report content."
    }

    Write-Host "Local smoke test passed. Output: $outputPath"
}
finally {
    Remove-Item Env:\CONTROL_PLANE_OUTPUT_PATH -ErrorAction SilentlyContinue
}
