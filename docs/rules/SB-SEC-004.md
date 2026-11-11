# SB-SEC-004 Personal data in queue item

| | |
| --- | --- |
| ID | `SB-SEC-004` |
| Scope | Activity |
| Default severity | Error |
| Package | `SorenB.Rules.Privacy` |
| Added in | 2.1.0 |

## What it checks

This rule finds personal data put into Orchestrator queue items by `Add Queue Item`, `Add Transaction Item` and `Bulk Add Queue Items`. It checks:
- **`Reference`:** names that point to personal data, and CPR numbers.
- **`ItemInformation` fields:** each field's name (it becomes a key of `SpecificContent`) and its value.
- **The item collection or DataTable expression:** the names it uses, for example `dt_Ansøgere`.

The activity's settings (`QueueType`, `FolderPath`, `Priority`, dates) are not item data and are not checked.

Names are matched as whole words, the same way as in [`SB-SEC-003`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-003.md). A field `ElevNavn` matches; a field `Holdnavn` doesn't.

**Limitation:** the columns of a DataTable passed to `Bulk Add Queue Items` are only known at run time, so the rule only sees the DataTable's name.

## Why it matters

Queue items are stored in Orchestrator for as long as the queue's retention policy keeps them. The `Reference` is shown in queue lists and indexed for search, so a CPR number or a name in it is visible to everyone with access to the queue.

## How to fix

Put a case key in the queue item, and look up the person when the item is processed.

| Before | After |
| --- | --- |
| `Reference`: `"Sag " + cprNummer` | `Reference`: `"Sag " + sagsId` |
| `ItemInformation`: `ElevNavn` = `row("Navn")` | `ItemInformation`: `BlanketID` = `row("BlanketID")` |

If a name is not about a person, add it to the ignored names.

## Parameters

The same parameters and defaults as [`SB-SEC-003`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-003.md#parameters):

| Parameter | Key |
| --- | --- |
| Names that point to personal data | `personal_data_names` |
| Names to ignore | `personal_data_ignored_names` |

Each rule has its own copy of the parameters, so set both rules to the same lists. Do it centrally through the Workflow Analyzer tab of your Automation Ops Studio policy. See the [governance baseline](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/governance-baseline.md).

## Example message

```
Activity 'Add Queue Item' adds the queue item field 'ElevNavn', which looks like personal data (elevnavn). Queue items are stored in Orchestrator, and the Reference is shown in queue lists and indexed for search. Put a case key in the queue item and look up the person when the item is processed. If the name is not about a person, add it to the ignored names.
```

---

[All rules](https://github.com/sorenbcoding/privacy-compliance-rules#rules) · Privacy Compliance Rules by Søren Schytz Birk
