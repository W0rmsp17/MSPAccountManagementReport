# Release

This module is released by publishing a versioned container image and registering the matching `module.manifest.json` in the control plane.

The manifest version and image tag must match.
The package validation script enforces that rule.

## Prepare a Release

Run from the repository root:

```powershell
.\scripts\prepare-release.ps1 -Version 0.1.0
```

The script updates:

- `module.manifest.json` `version`
- `module.manifest.json` `image`

It then runs:

```powershell
.\scripts\test-module-package.ps1
```

## Publish

After reviewing the manifest change:

```powershell
git add module.manifest.json
git commit -m "Release module 0.1.0"
git tag v0.1.0
git push
git push origin v0.1.0
```

The `Publish Module` GitHub Actions workflow builds and pushes:

- `ghcr.io/w0rmsp17/mspaccountmanagementreport/msp-account-management-report:0.1.0`
- `ghcr.io/w0rmsp17/mspaccountmanagementreport/msp-account-management-report:latest`

## Control Plane Registration

After the image is published, import or refresh the module registration using `module.manifest.json`.
The control plane should reject a manifest if the referenced image tag has not been published or cannot be pulled by the execution environment.
