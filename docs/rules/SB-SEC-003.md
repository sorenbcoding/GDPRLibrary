# SB-SEC-003 Personal data in log message

| | |
| --- | --- |
| ID | `SB-SEC-003` |
| Scope | Activity |
| Default severity | Error |
| Package | `SorenB.Rules.Privacy` |
| Added in | 2.1.0 |

## What it checks

This rule finds personal data written to the robot log by three activities:

| Activity | What is checked |
| --- | --- |
| `Log Message` | The `Message` expression |
| `Write Line` | The `Text` expression |
| `Add Log Fields` | Every field: its name and its value expression |

It reports:
- **A name that points to personal data:** a variable, argument, column or field whose name matches the personal-data list, for example `cprNummer`, `row("ElevNavn")`, or a log field named `Fornavn`.
- **A CPR number** written as literal text.

Names are matched against **whole words**. `ElevNavn` is split into `elev` and `navn`, so it matches. `Holdnavn` is one word and is not on the list, so it doesn't match. Literal text such as `"Behandler ansøger"` describes something rather than referring to data, so only its CPR numbers are checked.

## Why it matters

Robot logs are stored in Orchestrator for the whole retention period, and Insights can copy them. Everyone with log access can read them. Logs are the most common way student and staff data leaks out of a robot.

## How to fix

Log a case key or a count instead, and look up the person in the system that owns the data.

| Before | After |
| --- | --- |
| `Log Message`: `"Behandler " + cprNummer` | `Log Message`: `"Behandler sag " + sagsId` |
| `Add Log Fields`: field `Fornavn` = `row("ElevNavn")` | `Add Log Fields`: field `BlanketID` = `row("BlanketID")` |

If a name is not about a person, for example `StudieNavn` (a programme's name), add it to the ignored names.

## Parameters

| Parameter | Key | Default |
| --- | --- | --- |
| Names that point to personal data | `personal_data_names` | `cpr;cprnr;cprnummer;personnummer;ssn;navn;fornavn;efternavn;mellemnavn;elevnavn;surname;elev;studerende;kursist;ansøger;email;telefon;telefonnummer;mobil;mobilnummer;phone;adresse;fødselsdato;foedselsdato;birthdate;værge;værger;forælder;forældre;fravær;fraværs;fraværsprocent;sygefravær` |
| Names to ignore | `personal_data_ignored_names` | `filnavn;arknavn;mappenavn;kolonnenavn;tabelnavn;holdnavn;kursusnavn;fagnavn;uddannelsesnavn;skolenavn;afdelingsnavn;firmanavn;servernavn;værtsnavn;domænenavn;procesnavn;kønavn;assetnavn` |

- Both lists are semicolon-separated and case-insensitive.
- An ignored name can be a whole identifier (`filnavn`) or a word together with the word before it: `Fil` + `Navn` is ignored as `filnavn`.
- Set both lists centrally for every developer through the Workflow Analyzer tab of your Automation Ops Studio policy. See the [governance baseline](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/governance-baseline.md).
- [`SB-SEC-004`](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/SB-SEC-004.md) uses the same parameter keys, so set both rules to the same lists.

## Example messages

```
Activity 'Add Log Fields - info om blanket/ansøger' adds the log field 'Fornavn', which looks like personal data (fornavn). Robot logs are stored in Orchestrator for the whole retention period, where everyone with log access can read them. Remove the field or log a case key instead. If the name is not about a person, add it to the ignored names.
```

```
Activity 'Log Message' logs 'cprNummer', which looks like personal data (cprnummer). Robot logs are stored in Orchestrator for the whole retention period, where everyone with log access can read them. Log a case key or a count instead. If the name is not about a person, add it to the ignored names.
```

---

[All rules](https://github.com/sorenbcoding/privacy-compliance-rules#rules) · Privacy Compliance Rules by Søren Schytz Birk
