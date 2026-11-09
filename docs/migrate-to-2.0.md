# Upgrade from 1.x to 2.0

Version 2.0.0 renames the package from `GDPR.Compliance.Workflow.Analyzer.Rules` to `SorenB.Rules.Privacy`. NuGet sees this as a new package, so updating the old one does not move you to 2.0.

## Steps

1. Uninstall `GDPR.Compliance.Workflow.Analyzer.Rules` from your projects, or delete its DLL from the custom rules location.
2. Install `SorenB.Rules.Privacy`. See [Install](https://github.com/sorenbcoding/privacy-compliance-rules#install).
3. Run **Analyze Project** and check that `SB-SEC-001` and `SB-SEC-002` are listed in **Project Settings > Workflow Analyzer**.

## What carries over

- **Rule IDs:** `SB-SEC-001` and `SB-SEC-002` are unchanged.
- **Parameter key:** `pin_in_code` is unchanged.

Because both are unchanged, the rule settings in your projects and governance policies (enabled state, severity, custom regex) carry over.

## What changes

- **Package ID, assembly and namespace:** all are now `SorenB.Rules.Privacy`.
- **Rule titles and messages:** each message now says what is wrong, why it matters and how to fix it.
- **Help links:** each rule's help link opens [its own page](https://github.com/sorenbcoding/privacy-compliance-rules/tree/main/docs/rules).

See the [CHANGELOG](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/CHANGELOG.md) for the full list.

---

[Back to README](https://github.com/sorenbcoding/privacy-compliance-rules#readme) · Privacy Compliance Rules by Søren Schytz Birk
