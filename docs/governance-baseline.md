# Recommended governance baseline

Privacy Compliance Rules find personal data, secrets and calls to non-approved services inside a project. Some risks are better handled by UiPath's own platform controls. This page lists the built-in rules and Automation Ops settings that complete this package, so that compliance is met **before anyone publishes to Cloud Orchestrator**.

It is written for the administration and digitalisation teams in the Danish education sector, where GDPR and data residency are the main concerns. Check the setting names against your UiPath version; they change between releases.

## 1. Enforce the Workflow Analyzer on publish

In your Automation Ops Studio policy, set **Enforce Analyzer before Publish** to **Yes**. Every rule at severity Error then blocks publishing. See [Enforce the rules on publish](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/enforce-on-publish.md).

## 2. Set this package's parameters centrally

The **Workflow Analyzer** tab of the Automation Ops Studio policy enables rules, sets their severity and fills in rule parameters for every developer. When the ruleset is enforced, developers can't change it locally. The policy can hold the settings inside it, or reference a shared rules file (`RuleConfig.json` format).

| Rule | Parameter key | What your organisation decides |
| --- | --- | --- |
| [`SB-SEC-001`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-001.md) | `pin_in_code` | The ID-number pattern (default: Danish CPR) |
| [`SB-SEC-003`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-003.md), [`SB-SEC-004`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-004.md) | `personal_data_names`, `personal_data_ignored_names` | Which names count as personal data in your systems, and which don't. Set both rules to the same lists. |
| [`SB-SEC-005`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-005.md) | `approved_domains` | The services that are approved for your data. For example, add Microsoft 365 endpoints once your EU Data Boundary setup is approved. |
| [`SB-SEC-006`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-006.md) | `secret_names` | Variable names your developers use for secrets |

**Who maintains these lists:** whoever owns compliance, typically the DPO, IT security or the digitalisation team. Developers don't. Changes go through the policy and reach every developer on the next policy sync.

Reference: UiPath [Settings for Studio policies](https://docs.uipath.com/automation-ops/automation-cloud/latest/user-guide/settings-for-studio-policies) and [Governance](https://docs.uipath.com/studio/standalone/2024.10/user-guide/governance).

## 3. Restrict AI and third-party cloud packages (`ST-USG-014`)

There is no custom rule for this, because UiPath's built-in [`ST-USG-014` Package Restrictions](https://docs.uipath.com/studio/standalone/2024.10/user-guide/st-usg-014) already does it:
- **Lists:** it has an allowed list and a prohibited list.
- **Wildcards and versions:** `UiPath.*` allows only UiPath packages, and `Package>=1.2.0` restricts a version range.
- **Allowed list wins:** once any package is on the allowed list, every package not on it is prohibited.
- **Severity:** a violation is an Error, so with step 1 it blocks publishing.

Whether a GenAI or cloud package is compliant depends on how your tenant is configured (step 5), not on the workflow. So review these packages, and approve or prohibit each one:

| Package ID (verified on the UiPath Official feed, 2026-11-11) | Sends data to |
| --- | --- |
| `UiPath.OpenAI.IntegrationService.Activities` | OpenAI (US) |
| `UiPath.MicrosoftAzureOpenAI.IntegrationService.Activities` | Your Azure OpenAI resource, in the region you deploy it to |
| `UiPath.GoogleVertex.IntegrationService.Activities` | Google Vertex AI |
| `UiPath.GSuite.Activities` | Google Workspace |
| `UiPath.Dropbox.IntegrationService.Activities` | Dropbox |
| `UiPath.Box.IntegrationService.Activities` | Box |
| `UiPath.Slack.IntegrationService.Activities` | Slack |
| `UiPath.Salesforce.Activities` | Salesforce |
| `UiPath.MicrosoftTranslator.Activities` | Microsoft Translator |

**The safest setup is an allowed list:** list the packages you have approved (for example `UiPath.System.Activities;UiPath.UIAutomation.Activities;UiPath.Excel.Activities;UiPath.Mail.Activities;…`), and everything else is prohibited. A prohibited list only blocks what you thought of.

Use [`ST-USG-026`](https://docs.uipath.com/automation-ops/automation-cloud/latest/user-guide/settings-for-studio-web-policies) (activity restrictions) when you want to block individual activities but keep the rest of their package.

## 4. Lock the package feeds

In the Automation Ops Studio policy, keep users from adding or removing package sources. By default they can't. Then developers can only install packages from feeds you control, and your `ST-USG-014` lists can't be bypassed through a private feed.

## 5. Govern the models: AI Trust Layer

Which model a GenAI activity actually calls, and where that model runs, is configured in the tenant, not in the workflow:
- **AI Trust Layer policies** in Automation Ops control which AI features and model providers are allowed.
- **Bring your own LLM** (LLM Gateway) routes GenAI features to your own model subscription, for example an Azure OpenAI deployment in an EU region under your data processing agreement.
- **Self-hosted models:** check in UiPath's documentation for your version whether a fully self-hosted model can be used as a BYO endpoint.

Sending student or staff data to any model needs a data protection impact assessment (DPIA) and a data processing agreement.

## 6. Keep the rest of the defaults

- [`SB-SEC-002`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-002.md) needs no setup. Keep `.screenshots` in the `.gitignore` that Studio creates.
- Enable UiPath's SecureString rules `ST-SEC-007`, `ST-SEC-008` and `ST-SEC-009` alongside `SB-SEC-006`.

---

[Back to README](https://github.com/sorenbcoding/privacy-compliance-rules#readme) · Privacy Compliance Rules by Søren Schytz Birk
