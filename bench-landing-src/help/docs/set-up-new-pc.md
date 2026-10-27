# Set up a new computer

Use this when preparing a new or freshly reset PC. KillerBench reviews the computer, the applications you plan to install and the name you want to give it, then points to the guarded tools that make each change.

## Start with a baseline

1. Run **Full system scan** from **Diagnostics** and keep the report as a record of the computer's starting state.
2. Install pending updates. See [Fix Windows Update](fix-windows-update.html) if any fail.

## Review applications and configuration

1. Open **Onboarding** and choose **Onboarding**.
2. Select the applications and configuration groups you plan to set up and press **Run diagnosis**. The review compares them with what is already installed. KillerBench does not download or run installers.
3. Before running a vendor installer, verify it from the command line. KillerBench trusts it only when it carries a valid signature from that vendor's exact organization name.

## Name the computer

1. Choose **Computer name**, enter the name and press **Run diagnosis** to review it against the current identity and domain role.
2. On a workgroup computer, tick **Rename this workgroup PC after fresh checks** to rename it. A restart is needed to finish.

## Protect and tidy it

- [Encrypt a drive with BitLocker](encrypt-with-bitlocker.html) and back up the recovery key.
- [Make Windows more private](make-windows-private.html) and review **Declutter Windows** in **Maintenance**.
- Create a restore point with **Prepare one restore point** once everything is in place.

## From the command line

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

See [workflow](cli.html#cmd-workflow) and [restore-point](cli.html#cmd-restore-point).

## Related

- [Onboarding section](onboarding.html)
- [Your first diagnosis](first-run.html)
