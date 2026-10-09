# Encrypt a drive with BitLocker

Use this to turn on BitLocker, back up the recovery key and confirm the drive is protected. KillerBench backs up and verifies the recovery password before it starts encryption, and it never shows or logs a recovery password.

## Before you start

- Run KillerBench as administrator.
- The Windows drive needs a TPM protector and a recovery password protector. A data drive needs only a recovery password.
- Decide where the recovery key backup goes: a file outside the drive you are encrypting, and optionally Active Directory or Microsoft Entra ID.

## Check the drive

1. Open **BitLocker** and choose the drive in **Volume**.
2. Leave the operation on **Inspect** and press **GO**. The report shows encryption state, protection status and protector identities.
3. If the TPM is present but not ready, prepare it from **Security** first. KillerBench never clears a TPM.

## Add the protectors you need

1. Open **More BitLocker actions**.
2. Choose **Add a TPM protector** or **Add a recovery password**, press **Preview**, tick **Make this change on the selected drive when I press Run action**, then press **Run action**.
3. Back up a new recovery password straight away. KillerBench does not display it.

## Back up the recovery key and encrypt

1. Press **Find recovery protectors** and select the recovery password protector.
2. Enter a full backup file path outside the drive you are encrypting.
3. Choose **Encrypt**, pick the encryption method, and tick **Back up the selected recovery key, then start encryption when I press GO**.
4. Press **GO**. KillerBench writes the backup, verifies it, then starts encryption once.
5. Encryption continues in the background. Run **Inspect** later to confirm the drive reports fully encrypted with protection on.

## Back up to Active Directory or Entra ID

In **More BitLocker actions**, choose the directory backup for the selected protector. KillerBench reports that Windows accepted the request but cannot read the directory copy, so confirm it in Active Directory or the Entra admin center.

## Data drives and auto-unlock

After a data drive is encrypted, **Turn on auto-unlock** makes it unlock when Windows starts. The Windows drive must already be fully encrypted with protection on.

## From the command line

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

See [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) and [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Each has a matching `-recover` command for an interrupted request.

## Related

- [BitLocker section](bitlocker.html)
- [Security section](security.html)
- [How changes stay safe](safety.html)
