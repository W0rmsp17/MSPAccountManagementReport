$ErrorActionPreference = "Stop"

$requiredFiles = @(
    "module.manifest.json",
    "Dockerfile",
    "README.md",
    "MSPAccountManagementReport/MSPAccountManagementReport.csproj",
    "MSPAccountManagementReport/data/m365-sku-map.json",
    "samples/job-input.json",
    "samples/subscribed-skus.sample.json",
    "samples/users.sample.json"
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file)) {
        throw "Required module package file is missing: $file"
    }
}

$manifest = Get-Content -LiteralPath "module.manifest.json" -Raw | ConvertFrom-Json

$requiredManifestProperties = @(
    "schemaVersion",
    "id",
    "name",
    "version",
    "repository",
    "image",
    "runtime",
    "entrypoint",
    "executionContract",
    "supportedScopes",
    "parametersSchema",
    "requiredPermissions",
    "outputsSchema"
)

foreach ($property in $requiredManifestProperties) {
    if ($null -eq $manifest.$property) {
        throw "Required manifest property is missing: $property"
    }
}

if ($manifest.runtime -ne "container-apps-job") {
    throw "Unsupported runtime '$($manifest.runtime)'. Expected 'container-apps-job'."
}

if ($manifest.requiredPermissions.Count -lt 1) {
    throw "Manifest must declare required permissions."
}

if ($manifest.supportedScopes.Count -lt 1) {
    throw "Manifest must declare supported scopes."
}

Write-Host "Module package validation passed for $($manifest.id) $($manifest.version)."
