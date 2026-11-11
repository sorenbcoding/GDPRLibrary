# Changelog

All notable changes to Privacy Compliance Rules are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed
- **Breaking:** the package is renamed from `GDPR.Compliance.Workflow.Analyzer.Rules` to `SorenB.Rules.Privacy` (product name Privacy Compliance Rules). Rule IDs and the `pin_in_code` parameter key are unchanged. See the migration note in the README. (#3)
- Every rule message now says what is wrong, why it matters and how to fix it. (#3)
- Each rule's help link opens its own page under `docs/rules/`. (#5)
- `SB-SEC-002` reports a Warning instead of an Error when the `.screenshots` folder is ignored by git, so it no longer blocks publishing in that case. It reads `.git/info/exclude` and the `.gitignore` files without needing the git CLI. (#9)
- Rules register only in Studio versions with the `WorkflowAnalyzerV4` feature. (#3)
- `UiPath.Activities.Api` is no longer a package dependency. (#3)
- MIT license. (#3)

### Fixed
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
