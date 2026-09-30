# BitLocker

BitLocker inspects drive encryption, protectors and recovery information, then helps you back up, protect or change a drive. It works on this computer only.

## Using the BitLocker tool

Choose the drive letter and an **Operation**, then press **GO**.

- **Inspect** reads encryption state, protection status and protector identities. It is the default and changes nothing.
- **BackupRecovery** writes the selected recovery password to a new file outside the protected drive and verifies it. Press **Find recovery protectors** first, choose the protector, then confirm the backup.
- **Encrypt** backs up the selected recovery password, verifies the backup, then starts encryption once with the method you choose. The Windows drive needs a TPM protector and a recovery password. A data drive needs only a recovery password.
- **ResumeProtection** turns protection back on for a fully encrypted drive where it was suspended.

Each of these needs its own confirmation box, and KillerBench rechecks the drive immediately before it acts.

## More BitLocker actions

Open **More BitLocker actions** for changes that sit outside GO. Choose an action, press **Preview** to read exactly what it requires, then confirm and press **Run action**.

- **Turn on auto-unlock** makes an encrypted data drive unlock when Windows starts. The Windows drive must be fully encrypted with protection on.
- **Add a TPM protector** or **Add a recovery password** creates one new protector. KillerBench never shows or logs a new recovery password, so back it up straight away.
- **Back up the recovery password to Active Directory** or **Microsoft Entra ID** sends the protector you selected above. KillerBench reports that Windows accepted the request. It cannot read the directory copy, so confirm it there.
- **Decrypt this drive** starts decryption, for example before encrypting again with a required method. It needs a second confirmation because the drive is unprotected afterwards, and it refuses the Windows drive while other drives auto-unlock from it.

## TPM and FIPS

If the TPM is present but not ready, the Security section can prepare it without clearing it. KillerBench never clears a TPM, because that destroys keys held by BitLocker and Windows Hello. The FIPS algorithm policy switch is available as an explicit, reversible repair for organizations that need FIPS-approved BitLocker PINs.

## Tools in this section

Inspect encryption, protectors and recovery information, then back them up or protect the drive.

### BitLocker

Preview BitLocker options and inspect the selected volume. No encryption or recovery backup is performed by this workflow.
