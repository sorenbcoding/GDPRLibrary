# SB-SEC-005 Call to service outside approved regions

| | |
| --- | --- |
| ID | `SB-SEC-005` |
| Scope | Activity |
| Default severity | Error |
| Package | `SorenB.Rules.Privacy` |
| Added in | 2.1.0 |

## What it checks

This rule finds web addresses (`http://` and `https://`) written into a workflow whose host is not on the approved domains list. It looks at:
- **Activity properties:** every argument and property of every activity, for example:
  - `HTTP Request` (`EndPoint`, `RequestUrl`)
  - `Navigate To`, `Open Browser` and `Go To URL` (`Url`)
  - `Use Application/Browser`
  - `Assign` values
- **Variable defaults:** the default values of variables, such as `str_BaseURL = "https://…"`.

Each host is reported once per activity.

**Always approved:** intranet hosts, meaning:
- names without a dot (`http://intranet`)
- `localhost`
- private and loopback IP addresses (`10.x`, `172.16–31.x`, `192.168.x`, `127.x`, `::1`)

**Limitation:** an address built only from variables or assets, for example `str_BaseURL + "/search"`, can't be resolved when the project is analyzed. The rule checks the literal part, and it checks the variable where its default is set. An address read from an Orchestrator asset at run time is not checked.

## Why it matters

Data residency. A fixed call to a US-hosted API, translation service or file-sharing site sends data out of the EU without anyone noticing. That can happen without a data processing agreement or a data protection impact assessment (DPIA).

## How to fix

Use a service in an approved region. If the service is approved by your organisation, add its domain to the approved list centrally, rather than in each project.

## Parameters

| Parameter | Key | Default |
| --- | --- | --- |
| Approved domains | `approved_domains` | `*.dk;*.eu;*.uipath.com` |

- **List format:** semicolon-separated and case-insensitive.
- **Wildcard entries:** `*.dk` approves `dk` itself and every subdomain (`borger.dk`, `app.studica.dk`).
- **Exact entries:** `api.example.com` approves only that host.

**Maintaining the list:** the default is a strict starting point. The list your developers work with should be set once by whoever owns compliance (DPO, IT security or the digitalisation team):
1. In Automation Ops, open the Studio policy for your developers.
2. On the **Workflow Analyzer** tab, find `SB-SEC-005` and set `approved_domains`. For example, add Microsoft 365 endpoints (`graph.microsoft.com;login.microsoftonline.com;*.sharepoint.com`) once your EU Data Boundary setup is approved.
3. Deploy the policy. When the ruleset is enforced, developers can't change the list in their projects.

See the [governance baseline](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/governance-baseline.md) for the full setup.

## Example messages

```
Activity 'HTTP Request' uses the address 'api.example.com' (RequestUrl), which is not on the approved domains list. Data sent to a service outside the approved regions may leave the EU without a data processing agreement. Use a service in an approved region, or have the domain approved and added to approved_domains in your Automation Ops policy.
```

```
Variable 'str_BaseURL' holds the address 'distribution.virk.example.com', which is not on the approved domains list. Data sent to a service outside the approved regions may leave the EU without a data processing agreement. Use a service in an approved region, or have the domain approved and added to approved_domains in your Automation Ops policy.
```

---

[All rules](https://github.com/sorenbcoding/privacy-compliance-rules#rules) · Privacy Compliance Rules by Søren Schytz Birk
