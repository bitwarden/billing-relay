#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Sets up user secrets for the Billing project from a secrets.json file.

.DESCRIPTION
    This script reads a secrets.json file (created by copying secrets.json.example 
    and filling in your values) and configures them as user secrets for the Billing project.

.PARAMETER SecretsFile
    Path to the secrets.json file. Defaults to "./secrets.json"

.EXAMPLE
    .\setup_secrets.ps1
    
.EXAMPLE
    .\setup_secrets.ps1 -SecretsFile "my-secrets.json"
#>

param(
    [string]$SecretsFile = "./secrets.json"
)

# Get the script directory and resolve absolute paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

# Resolve the absolute path to the secrets file
if ([System.IO.Path]::IsPathRooted($SecretsFile)) {
    $secretsPath = $SecretsFile
} else {
    $secretsPath = Join-Path $scriptDir $SecretsFile
}

# Check if secrets file exists
if (-not (Test-Path $secretsPath)) {
    Write-Error "Secrets file '$secretsPath' not found. Please create it by copying secrets.json.example and filling in your values."
    exit 1
}

Write-Host "Setting up user secrets for Billing project..." -ForegroundColor Green
Write-Host "Project directory: $projectDir" -ForegroundColor Gray
Write-Host "Secrets file: $secretsPath" -ForegroundColor Gray

# Read and parse the secrets file first
try {
    $secretsContent = Get-Content $secretsPath -Raw | ConvertFrom-Json
} catch {
    Write-Error "Failed to read or parse secrets file: $($_.Exception.Message)"
    exit 1
}

# Change to project directory
Push-Location $projectDir

try {
    # Initialize user secrets if not already done
    Write-Host "Initializing user secrets..." -ForegroundColor Yellow
    dotnet user-secrets init
    
    # Clear existing secrets
    Write-Host "Clearing existing secrets..." -ForegroundColor Yellow
    dotnet user-secrets clear
    
    # Set the main webhook key
    Write-Host "Setting main webhook key..." -ForegroundColor Yellow
    $webhookKey = $secretsContent.globalSettings.webhookKey
    if ([string]::IsNullOrWhiteSpace($webhookKey)) {
        Write-Warning "Webhook key is empty or null"
    } else {
        dotnet user-secrets set "globalSettings:webhookKey" $webhookKey
    }
    
    # Set environment configurations
    foreach ($envName in $secretsContent.globalSettings.environments.PSObject.Properties.Name) {
        $envConfig = $secretsContent.globalSettings.environments.$envName
        
        Write-Host "Setting $envName environment configuration..." -ForegroundColor Yellow
        
        $baseAddress = $envConfig.baseAddress
        $envWebhookKey = $envConfig.webhookKey
        
        if ([string]::IsNullOrWhiteSpace($baseAddress)) {
            Write-Warning "Base address for $envName is empty or null"
        } else {
            dotnet user-secrets set "globalSettings:environments:${envName}:baseAddress" $baseAddress
        }
        
        if ([string]::IsNullOrWhiteSpace($envWebhookKey)) {
            Write-Warning "Webhook key for $envName is empty or null"
        } else {
            dotnet user-secrets set "globalSettings:environments:${envName}:webhookKey" $envWebhookKey
        }
    }
    
    Write-Host "User secrets setup completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To verify your secrets, run:" -ForegroundColor Cyan
    Write-Host "  dotnet user-secrets list" -ForegroundColor White
    
} catch {
    Write-Error "Failed to setup user secrets: $($_.Exception.Message)"
    exit 1
} finally {
    # Return to original directory
    Pop-Location
} 