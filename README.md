# MSP Account Management Report

A snap-in module for the MSP Automation Control Plane.

This module is intended to produce account-management reporting that an MSP account manager can use for customer review conversations: tenant overview, license usage, unused license indicators, cost signals, and governance findings.

The first version validates the standalone module repository, Docker image, module manifest, job input parsing, result output path, subscribed SKU license-summary report shape, user license assignment summary, account-management recommendations, and a rendered Markdown report body.

## Module Contract

The module reads standard control-plane job input from either:

- a JSON file path passed as the first CLI argument
- `CONTROL_PLANE_JOB_INPUT_BASE64`
- `samples/job-input.json` when running locally from build output

The module writes output to:

- `CONTROL_PLANE_OUTPUT_BLOB_URI` when running inside the control plane
- `CONTROL_PLANE_OUTPUT_PATH` when supplied locally
- `.out/result.json` by default

Live Microsoft Graph collection is enabled when the controller supplies `GRAPH_ACCESS_TOKEN`.
Without that token, the module uses `samples/subscribed-skus.sample.json` and `samples/users.sample.json` so local runs and CI remain deterministic.

## Local Run

```powershell
dotnet run --project .\MSPAccountManagementReport\MSPAccountManagementReport.csproj -- .\samples\job-input.json
```

Output is written to `.out/result.json`.

The local sample includes Microsoft 365 Business Premium, one unknown SKU to exercise fallback behavior, licensed users, an unlicensed user, and a disabled licensed user.

Run the local executable smoke test:

```powershell
.\scripts\test-local-smoke.ps1
```

## Docker Build

```powershell
docker build -t msp-account-management-report:0.1.0 .
```

## Manifest

Register `module.manifest.json` in the control plane after publishing the container image.
The manifest declares the module image, runtime, entrypoint, supported scopes, parameter schema, required Graph permissions, and execution contract.

Validate the package before registration:

```powershell
.\scripts\test-module-package.ps1
```

`module.manifest.schema.json` documents the import contract expected by the control plane.
`module.output.schema.json` documents the structured output contract emitted by the module.
`docs/control-plane-import.md` describes the expected repository import flow.
`docs/release.md` describes the tag-and-publish workflow.
`samples/control-plane-import-request.json` provides a sample management-interface import request.

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

Current recommendations include disabled users with licenses, unlicensed users, and available license capacity.

The output also includes `report.renderedReport`, a Markdown report body intended for controller UI display, email body generation, or later artifact rendering.

## Downstream Consumers

The module treats structured JSON output as the source of truth.
The rendered report is a convenience layer, not the only supported consumption path.
Future platform consumers can use the same output payload for management UI display, email delivery, webhooks, PSA integrations, dashboards, or bring-your-own AI report generation without changing the module execution contract.
