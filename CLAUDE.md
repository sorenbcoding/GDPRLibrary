Owner: sorenbcoding
Brand: sorenb-brand

# CLAUDE.md

Workflow Analyzer rules for UiPath Studio that check projects for personal data and other privacy risks (GDPR). Brand category: `rules` (teal accent). Development follows the `sorenb-dev-cycle` skill.

## Commands

| Task | Command |
| --- | --- |
| Build (also packs the `.nupkg`) | `dotnet build -c Release` |
| Test | `dotnet test -c Release` |
| Brand check | `python ~/.claude/skills/sorenb-brand/scripts/brand_check.py . --category rules` |

`UiPath.Activities.Api` comes from the UiPath-Official feed, configured in `nuget.config`.

## Layout

- `src/SorenB.Rules.Privacy/`: the rule package. Each rule lives in `Rules/` and is registered in `RegisterAnalyzerConfiguration.cs`. Messages go through `RuleMessage.Format(what, why, fix)`.
- `tests/SorenB.Rules.Privacy.Tests/`: xUnit + NSubstitute. `BrandVoiceTests` checks every rule message against the brand voice.
- `branding/`: logo SVG + PNG. The light 128 px PNG is the package icon.
- `Directory.Build.props`: version and shared metadata. Product identity is in the src csproj.
- When adding a rule, also add its ID to `RULE_IDS.md`, a README reference section with anchor `#sb-xxx-nnn`, and a CHANGELOG entry.

## Facts

- No CI. Rely on local build, tests and brand check, and say so in PRs.
- Rule IDs are `SB-<AREA>-<NNN>`. Privacy and security rules use `SB-SEC-`. Never reuse or renumber an ID.
- Rule inspect methods are `internal` and tested via `InternalsVisibleTo`, with NSubstitute fakes for the UiPath model interfaces.
- Keep rule severity at `Error` for privacy rules so an enforced Workflow Analyzer blocks publishing.
- No releases or tags yet. Releases need the user's go-ahead.
