# SB-SEC-001 Personal identification number in workflow

| | |
| --- | --- |
| ID | `SB-SEC-001` |
| Scope | Activity |
| Default severity | Error |
| Package | `SorenB.Rules.Privacy` |
| Added in | 1.0.0 |

## What it checks

This rule finds personal identification numbers (PINs) in workflow source. By default it looks for Danish CPR numbers (`DDMMYY-NNNN` or `DDMMYYNNNN`).

It checks four places in each activity:
- the display name
- the annotation
- the argument values
- the variable default values

## Why it matters

Values in workflow files are stored in source control, in the published package and in Orchestrator, where everyone with access can read them.

## How to fix

Remove the number and read it at runtime from a queue item, an Orchestrator asset or another secure source.

| Before | After |
| --- | --- |
| Variable `CustomerId`, default `"010190-1234"` | Variable `CustomerId`, no default. Assigned from `in_TransactionItem.SpecificContent("CustomerId")` |
| Activity named `Look up 010190-1234` | Activity named `Look up customer` |

## Parameters

| Parameter | Key | Default |
| --- | --- | --- |
| Regex for personal identification number | `pin_in_code` | `(0[1-9]\|[12]\d\|3[01])(0[1-9]\|1[0-2])\d{2}[-]?\d{4}` |

Change the pattern in **Project Settings > Workflow Analyzer**:
- To check another country's ID number, replace the pattern.
- To turn the check off without disabling the rule, clear the pattern.
- If the pattern is not a valid regular expression, the rule reports that once instead of checking.

## Example message

```
Variable 'CustomerId' has a personal identification number in its default value. Values in workflow files are stored in source control, in the published package and in Orchestrator. Clear the default value and assign the number at runtime.
```

---

[All rules](https://github.com/sorenbcoding/privacy-compliance-rules#rules) · Privacy Compliance Rules by Søren Schytz Birk
