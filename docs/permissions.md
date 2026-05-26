# Permissions

This module uses Microsoft Graph application permissions supplied by the control plane at runtime.
The module does not store Graph credentials or tenant secrets.

## Required Microsoft Graph Permissions

| Permission | Type | Why it is needed |
| --- | --- | --- |
| `Organization.Read.All` | Application | Reads tenant organization metadata and subscribed SKU information for account-management reporting. |
| `User.Read.All` | Application | Reads user profile and assigned license data to identify licensed, unlicensed, and disabled licensed accounts. |
| `Directory.Read.All` | Application | Provides directory read coverage for tenant-level reporting where user and license relationships require directory context. |

## Runtime Token Handling

The control plane should inject a short-lived Microsoft Graph bearer token through `GRAPH_ACCESS_TOKEN`.
The module uses the token only for the current execution and does not persist it in output artifacts.

When `GRAPH_ACCESS_TOKEN` is omitted, the module falls back to local sample data.
This supports local development and CI without tenant access.

## Consent Review Notes

Before assigning this module to a client connection, an operator should confirm:

- the client has approved tenant-level license and user reporting
- the control plane app registration has the required Graph application permissions
- admin consent has been granted in the target tenant
- generated report artifacts are stored according to the MSP/customer data handling policy

Future report sections, such as inactive user analysis from usage reports, may require additional permissions such as `Reports.Read.All`.
