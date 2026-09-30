# How changes stay safe

KillerBench treats diagnosis and change as different jobs. Diagnosis only reads. A change happens only through a specific repair or action that you choose, and it follows the same steps every time.

## Before a change

1. **Fresh evidence.** The tool reads the current state again. A saved report or an old event never authorizes a change.
2. **Exact eligibility.** The change is offered only when the evidence matches the exact problem it fixes. Anything incomplete blocks it.
3. **Your confirmation.** Every change has its own confirmation. Some, such as decrypting a drive, need a second one.
4. **Prerequisites.** Administrator rights, restart state and ownership by policy or device management are checked. Settings that a policy owns are left alone.
5. **A pending record.** KillerBench writes down what it is about to do before it asks Windows to do it.
6. **A final recheck.** Everything is checked again immediately before the request, so a change in the meantime stops it.

## After a change

- KillerBench reads the result back and reports **Verified** only when Windows shows the expected state.
- If Windows returns success but the state did not change, the result says so and nothing is repeated.
- Each change is saved in a journal under `%LOCALAPPDATA%\KillerBench`.

## If something is interrupted

If KillerBench is closed or loses contact while a request may have reached Windows, the pending record stays. The next run of that action checks what actually happened instead of sending the request again. The command line has matching `-recover` commands for the same purpose.

## What KillerBench will not do

- Clear a TPM, delete protectors it did not create, or display recovery passwords.
- Change a setting that group policy or device management controls.
- Install software on its own, send data to a cloud service, or run a change you did not confirm.
