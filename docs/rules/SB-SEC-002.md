# SB-SEC-002 Screenshots folder in project

| | |
| --- | --- |
| ID | `SB-SEC-002` |
| Scope | Project |
| Default severity | Error. Warning if the folder is git-ignored |
| Package | `SorenB.Rules.Privacy` |
| Added in | 1.1.0 |

## What it checks

This rule reports a `.screenshots` folder in the project folder. The severity depends on whether git ignores the folder:

| Situation | Severity |
| --- | --- |
| `.screenshots` present and not listed in any `.gitignore` | The configured level (default Error) |
| `.screenshots` present and listed in `.gitignore` | Warning, or the configured level if that is lower |
| No `.screenshots` folder | No result |

An enforced Workflow Analyzer blocks publishing only on errors, so a git-ignored folder does not block publishing.

## Why it matters

Studio stores screenshots of the target applications there, which can show personal data.
- **Not ignored:** the folder can be committed to source control.
- **Ignored:** the folder stays out of source control, but it can still be included when the project is published.

## How to fix

Add the folder to `.gitignore`, and delete it before you publish:

```
.screenshots/
```

## How the rule decides "ignored"

The rule reads the ignore files itself, so it works on PCs without the git CLI. What it reads depends on whether the project is in a git repo:

- **In a git repo** (a `.git` folder in the project folder or above it):
  - `.git/info/exclude`
  - every `.gitignore` from the repo root down to the project folder
- **Not in a git repo:** the `.gitignore` in the project folder. Studio adds one to every new project, and it already lists `.screenshots`. Git applies it as soon as the project is put under source control.

The usual syntax works:
- comments
- `!` negation
- a trailing `/`
- leading `/` anchoring
- `*`, `?` and `**`

**Limitations:**
- The global ignore file (`core.excludesFile`) is not read. It is set per PC and not shared with the team, so the rule treats those entries as not ignored.
- Ignoring a folder does not remove files that were committed before the ignore entry was added. To stop tracking them, run `git rm -r --cached .screenshots`.

## Parameters

No parameters.

## Example messages

Not ignored (Error):

```
Folder .screenshots exists in project folder 'C:\Projects\InvoiceProcessing'. Studio stores screenshots of the target applications there, which can show personal data, and the folder can end up in source control and in the published package. Delete the .screenshots folder and add it to .gitignore.
```

Ignored (Warning):

```
Folder .screenshots exists in project folder 'C:\Projects\InvoiceProcessing' and is ignored by .gitignore. The screenshots stay out of source control but can still be included when the project is published. Delete the .screenshots folder before you publish.
```

---

[All rules](https://github.com/sorenbcoding/privacy-compliance-rules#rules) · Privacy Compliance Rules by Søren Schytz Birk
