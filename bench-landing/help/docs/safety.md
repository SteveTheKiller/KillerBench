# How changes stay safe

KillerBench treats diagnosis and change as different jobs. Diagnosis only reads. The full system scan never runs repairs. Select a specific repair or action, review its options and prerequisites, and confirm the proposed change.

## Before a change

1. **Fresh evidence.** The tool reads the current state again. A saved report or an old event never authorizes a change.
2. **Exact eligibility.** The change is offered only when the evidence matches the exact problem it fixes. Anything incomplete blocks it.
3. **Your confirmation.** Review the selected action in its confirmation dialog. Some actions, such as decrypting a drive, need a second confirmation. The dialog follows the active app theme; its appearance does not change the action's requirements.
4. **Prerequisites.** Administrator rights, restart state and ownership by policy or device management are checked. Settings that a policy owns are left alone.
5. **A pending record.** KillerBench writes down what it is about to do before it asks Windows to do it.
6. **A final recheck.** Everything is checked again immediately before the request, so a change in the meantime stops it.

## After a change

- KillerBench reads the result back and reports **Verified** only when Windows shows the expected state.
- If Windows returns success but the state did not change, the result says so and nothing is repeated.
- Each change is saved in a journal under `%LOCALAPPDATA%\KillerBench`.

## If something is interrupted

Supported journaled actions retain a pending record when a request may have reached Windows and verification has not completed. Use the action's documented reconciliation path, or its matching `-recover` command, to inspect that record. Reconciliation is not an automatic rollback. Keep the journal when an outcome is uncertain, and do not manually repeat the change. Crash, cancellation and later-boot behavior still need action-specific native validation.

## What KillerBench will not do

- Clear a TPM, delete protectors it did not create, or display recovery passwords.
- Change a setting that group policy or device management controls.
- Run a repair just because a historical event or imported report mentions a problem.

Diagnostics and report analysis run locally. An action you select can contact an external destination, including software downloads or recovery-key backup to Active Directory or Entra ID. Review the target before confirming. Saved captures and raw evidence can include machine identity and source paths, so review them before sharing.

## Development status

Repair coverage is still expanding. A successful build or simulated check does not establish native repair behavior.
