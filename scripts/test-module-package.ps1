$ErrorActionPreference = "Stop"

$requiredFiles = @(
    "module.manifest.json",
    "module.manifest.schema.json",
    "module.output.schema.json",
    "Dockerfile",
    "README.md",
    "docs/control-plane-import.md",
    "docs/release.md",
    "MSPAccountManagementReport/MSPAccountManagementReport.csproj",
    "MSPAccountManagementReport/data/m365-sku-map.json",
    "scripts/test-local-smoke.ps1",
    "samples/control-plane-import-request.json",
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

if ($manifest.schemaVersion -ne "1.0") {
    throw "Unsupported manifest schemaVersion '$($manifest.schemaVersion)'. Expected '1.0'."
}

if ($manifest.id -notmatch "^[a-z0-9][a-z0-9-]{2,63}$") {
    throw "Manifest id '$($manifest.id)' must be lowercase kebab-case."
}

if ($manifest.version -notmatch "^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$") {
    throw "Manifest version '$($manifest.version)' must be semantic version compatible."
}

if ($manifest.image -notmatch ":[^/:]+$") {
    throw "Manifest image '$($manifest.image)' must include an explicit tag."
}

if ($manifest.image -notmatch ":$([regex]::Escape($manifest.version))$") {
    throw "Manifest image tag must match manifest version '$($manifest.version)'."
}

if ($manifest.timeoutSeconds -lt 1 -or $manifest.timeoutSeconds -gt 3600) {
    throw "Manifest timeoutSeconds must be between 1 and 3600."
}

if ($manifest.concurrency -lt 1) {
    throw "Manifest concurrency must be at least 1."
}

if ($manifest.requiredPermissions.Count -lt 1) {
    throw "Manifest must declare required permissions."
}

if ($manifest.supportedScopes.Count -lt 1) {
    throw "Manifest must declare supported scopes."
}

if ($manifest.outputsSchema.required -notcontains "report") {
    throw "Manifest outputsSchema must require the report object."
}

if ($manifest.outputsSchema.schema -ne "module.output.schema.json") {
    throw "Manifest outputsSchema must reference module.output.schema.json."
}

Write-Host "Module package validation passed for $($manifest.id) $($manifest.version)."
