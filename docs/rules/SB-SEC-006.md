# SB-SEC-006 Password or secret in workflow

| | |
| --- | --- |
| ID | `SB-SEC-006` |
| Scope | Activity |
| Default severity | Error |
| Package | `SorenB.Rules.Privacy` |
| Added in | 2.1.0 |

## What it checks

This rule finds passwords, API keys, tokens and connection-string passwords written into a workflow as **fixed values**. It reports:

| What | Example |
| --- | --- |
| A fixed value in a secret property (`Password`, `SecurePassword`, `SecureText`, `ClientSecret`, `ConsumerSecret`, `OAuth2Token`, `ApiKey`, `Token`, `BasicAuthPassword`, certificate passwords, …) | `HTTP Request` with `Password` = `"abc123"` |
| A credential built from a fixed password | `New NetworkCredential("", "Sommer2026").SecurePassword` |
| A connection string with a password | `"Server=sql01;Database=Elever;Password=Sommer2026;"` |
| An `Assign` of a fixed value to a variable with a secret name | `str_Kodeord = "Sommer2026"` |
| A variable with a secret name and a fixed default value | `apiKey` with default `"sk-live-123"` |

**Not reported:**
- empty values and `{x:Null}`
- variables, arguments and `Get Credential` outputs
- placeholders such as `Password={0}`
- addresses: `str_TokenUrl = "https://…"` is configuration, not a secret

Secret names are matched as whole words of the variable name: `str_Password` matches.

The messages never repeat the secret itself.

## Why it matters

The published `.nupkg` contains the workflow as plain XAML, so everyone with access to the feed can read a fixed secret. The same goes for anyone who can see the source control repository. In the education sector, a leaked password often leads straight to student or HR systems.

## How to fix

Store the secret in an Orchestrator credential asset and read it at run time with `Get Credential`. For a connection string, store at least the password in a credential asset and build the string at run time.

| Before | After |
| --- | --- |
| `Type Into` `SecureText` = `New NetworkCredential("", "Sommer2026").SecurePassword` | `Get Credential` → `SecurePassword`, then `Type Into` `SecureText` = `secPassword` |
| `HTTP Request` `Password` = `"abc123"` | `Password` from a credential asset |

## Built-in UiPath rules

UiPath's own rules [`ST-SEC-007`](https://docs.uipath.com/studio/standalone/2023.10/user-guide/st-sec-007), [`ST-SEC-008`](https://docs.uipath.com/studio/standalone/2023.10/user-guide/st-sec-008) and [`ST-SEC-009`](https://docs.uipath.com/studio/standalone/2023.4/user-guide/st-sec-009) check how `SecureString` arguments and variables are passed and used, for example casting a `SecureString` back to `String`. They don't check whether a secret is written into the workflow as a fixed value, which is what `SB-SEC-006` does. Enable all four.

## Parameters

| Parameter | Key | Default |
| --- | --- | --- |
| Names of variables that hold secrets | `secret_names` | `password;pwd;passwd;secret;token;apikey;kodeord;adgangskode` |

The secret property names (`Password`, `ApiKey`, …) are built in.

Set the list centrally through the Workflow Analyzer tab of your Automation Ops Studio policy. See the [governance baseline](https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/governance-baseline.md).

## Example message

```
Activity 'HTTP Request' has a fixed value in 'Password'. The published package contains the workflow as plain XAML, so everyone with access to the feed can read the secret. Store the secret in an Orchestrator credential asset and read it with Get Credential at run time.
```

---

[All rules](https://github.com/sorenbcoding/privacy-compliance-rules#rules) · Privacy Compliance Rules by Søren Schytz Birk
