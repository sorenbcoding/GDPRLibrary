<p align="center">
  <img src="https://raw.githubusercontent.com/sorenbcoding/privacy-compliance-rules/main/branding/privacy-compliance-rules-light.svg#gh-light-mode-only" width="96" alt="Privacy Compliance Rules logo">
  <img src="https://raw.githubusercontent.com/sorenbcoding/privacy-compliance-rules/main/branding/privacy-compliance-rules-dark.svg#gh-dark-mode-only" width="96" alt="Privacy Compliance Rules logo">
</p>

<h1 align="center">Privacy Compliance Rules</h1>
<p align="center">By Søren Schytz Birk</p>

Workflow Analyzer rules for UiPath Studio. They find personal data and other privacy risks (GDPR) in a project before it is published to Orchestrator.

## Install

Package ID: `SorenB.Rules.Privacy`. Built against `UiPath.Activities.Api` 23.10.3 (Studio 2023.10), targeting `net6.0`. Download the `.nupkg` from [Releases](https://github.com/sorenbcoding/privacy-compliance-rules/releases).

| Option | Scope | How |
| --- | --- | --- |
| Project dependency | One project | Add the folder or feed that holds the `.nupkg` as a package source, then install `SorenB.Rules.Privacy` in **Manage Packages**. |
| Custom rules location | Every project in Studio | Extract `lib/net6.0/SorenB.Rules.Privacy.dll` from the `.nupkg` into the folder set in **Home > Settings > Locations > Custom Workflow Analyzer rules location**, then restart Studio. |

All rules default to severity Error. If the Workflow Analyzer is enforced on publish, a project with an error can't be published. See [Enforce the rules on publish](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/enforce-on-publish.md).

Upgrading from `GDPR.Compliance.Workflow.Analyzer.Rules`? See [Upgrade from 1.x to 2.0](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/migrate-to-2.0.md).

## Quick example

A variable `CustomerId` has the default value `"010190-1234"`. **Analyze File** reports:

```
SB-SEC-001  Variable 'CustomerId' has a personal identification number in its default value. Values in workflow files are stored in source control, in the published package and in Orchestrator. Clear the default value and assign the number at runtime.
```

## Rules

| ID | Title | Scope | Default severity |
| --- | --- | --- | --- |
| [`SB-SEC-001`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-001.md) | Personal identification number in workflow | Activity | Error |
| [`SB-SEC-002`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-002.md) | Screenshots folder in project | Project | Error (Warning if git-ignored) |
| [`SB-SEC-003`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-003.md) | Personal data in log message | Activity | Error |
| [`SB-SEC-004`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-004.md) | Personal data in queue item | Activity | Error |
| [`SB-SEC-005`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-005.md) | Call to service outside approved regions | Activity | Error |
| [`SB-SEC-006`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-006.md) | Password or secret in workflow | Activity | Error |

Each rule's help link in Studio opens its page.

## Changelog

See [CHANGELOG.md](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/CHANGELOG.md). Rule IDs are listed in [RULE_IDS.md](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/RULE_IDS.md).

## License

MIT. See [LICENSE](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/LICENSE).

---

By Søren Schytz Birk · UiPath Community MVP · [github.com/sorenbcoding](https://github.com/sorenbcoding)

Privacy Compliance Rules is an independent open-source project by Søren Schytz Birk (github.com/sorenbcoding) and is not affiliated with, endorsed by, or sponsored by UiPath. UiPath and related names are trademarks of UiPath Inc.
