# Security

Security inspects Microsoft Defender, the firewall, certificates and the sign-in services. It also exposes the TPM and platform security state that BitLocker and Windows Hello depend on.

## Tools in this section

Inspect protection, firewall, certificates and sign-in services.

### Microsoft Defender

Inspect Defender protection and signature status. Auto-repair refreshes signatures only when Defender reports active protection and outdated signatures. Uses configured signature sources. No protection, exclusion or policy changes; no malware scan.

### Windows Security Center

Review bounded antivirus registrations with raw provider state, exact service-failure history and current Windows Security Center readiness without interpreting protection health. An empty inventory is not a protection-health verdict, and registration count, raw state or historical failures cannot authorize recovery or a security change. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing security status monitoring and notifications may resume. Antivirus configuration, provider registrations, protection settings, exclusions, notifications, policy and startup type are preserved. No scan, update or protection change is requested. Verification covers stable service state only, not antivirus presence, protection state, signature freshness, notification delivery, registration ownership or effective policy.

### Firewall services

Read Domain, Private and Public profile enabled settings from ActiveStore, then check Base Filtering Engine and Windows Defender Firewall services. Optional recovery starts only enabled, stopped services after fresh dependency checks. Existing filtering policy may take effect and interrupt network or remote access. Startup types, service permissions, firewall rules, profiles and IPsec settings are preserved. No resets, rule changes or service stops. Verification establishes service running state only; effective protection and connectivity remain unverified.

### Certificate expiration

Inspect certificate dates and build cache-only local trust chains for the machine and current-user personal stores, then return one bounded operational conclusion. Revocation, server presentation, bindings, enrollment, policy ownership, remote trust and private-key usability remain unverified. No certificate, trust setting, enrollment, binding, private key, application, service or policy is changed.

### Credential Manager

Check Credential Manager service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume requests for stored credentials. Vault contents, passwords, passkeys, certificates, tokens, credential registrations, permissions, policy, service permissions and startup type are preserved. No credential content or operation is requested. Verification covers stable service state only, not credential retrieval, authentication or application access.

### Web Account Manager

Review payload-sanitized account broker history and exact Web Account Manager availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume account and token requests under current policy. Historical events cannot authorize recovery or establish current account state. Accounts, credentials, tokens, cookies, application registrations, permissions, sign-in state, policy, service permissions and startup type are preserved. KillerBench selects no account, credential, token, cookie, permission or sign-in operation. Verification covers stable service state only, not authentication, token issuance or application access.

### Microsoft Account Sign-in Assistant

Review payload-sanitized account broker history and exact Microsoft Account Sign-in Assistant availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing Microsoft account sign-in requests may resume under current policy. Historical events cannot authorize recovery or establish current account state. Accounts, credentials, tokens, device registration, application data, permissions, sign-in state, policy, service permissions and startup type are preserved. KillerBench selects no account, credential, token, registration, permission or sign-in operation. Verification covers stable service state only, not authentication, account connectivity or application access.
