# Secure Discord webhook configuration

NosGM must not store Discord webhook URLs in tracked source files. Webhook URLs are credentials and must be treated like passwords.

## Required action after an exposed webhook

1. Delete or regenerate the exposed webhook in the Discord channel integration settings.
2. Create a replacement webhook.
3. Store the replacement only in an environment variable on the machine running the World server.
4. Restart the server process after changing the variable.

Removing a webhook from the latest source tree does not remove it from Git history, forks, clones, logs or notifications. Rotation is therefore mandatory after accidental publication.

## Environment variables

- `NOSGM_DISCORD_WEBHOOK_URL`: general Discord embed destination.
- `NOSGM_TITANSHIELD_WEBHOOK_URL`: optional TitanShield-specific destination.

TitanShield falls back to `NOSGM_DISCORD_WEBHOOK_URL` when its dedicated variable is missing.

## Windows PowerShell

Set values for the current user:

```powershell
[Environment]::SetEnvironmentVariable(
    "NOSGM_DISCORD_WEBHOOK_URL",
    "PASTE_THE_NEW_WEBHOOK_HERE",
    "User")

[Environment]::SetEnvironmentVariable(
    "NOSGM_TITANSHIELD_WEBHOOK_URL",
    "PASTE_THE_NEW_TITANSHIELD_WEBHOOK_HERE",
    "User")
```

Close and reopen the terminal, Visual Studio or service manager that starts NosGM so the new process receives the variables.

For a temporary test in the current PowerShell window:

```powershell
$env:NOSGM_DISCORD_WEBHOOK_URL = "PASTE_THE_NEW_WEBHOOK_HERE"
$env:NOSGM_TITANSHIELD_WEBHOOK_URL = "PASTE_THE_NEW_TITANSHIELD_WEBHOOK_HERE"
```

## Service accounts

When NosGM runs as a Windows service, configure the variables for the service account or inject them through the service manager. A variable created only for an interactive user is not automatically visible to another service identity.

## Safe failure behavior

When the variable is missing, malformed, uses plain HTTP or points outside Discord's webhook hosts, NosGM does not send the message. The secret is never written to server logs.
