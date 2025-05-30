# Development Setup

This folder contains development tools for setting up local configuration and secrets for the Billing project.

## Files

- **`secrets.json.example`** - Template file showing the expected configuration structure
- **`setup_secrets.ps1`** - PowerShell script to configure user secrets from a `secrets.json` file
- **`README.md`** - This file

## Quick Start

1. **Copy the example file:**
   ```bash
   cp secrets.json.example secrets.json
   ```

2. **Edit `secrets.json` with your actual values:**
   ```json
   {
     "globalSettings": {
       "webhookKey": "your-actual-webhook-key",
       "environments": {
         "your-environment-name-1": {
           "baseAddress": "https://your-first-environment-endpoint.com",
           "webhookKey": "your-first-environment-webhook-key"
         },
         "your-environment-name-2": {
           "baseAddress": "https://your-second-environment-endpoint.com",
           "webhookKey": "your-second-environment-webhook-key"
         }
       }
     }
   }
   ```

   **Important**: Replace `your-environment-name-1` and `your-environment-name-2` with your actual environment names (e.g., `US`, `EU`, `DEV`, `STAGING`, etc.).

3. **Run the setup script:**
   ```powershell
   .\setup_secrets.ps1
   ```

## Script Usage

The `setup_secrets.ps1` script:
- Reads your `secrets.json` file
- Initializes .NET user secrets for the project
- Configures all the necessary secrets using `dotnet user-secrets set`
- Provides confirmation and next steps

### Parameters

- **`-SecretsFile`** (optional): Path to your secrets file. Defaults to `./secrets.json`

### Examples

```powershell
# Use default secrets.json file
.\setup_secrets.ps1

# Use a custom secrets file
.\setup_secrets.ps1 -SecretsFile "my-local-secrets.json"
```

## Verifying Setup

After running the script, you can verify your secrets are configured correctly:

```bash
dotnet user-secrets list
```

## Environment Configuration

You can define any number of environments in your configuration. Common examples include:

- **Production environments**: `US`, `EU`, `APAC`
- **Development environments**: `DEV`, `STAGING`, `LOCAL`
- **Custom environments**: `PRODUCTION-WEST`, `PRODUCTION-EAST`, etc.

The environment names are case-insensitive when matching incoming requests, so `US`, `us`, and `Us` will all match the same configuration.

## Security Notes

- **Never commit `secrets.json`** to version control
- The `secrets.json.example` file should only contain placeholder values
- User secrets are stored securely outside your project directory
- Each developer should create their own `secrets.json` file locally

## Configuration Structure

The configuration follows this hierarchy:

```
globalSettings:
├── webhookKey (string) - Main webhook authentication key
└── environments (object)
    ├── your-environment-name-1
    │   ├── baseAddress (string) - Environment endpoint URL
    │   └── webhookKey (string) - Environment-specific webhook key
    └── your-environment-name-2
        ├── baseAddress (string) - Environment endpoint URL
        └── webhookKey (string) - Environment-specific webhook key
```

This structure can be extended to support any number of environments as needed. 