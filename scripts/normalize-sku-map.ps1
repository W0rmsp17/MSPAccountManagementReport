param(
    [string] $SourcePath = "..\Data\skus.json",
    [string] $OutputPath = ".\MSPAccountManagementReport\data\m365-sku-map.json"
)

$ErrorActionPreference = "Stop"

$resolvedSource = Resolve-Path -LiteralPath $SourcePath
$items = Get-Content -LiteralPath $resolvedSource -Raw | ConvertFrom-Json

$skus = [ordered]@{}
foreach ($item in $items) {
    $displayName = $item.'Product name'
    $skuPartNumber = $item.'String ID'
    $skuId = $item.GUID

    if ([string]::IsNullOrWhiteSpace($displayName) -or
        [string]::IsNullOrWhiteSpace($skuPartNumber) -or
        [string]::IsNullOrWhiteSpace($skuId)) {
        continue
    }

    if ($skus.Contains($skuPartNumber)) {
        continue
    }

    $skus[$skuPartNumber] = [ordered]@{
        displayName = $displayName.Trim()
        skuPartNumber = $skuPartNumber.Trim()
        skuId = $skuId.Trim()
    }
}

$catalog = [ordered]@{
    schemaVersion = "1.0"
    source = "Microsoft licensing service plan reference"
    sourceUrl = "https://learn.microsoft.com/en-us/entra/identity/users/licensing-service-plan-reference"
    generatedUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    skuCount = $skus.Count
    skus = $skus
}

$resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutput
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$catalog | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resolvedOutput -Encoding utf8

Write-Host "Wrote $($skus.Count) SKU mappings to $resolvedOutput"
