# Billing Relay

ASP.NET Core service (.NET 10, `Bitwarden.Server.Sdk`) owned by `@bitwarden/team-billing-dev`. The README says only "A service for relaying billing events." Everything below comes from the code, build files and workflows. If something isn't covered here, ask the Billing team instead of guessing.

## What the code does

There is one business endpoint, `POST /paypal/ipn?key=...` in `src/Billing/Controllers/PayPalController.cs`:

1. Reads the form body into an ordered list of key/value pairs.
2. Looks for a `custom` form field and pulls the region out of it with the regex `region:(?<regionValue>[^,]*)`.
3. If the region is `EU`, it uses `globalSettings:euBillingBaseAddress`. Anything else goes to `globalSettings:usBillingBaseAddress`, and a value other than `US` also logs a warning.
4. Increments the `bitwarden.billing_relay.paypal_requests` counter (meter `Bitwarden.BillingRelay`, tag `region`) in `src/Billing/Observability.cs`.
5. Posts the same form data to `{baseAddress}/paypal/ipn?key={key}`. It returns `200` on success, the downstream status code and body on a non-success response, and `500` with a fixed message on an exception.

`src/Billing/Program.cs` also maps the health checks `/health` and `/alive`, and binds `GlobalSettingsOptions` (`src/Billing/Options/GlobalSettingsOptions.cs`) from the `globalSettings` config section. Both base addresses are empty strings in `src/Billing/appsettings.json`.

## Layout

| Path                                   | Contents                                                                               |
| -------------------------------------- | -------------------------------------------------------------------------------------- |
| `src/Billing/`                         | The service (`Billing.csproj`), `Dockerfile`, appsettings, `launchSettings`            |
| `test/Billing.Test/`                   | xUnit + NSubstitute unit tests for `PayPalController`                                  |
| `test/Billing.Test/Utilities/`         | `MockHttpMessageHandler` (records the outbound request), `ActionResultExtensions`      |
| `test/load/paypalcontroller.js`        | k6 load test, run by `.github/workflows/load-test.yml`                                 |
| `charts/billing-relay/`                | Helm chart; its README says it is for local or ephemeral environments                  |
| `global.json`, `Directory.Build.props` | Pins the .NET SDK and `Bitwarden.Server.Sdk`; sets `TargetFramework` and `BaseVersion` |

## Commands

These are the commands CI runs (`.github/workflows/_test.yml` and `_build.yml`):

```bash
dotnet restore --locked-mode test/Billing.Test/Billing.Test.csproj
dotnet build --verbosity minimal test/Billing.Test/Billing.Test.csproj
dotnet test test/Billing.Test/Billing.Test.csproj

dotnet build --verbosity minimal src/Billing/Billing.csproj
dotnet publish src/Billing/Billing.csproj -c Release -o <dir>
```

Formatting: `npm install` runs the `prepare` script (`husky install`). The `.husky/pre-commit` hook then runs `npx lint-staged`, which runs `prettier --cache --write --ignore-unknown` on staged files. C# style rules live in `.editorconfig`.

For the Helm chart and local KIND cluster, follow `charts/billing-relay/README.md`.

## Conventions shown in the code

- `.editorconfig`: file-scoped namespaces (warning), `_camelCase` private fields, `Async` suffix on async methods, `System.*` usings first, unused usings (`IDE0005`) as warnings, LF line endings.
- Outbound HTTP goes through `IHttpClientFactory`. Tests substitute it and return an `HttpClient` backed by `MockHttpMessageHandler`, then assert on the request URI and body.
- `PayPalControllerTests.PostIpnAsync_PreservesFormDataOrder` shows that the order of forwarded form fields matters, but its test data has at most one field, so it cannot catch a reordering. Ask the Billing team before changing how the form is read or forwarded.
- Region cases for the test theories come from `CustomFieldsGenerator` in `PayPalControllerTests.cs`.
- Log calls use structured templates (`{CloudRegion}`), not string interpolation.

## Build, release and deploy (from workflows)

- `src/Billing/Dockerfile` has no build stage. It copies `obj/build-output/publish` into the `aspnet:10.0-azurelinux3.0-distroless` image and listens on port 5000. `_docker.yml` unzips the `_build.yml` artifact into that folder before building the image.
- `ci.yml` (pull requests) runs test, build and Docker. It calls `_docker.yml` with `push-docker-image: true`, so PR images are pushed to `bitwardenprod.azurecr.io/billing-relay`, tagged with the branch name.
- `cd.yml` (push to `main`, ignoring `charts/**`) computes the version, creates a `v<version>` git tag, tests, builds, pushes the image, then triggers the `deploy-billing-relay-qa` and `deploy-billing-relay-prod` tasks.
- `_version.yml` builds the version from `BaseVersion` in `Directory.Build.props` plus a patch number taken from the latest git tag.
- `load-test.yml` runs every Monday and on demand. It runs k6 against `https://billingrelay.qa.bitwarden.pw` by default, reading `billing-paypal-webhook-key` and `dd-api-key` from Azure Key Vault `gh-billing-relay`.

## Working rules

- Never commit a webhook `key` value. The load test gets its key from Key Vault in CI.
- `.github/CODEOWNERS` adds `@bitwarden/dept-bre` as a reviewer for the shared and CI/CD workflows, and adds BRE plus `@bitwarden/team-appsec` for Docker files. Check it before you change those files.
- Test project package versions are pinned exactly (`[x.y.z]`) and are kept up to date by Renovate (`.github/renovate.json`). Keep that format when you add a package.
