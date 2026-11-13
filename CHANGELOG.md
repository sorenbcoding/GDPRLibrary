# Changelog

All notable changes to Privacy Compliance Rules are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [2.1.0] - 2026-11-13

### Added
- `SB-SEC-003` Personal data in log message: finds names that point to personal data, and CPR numbers, in `Log Message`, `Write Line` and `Add Log Fields`. The name list and ignore list are parameters. (#16)
- `SB-SEC-004` Personal data in queue item: finds personal data in the `Reference`, `ItemInformation` fields and item collection of `Add Queue Item`, `Add Transaction Item` and `Bulk Add Queue Items`. Shares its parameters with `SB-SEC-003`. (#17)
- `SB-SEC-005` Call to service outside approved regions: finds web addresses whose host is not on the `approved_domains` parameter (default `*.dk;*.eu;*.uipath.com`; intranet hosts are always approved). (#18)
- `SB-SEC-006` Password or secret in workflow: finds passwords, API keys, tokens and connection-string passwords written into a workflow as fixed values. (#19)
- [Recommended governance baseline](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/governance-baseline.md): the Automation Ops settings, central rule parameters, `ST-USG-014` package restrictions, feed lockdown and AI Trust Layer setup that complete these rules. (#20)

## [2.0.0] - 2026-11-11

### Changed
- **Breaking:** the package is renamed from `GDPR.Compliance.Workflow.Analyzer.Rules` to `SorenB.Rules.Privacy` (product name Privacy Compliance Rules). Rule IDs and the `pin_in_code` parameter key are unchanged. See [Upgrade from 1.x to 2.0](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/migrate-to-2.0.md). (#3)
- Every rule message now says what is wrong, why it matters and how to fix it. (#3)
- Each rule's help link opens its own page under `docs/rules/`. (#5)
- `SB-SEC-002` reports a Warning instead of an Error when the `.screenshots` folder is ignored by git, so it no longer blocks publishing in that case. It reads `.git/info/exclude` and the `.gitignore` files without needing the git CLI. (#9)
- Rules register only in Studio versions with the `WorkflowAnalyzerV4` feature. (#3)
- `UiPath.Activities.Api` is no longer a package dependency. (#3)
- MIT license. (#3)

### Fixed
- `SB-SEC-002` messages no longer say the `.screenshots` folder can end up in the published package; Studio leaves it out. (#13)
- `SB-SEC-002` reads the project's `.gitignore` when the project is not in a git repo, so the `.gitignore` Studio creates is respected. (#11)
- Both rules now set their default severity (Error) in `DefaultErrorLevel`, as the SDK requires. Before, they set the runtime `ErrorLevel`, which left the default at Off. (#7)
- `SB-SEC-001` no longer throws when the `pin_in_code` parameter is empty; it skips the check. (#1)
- `SB-SEC-001` reports an invalid `pin_in_code` pattern once instead of throwing for every activity. (#1)
- `SB-SEC-001` names the argument, not the activity, when an argument holds a personal identification number. (#1)

## [1.1.1] - 2025-08-07

### Fixed
- Version property and minor fixes.

## [1.1.0] - 2024-11-07

### Added
- `SB-SEC-002`: checks for a `.screenshots` folder in the project folder.

## [1.0.0] - 2024-09-23

### Added
- `SB-SEC-001`: checks activity display names, annotations, arguments and variable defaults for personal identification numbers (default: Danish CPR number).
