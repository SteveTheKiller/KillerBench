# Updates

Updates covers Windows Update, the services behind it and the component store. You can see what is installed, what is pending and why an update fails, and you can review available updates before choosing any to install.

## Tools in this section

Watch servicing activity, review available updates and repair the Windows Update chain.

### Servicing activity monitor

Observe bounded Windows Update and Setup servicing activity over one or more snapshots. Options control refresh timing and whether update events, update history, the exact Windows Update scheduled task, exact service states, restart indicators, current-account background transfers, Delivery Optimization activity and privacy-bounded Windows Installer activity are included. Process IDs, command lines, users and paths are excluded. Activity does not prove progress, completion, a stall or a fault. No update, setup, transfer, task, service, process, policy, restart or repair action is requested.

### Available updates

Discover selected software and driver updates using the configured source, Windows Update or an already registered Microsoft Update service. Source readiness is checked before discovery. No installation, license acceptance, source registration or policy changes. This is separate from servicing repair.

### Windows Update

Inventory bounded Windows servicing package, optional-feature and capability states, check restart indicators, free space and update services, observe Delivery Optimization without starting it, then scan the component store and protected system files. Pending package, feature or capability states request review but do not authorize a component action. Auto-repair can start enabled Windows Update, BITS, Cryptographic Services, Windows Modules Installer and Update Orchestrator services. Starting Update Orchestrator may resume existing scheduled update activity under current policy. Packages, optional features, capabilities, Delivery Optimization cache, peer, bandwidth, policy, source enrollment, schedules and startup settings are preserved. DISM or SFC requires live evidence of corruption. No update is selected or approved by this workflow.

### Windows Update Service

Check exact Windows Update Service state, its registered service DLL, bounded current update activity and exact service-failure history. DLL observations, update activity and historical failures cannot authorize a service start or update operation. Optional recovery starts only a stopped automatic service after fresh dependency and executable-registration checks. A stopped demand-start service remains informational. Existing update activity may resume under current policy. Update policy, source enrollment, schedules, restart settings, packages, BITS jobs, service permissions and startup type are preserved. KillerBench selects, approves or installs no update. Verification covers stable service state only, not source reachability, applicability, download, installation, completion or restart readiness.

### Delivery Optimization

Check the exact Delivery Optimization service state. A stopped demand-start service remains informational. Only an exact Paused state can offer recovery. Optional recovery repeats the state check, sends one bounded continue request and requires two coherent Running observations. Startup type, cache contents, peers, bandwidth, update source and managed policy are preserved. Transfer activity, cache health, update progress and completion remain unverified.

### BITS service

Check the exact Background Intelligent Transfer Service state, current-account transfer jobs and bounded exact service-failure history. Historical failures and transfer snapshots cannot authorize a service start. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume queued background transfers. Jobs, priorities, owners, destinations, proxy settings, policy and startup type are preserved. Verification covers stable service state only, not transfer progress, application ownership or completion.

### Background transfers (BITS)

Read BITS transfer jobs in the selected account scope and review exact job identities. Queue names do not establish application ownership. No transfers are resumed, completed, canceled, deleted or reassigned.

### Cryptographic Services

Check exact Cryptographic Services state, its registered service DLL and bounded exact service-failure history. Historical failures and DLL observations cannot authorize a service start or component replacement. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing catalog, certificate and update operations may resume. Certificates, keys, catalogs, trust settings, policy and startup type are preserved. Verification covers stable service state only, not certificate, signature, catalog or trust validation.

### Windows Modules Installer

Check exact Windows Modules Installer state, bounded servicing package transitions and exact service-failure history. Historical failures and package states cannot authorize a service start or package operation. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing servicing activity may resume. Packages, component-store content, sources, policy and startup type are preserved. Verification covers stable service state only, not component-store health, package applicability, servicing completion or restart readiness.

### Windows Update Medic Service

Check exact Windows Update Medic Service state, its registered service DLL, bounded current update activity and exact service-failure history. DLL observations, update activity and historical failures cannot authorize a service start or component action. A stopped demand-start service can be normal. When this exact tool is selected, optional recovery can start only the stopped enabled service after fresh dependency checks. Windows remediation may resume under current policy and can repair update components or trigger update activity. Update policy, source enrollment, schedules, packages, BITS jobs, restart settings, service permissions and startup type are preserved. KillerBench selects no update and changes no update setting. Verification covers stable service state only, not remediation, component health, applicability, download, installation or restart readiness.

### Update Orchestrator Service

Check exact Update Orchestrator Service state, the exact built-in Windows Update task, bounded current activity and exact service-failure history. Task results, update activity and historical failures cannot authorize a service start or update operation. Optional recovery starts only a stopped automatic service after fresh dependency and executable-registration checks. A stopped demand-start service remains informational. Existing scheduled update activity may resume under current policy and can download or install already approved updates. Update policy, source enrollment, schedules, restart settings, packages, BITS jobs, service permissions and startup type are preserved. KillerBench selects or approves no update. Verification covers stable service state only, not applicability, download, installation, completion or restart readiness.

### Component-store cleanup

Analyze whether Windows recommends component-store cleanup. Optional maintenance removes superseded components immediately, then analyzes again. No ResetBase or manual WinSxS deletion. No automatic undo; reclaimed disk space is not measured.
