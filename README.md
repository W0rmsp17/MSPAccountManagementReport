# MSP Account Management Report

A snap-in module for the MSP Automation Control Plane.

This module is intended to produce account-management reporting that an MSP account manager can use for customer review conversations: tenant overview, license usage, unused license indicators, cost signals, and governance findings.

The first version is a contract-validating scaffold. It proves the standalone module repository, Docker image, module manifest, job input parsing, and result output path before adding Microsoft Graph collection logic.

## Module Contract

The module reads standard control-plane job input from either:

- a JSON file path passed as the first CLI argument
- `CONTROL_PLANE_JOB_INPUT_BASE64`
- `samples/job-input.json` when running locally from build output

The module writes output to:

- `CONTROL_PLANE_OUTPUT_BLOB_URI` when running inside the control plane
- `CONTROL_PLANE_OUTPUT_PATH` when supplied locally
- `.out/result.json` by default

## Local Run

```powershell
dotnet run --project .\MSPAccountManagementReport\MSPAccountManagementReport.csproj -- .\samples\job-input.json
```

Output is written to `.out/result.json`.

## Docker Build

```powershell
docker build -t msp-account-management-report:0.1.0 .
```

## Manifest

Register `module.manifest.json` in the control plane after replacing the `image` value with the published container image.

Initial required Microsoft Graph application permissions:

- `Organization.Read.All`
- `User.Read.All`
- `Directory.Read.All`

Future report sections may require additional permissions such as `Reports.Read.All`.

## SKU Friendly Names

The module ships with a local Microsoft 365 SKU lookup at `MSPAccountManagementReport/data/m365-sku-map.json`.
This lets reports show friendly names such as `Microsoft 365 Business Premium` when Graph returns technical values like `SPB`.

To regenerate the lookup from a source export:

```powershell
.\scripts\normalize-sku-map.ps1 -SourcePath ..\Data\skus.json
```

## Planned Report Sections

- tenant overview
- subscribed SKUs
- assigned license summary
- unused license count
- unlicensed users
- inactive users where reporting permissions allow
- account-management findings and recommendations
