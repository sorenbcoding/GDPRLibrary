<p align="center">
  <img src="branding/privacy-compliance-rules-light.svg#gh-light-mode-only" width="96" alt="Privacy Compliance Rules logo">
  <img src="branding/privacy-compliance-rules-dark.svg#gh-dark-mode-only" width="96" alt="Privacy Compliance Rules logo">
</p>

<h1 align="center">Privacy Compliance Rules</h1>
<p align="center">By Søren Schytz Birk</p>

Workflow Analyzer rules for UiPath Studio. They find personal data and other privacy risks (GDPR) in a project before it is published to Orchestrator.

## Install

Package ID: `SorenB.Rules.Privacy`. Built against `UiPath.Activities.Api` 23.10.3 (Studio 2023.10), targeting `net6.0`.

Download `SorenB.Rules.Privacy.<version>.nupkg` from [Releases](https://github.com/sorenbcoding/privacy-compliance-rules/releases), then use one of the two options below.

| Option | Scope | How |
| --- | --- | --- |
| Project dependency | One project | Add the folder or feed that holds the `.nupkg` as a package source. Then install `SorenB.Rules.Privacy` in **Manage Packages**. |
| Custom rules location | Every project in Studio | Extract `lib/net6.0/SorenB.Rules.Privacy.dll` from the `.nupkg` into the folder set in **Home > Settings > Locations > Custom Workflow Analyzer rules location**. Restart Studio. |

The rules then appear in **Project Settings > Workflow Analyzer** under their IDs.

### Enforce on publish

Both rules default to severity **Error**. When the Workflow Analyzer is enforced, a project that breaks a rule cannot be published.

- **Automation Cloud:**
  - In Automation Ops, open **Governance**. Edit the Studio policy for your developers.
  - On the **Design** tab, set **Enforce Analyzer before Publish** to **Yes**.
  - Deploy the policy to the users, groups or tenant.
- **File-based governance:** set `AnalyzeOnPublishParam` to `true`. You can point `AdditionalAnalyzerRulePathParam` at a shared folder that holds the rule DLL.

See UiPath's [Settings for Studio policies](https://docs.uipath.com/automation-ops/automation-cloud/latest/user-guide/settings-for-studio-policies) and [Governance](https://docs.uipath.com/studio/standalone/latest/user-guide/governance) for the current setting names in your version.

### Migrate from GDPR.Compliance.Workflow.Analyzer.Rules

Version 2.0.0 renames the package from `GDPR.Compliance.Workflow.Analyzer.Rules` to `SorenB.Rules.Privacy`.

1. Uninstall the old package, or remove its DLL from the custom rules location.
2. Install `SorenB.Rules.Privacy`.

The rule IDs (`SB-SEC-001`, `SB-SEC-002`) and the parameter key (`pin_in_code`) are unchanged, so the rule settings in your projects and policies carry over.

## Quick example

A variable `CustomerId` has the default value `"010190-1234"`. **Analyze File** reports:

```
SB-SEC-001  Variable 'CustomerId' has a personal identification number in its default value. Values in workflow files are stored in source control, in the published package and in Orchestrator. Clear the default value and assign the number at runtime.
```

## Reference

| ID | Title | Scope | Default severity |
| --- | --- | --- | --- |
| [`SB-SEC-001`](#sb-sec-001) | Personal identification number in workflow | Activity | Error |
| [`SB-SEC-002`](#sb-sec-002) | Screenshots folder in project | Project | Error |

### SB-SEC-001

**Personal identification number in workflow.** This rule finds personal identification numbers (PINs) in workflow source. By default it looks for Danish CPR numbers (`DDMMYY-NNNN` or `DDMMYYNNNN`).

It checks four places in each activity:
- the display name
- the annotation
- the argument values
- the variable default values

**Why:** values in workflow files are stored in source control, in the published package and in Orchestrator, where everyone with access can read them.

**Fix:** remove the number and read it at runtime from a queue item, an Orchestrator asset or another secure source.

| Parameter | Key | Default |
| --- | --- | --- |
| Regex for personal identification number | `pin_in_code` | `(0[1-9]\|[12]\d\|3[01])(0[1-9]\|1[0-2])\d{2}[-]?\d{4}` |

- To check another country's ID number, replace the pattern.
- To turn the check off without disabling the rule, clear the pattern.
- If the pattern is not a valid regular expression, the rule reports that once instead of checking.

### SB-SEC-002

**Screenshots folder in project.** This rule reports a `.screenshots` folder in the project folder.

**Why:** Studio stores screenshots of the target applications there, which can show personal data. The folder can end up in source control and in the published package.

**Fix:** delete the `.screenshots` folder before you publish, and add it to `.gitignore`.

No parameters.

## Changelog

See [CHANGELOG.md](CHANGELOG.md). Rule IDs are listed in [RULE_IDS.md](RULE_IDS.md).

## License

MIT. See [LICENSE](LICENSE).

---

By Søren Schytz Birk · UiPath Community MVP · [github.com/sorenbcoding](https://github.com/sorenbcoding)

Privacy Compliance Rules is an independent open-source project by Søren Schytz Birk (github.com/sorenbcoding) and is not affiliated with, endorsed by, or sponsored by UiPath. UiPath and related names are trademarks of UiPath Inc.
