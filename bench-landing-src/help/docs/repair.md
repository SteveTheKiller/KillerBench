# Repair

Repair collects the tools that can fix something. Each one diagnoses first. A repair is offered only when fresh evidence shows the exact problem it fixes, and it runs only after you turn on **Auto-repair confirmed issues** or confirm a single repair yourself. Every repair is journaled and verified afterwards.

Step-by-step guides: [Repair system files](repair-system-files.html) and [Fix Windows Update](fix-windows-update.html).

## Automatic and manual repair

**Auto-repair confirmed issues** lets a diagnostic tool run the repairs it proves are needed after the scan. It is off by default.

For a single servicing repair, open **Advanced: logs and manual repairs** and choose **Repairs**. Pick **Component store (DISM)** or **System files (SFC)**, read the preview, tick **Run this repair on this PC** and press **Run repair**. See [How changes stay safe](safety.html) for what happens before and after any change.

## Tools in this section

Use this after a diagnosis identifies a problem. Repairs require confirmed evidence and explicit approval.

### Core Windows services

Check Power, Plug and Play, User Manager, Group Policy Client, Network Store Interface, RPC Endpoint Mapper, Remote Procedure Call, DCOM Server Process Launcher, Background Tasks Infrastructure and CoreMessaging services together. Optional recovery starts only exact enabled automatic services that fresh evidence confirms are stopped after the startup window and complete dependency checks. Demand-start services remain unchanged when idle. Power plans, devices, drivers, accounts, credentials, profiles, policy, adapters, addresses, routes, DNS, firewall configuration, registrations, endpoints, task registrations, application data, service permissions and startup types are preserved. No policy refresh, device scan, account action, network change, power-setting change, registration change or task launch is requested. Verification covers stable service state only, not end-to-end feature behavior.

### System files

Inventory bounded Windows servicing package, optional-feature and capability states, check prerequisites, scan protected system files, and automatically choose SFC only when the live check confirms corruption. Pending package, feature or capability states request review but do not authorize installation, removal, cleanup, rollback, enablement or disablement.

### WMI repository

Check WMI service availability, then repository consistency. Auto-repair can start an eligible stopped service and salvage confirmed corruption, then verify each result. Disabled service settings are preserved. Readable registrations are merged; unreadable content may require provider reinstallation. Management clients may be interrupted. No reset, deletion or automatic rollback.

### Windows system notifications

Read recent Service Control Manager history, then check COM+ Event System and System Event Notification Service together. Optional recovery verifies COM+ Event System readiness before starting SENS. Existing logon, network, power and COM+ notifications may resume under current configuration. COM+ catalogs, subscriptions, notification registrations, consumers, accounts, permissions, recovery settings, policy and startup types are preserved. No subscription, registration, consumer, account, permission or policy is changed. Verification covers stable service state only, not end-to-end notification delivery.

### Diagnostic Policy Service

Review bounded Diagnostic Policy operational history, exact service-failure history and current service readiness. Incomplete evidence takes precedence. Historical records cannot authorize service recovery or a troubleshooting action. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing diagnostic scenarios may resume. Policy, troubleshooting data and startup type are preserved. No troubleshooter or suggested fix is run. Verification covers stable service state only, not fault detection, diagnosis completion or repair outcome.

### Windows Error Reporting

Review crash-dump configuration, bounded dump-file metadata, recent system crash reports, exact Windows Error Reporting service-failure history and current service readiness. Historical reports do not establish a current fault or root cause and cannot authorize recovery. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing queued reports may resume under current policy. Report archives, dumps, consent, upload policy and startup type are preserved. No upload or deletion is requested. Verification covers stable service state only, not report creation, upload, delivery, problem resolution or crash diagnosis.

### Windows Event Collector

Review bounded Windows Event Collector subscription inventory, exact service-failure history and current service readiness. An empty inventory can be intentional, and subscription count or historical failures cannot authorize recovery. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing subscriptions may resume remote event collection. Subscriptions, forwarded events, credentials, channel settings, policy and startup type are preserved. Verification covers stable service state only, not source reachability, authentication, remote delivery, event freshness or subscription health.

### Windows Event Log

Inspect four diagnostic channels for enabled state, retention mode, current size, configured limit, record count and saturation, then check Windows Event Log service availability. Auto-repair can start a stopped, enabled service after checking dependencies and verify running state. A separately confirmed focused action can enable one exact disabled unmanaged diagnostic channel while preserving every other reported setting. Logs, channel permissions, retention, size limits, startup types and managed settings are preserved. Successful event delivery and archive capacity remain unverified.

### Windows Time service

Check Windows Time configuration, service and synchronization. Auto-repair can start W32Time when the current status reports the exact stopped-service error and fresh policy, mode and dependency checks pass, then request a resync and verify fresh status. This can adjust the clock using existing configured sources. Startup type, source configuration and managed settings are preserved.

### Task Scheduler service

Inventory bounded scheduled-task state, runtime results and missed-run counts, then combine them with exact Task Scheduler service failure history before checking current service availability. Task findings and historical service failures cannot authorize a service start. Optional recovery starts only an enabled, stopped exact service after fresh dependency checks. Existing enabled tasks may begin under their configured accounts, including missed triggers. No task definition, principal, credential reference, policy or startup type is changed, and no task is directly started or deleted. Verification covers stable service state only, not action success, credential validity or future task outcomes.

### User Profile Service

Check User Profile Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing sign-in and profile requests may resume. Profile folders, registry hives, permissions, profile-list registrations, credentials, user data, policy, service permissions and startup type are preserved. No profile operation is requested. Verification covers stable service state only, not sign-in, profile loading or user-data access.

### Backup services

Check Volume Shadow Copy, Microsoft Software Shadow Copy Provider, Windows Backup and Block Level Backup Engine services, then read VSS writer status, registered provider types, current snapshots and shadow-storage allocation. Stopped demand-start services can be normal when idle. Optional recovery starts only stopped automatic services after dependency checks, then verifies running state. Existing scheduled or interrupted backup work may resume. Disabled and demand-start startup settings, snapshots, shadow storage, provider registrations, backup plans, catalogs, schedules, targets, credentials and retention are preserved. No backup is selected or reconfigured, no service is restarted, and no snapshot or catalog is deleted. Provider registration, writer status, storage allocation and stable service state do not verify backup completion, destination access or restore success.
