param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$")]
    [string] $Version,

    [string] $ImageRepository = "ghcr.io/w0rmsp17/mspaccountmanagementreport/msp-account-management-report"
)

$ErrorActionPreference = "Stop"

$manifestPath = "module.manifest.json"
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Cannot find $manifestPath. Run this script from the repository root."
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$manifest.version = $Version
$manifest.image = "${ImageRepository}:${Version}"
$manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8

& "$PSScriptRoot\test-module-package.ps1"

Write-Host "Prepared module manifest for version $Version."
Write-Host "Image: $($manifest.image)"
Write-Host "Next steps:"
Write-Host "  git add module.manifest.json"
Write-Host "  git commit -m `"Release module $Version`""
Write-Host "  git tag v$Version"
Write-Host "  git push"
Write-Host "  git push origin v$Version"
