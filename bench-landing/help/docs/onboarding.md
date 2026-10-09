# Onboarding

Onboarding prepares a new computer. It reviews the applications and configuration you select and names the computer. It does not download or install anything by itself.

Step-by-step guides: [Set up a new computer](set-up-new-pc.html).

## Checking an installer before you run it

The command line can verify a vendor installer before you run it:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

KillerBench trusts an installer only when it carries a valid Authenticode signature from the vendor's exact organization name. It never runs the installer.

## Tools in this section

Prepare a new computer: applications, name and configuration.

### Onboarding

Review explicitly selected provisioning applications and configuration groups. Current uninstall, app-package and Office Click-to-Run registrations are correlated without downloading or executing installers. Configuration selections describe consequences and route to separately guarded workflows when implemented. No download, installation, account, update, cleanup, policy, registry, service, profile or restart action occurs.

### Computer name

Review the proposed computer name against current identity and domain role. No rename, credential collection or restart. Name availability, permissions and application dependencies remain unverified.
