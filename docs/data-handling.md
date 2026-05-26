# Data Handling

This module produces tenant account-management reporting data.
Generated outputs should be treated as customer-confidential operational data.

## Data Read

When live Graph collection is enabled, the module reads:

- subscribed SKU and license capacity information
- user display names
- user principal names
- account enabled state
- assigned license identifiers

## Data Written

The module writes a structured JSON result and rendered Markdown report content.
Outputs may contain:

- tenant license counts
- assigned and available license counts
- user license assignment summaries
- unlicensed user signals
- disabled licensed user signals
- account-management recommendations

The module does not intentionally write Graph access tokens, client secrets, app secrets, refresh tokens, or tenant credentials.

## Recommended Classification

Manifest classification: `CustomerConfidential`

The output is not secret material, but it can identify users and customer licensing posture.
Store artifacts in the control plane using tenant-aware access controls.

## Recommended Retention

Default recommendation: 90 days

MSPs may choose a shorter or longer retention period depending on customer contract terms and account-review practices.
Longer retention can support trend reporting, but increases the amount of customer operational data stored by the platform.

## Downstream Consumers

If the output is forwarded to a webhook, PSA, dashboard, or bring-your-own AI/reporting workflow, the control plane should make that data movement visible to the operator and apply the MSP/customer data handling policy.
