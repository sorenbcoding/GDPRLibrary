# Enforce the rules on publish

All Privacy Compliance Rules default to severity **Error**. When the Workflow Analyzer is enforced before publish, a project with an error cannot be published to Orchestrator. Warnings don't block publishing. For example, [`SB-SEC-002`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-002.md) reports only a warning when the `.screenshots` folder is git-ignored.

## Automation Cloud (Automation Ops)

1. In Automation Ops, open **Governance**, then create or edit the Studio policy for your developers.
2. On the **Design** tab, set **Enforce Analyzer before Publish** to **Yes**.
3. Deploy the policy to the users, groups or tenant that should follow it.

Developers also need the rules installed, as a project dependency or in a custom rules location. See [Install](https://github.com/sorenbcoding/privacy-compliance-rules#install).

## File-based governance (uipath.policies.config)

| Setting | Parameter | Value |
| --- | --- | --- |
| Enforce Analyzer before Publish | `AnalyzeOnPublishParam` | `true` |
| Workflow Analyzer rules location | `AdditionalAnalyzerRulePathParam` | Path to a shared folder that holds `SorenB.Rules.Privacy.dll` |

## Check the setting names for your version

Setting names and tabs change between UiPath versions. See UiPath's [Settings for Studio policies](https://docs.uipath.com/automation-ops/automation-cloud/latest/user-guide/settings-for-studio-policies) and [Governance](https://docs.uipath.com/studio/standalone/latest/user-guide/governance) for the current names.

---

[Back to README](https://github.com/sorenbcoding/privacy-compliance-rules#readme) · Privacy Compliance Rules by Søren Schytz Birk
