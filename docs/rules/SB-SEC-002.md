# SB-SEC-002 Screenshots folder in project

| | |
| --- | --- |
| ID | `SB-SEC-002` |
| Scope | Project |
| Default severity | Error |
| Package | `SorenB.Rules.Privacy` |
| Added in | 1.1.0 |

## What it checks

This rule reports a `.screenshots` folder in the project folder.

## Why it matters

Studio stores screenshots of the target applications there, which can show personal data. The folder can end up in source control and in the published package.

## How to fix

Delete the `.screenshots` folder before you publish, and add it to `.gitignore`:

```
.screenshots/
```

## Parameters

No parameters.

## Example message

```
Folder .screenshots exists in project folder 'C:\Projects\InvoiceProcessing'. Studio stores screenshots of the target applications there, which can show personal data, and the folder can end up in source control and in the published package. Delete the .screenshots folder and add it to .gitignore.
```

---

[All rules](https://github.com/sorenbcoding/privacy-compliance-rules#rules) · Privacy Compliance Rules by Søren Schytz Birk
