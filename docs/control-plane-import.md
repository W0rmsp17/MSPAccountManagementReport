# Control Plane Import

This repository is intended to be imported by the MSP Automation Control Plane as a snap-in module.

The control plane should treat `module.manifest.json` as the registration source of truth.
The module execution payload remains separate from registration and trigger configuration.

## Import Flow

1. Operator supplies a repository URL and manifest path.
2. Control plane fetches `module.manifest.json`.
3. Control plane validates the manifest against `module.manifest.schema.json`.
4. Control plane verifies package expectations such as image tag, runtime, permissions, supported scopes, and output contract.
5. Control plane stores a module registration record.
6. Operator assigns the module to one or more client connections.
7. Manual, scheduled, webhook, or event triggers can create jobs for the registered module.

## Registration Request Shape

See `samples/control-plane-import-request.json`.

The request describes where the module comes from and how it should be initially registered.
It should not contain client secrets, Graph tokens, tenant IDs, or execution payload data.

## Execution Boundary

Import metadata answers:

- where is the module manifest
- what image should run
- what permissions are required
- what scopes and parameters are supported
- what output shape is expected

Job execution answers:

- which client connection is targeted
- who requested the run
- what target scope was selected
- what parameters were supplied
- where output artifacts should be written

Keeping these separate allows the same module package to be reused across tenants and trigger types.
