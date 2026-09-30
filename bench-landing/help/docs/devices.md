# Devices

Devices covers printers, audio, cameras, Bluetooth, input and the Windows services that attached hardware relies on.

## Tools in this section

Printing, audio, cameras, Bluetooth, input and the services behind attached hardware.

### Printing

Check the Print Spooler, local queue pauses, visible print jobs, offline settings and driver-reported conditions, then inventory queue, driver, port, Standard TCP/IP endpoint, print processor and print monitor registrations. Correlate recent PrintService problems only when one exact current queue identity is present. Assigned Standard TCP/IP endpoints receive one bounded connection attempt without application data, SNMP or a print job. Auto-repair can start the enabled spooler, resume only exact paused queues with fresh waiting-job evidence, and clear offline mode. Idle intentional pauses remain review-only. Remote connections, SNMP community strings, extension registrations and job contents are preserved. A condition code or successful connection does not establish printer identity, supplies, mechanics, queue acceptance, rendering success or physical printing.

### Audio

Check the default multimedia input and output identities and shared formats, audio device and driver state, Audio Endpoint Builder and Windows Audio. Auto-repair starts stopped, enabled services and verifies running state. Startup settings, drivers, selected devices, exclusive mode and volume levels are preserved. Sound playback, recording and application routing remain untested.

### Camera support

Inspect camera and imaging device errors, hardware IDs, installed driver evidence and Windows Camera Frame Server availability. Optional recovery starts only an enabled, stopped service after dependency checks. An application already requesting camera access may resume under existing permissions. Camera privacy settings, permissions, device configuration, policy, drivers and startup type are preserved. No reset, permission change or driver installation. Verification covers reported device configuration and service running state only, not image capture, privacy access or physical operation.

### Bluetooth support

Inspect Bluetooth device errors, hardware IDs, installed driver evidence and Bluetooth Support Service availability. Optional recovery starts only an enabled, stopped service after dependency checks; paired devices may reconnect. Pairings, radio settings, device configuration, policy and startup type are preserved. No reset, device removal or driver installation. Verification covers reported device configuration and service running state only, not discovery, pairing, audio or data transfer.

### Smart card support

Inspect smart-card reader errors, hardware IDs, installed driver evidence and Smart Card service availability. Optional recovery starts only an enabled, stopped service after dependency checks. Applications may resume detecting inserted cards under existing policy. Certificates, PINs, credentials, reader and card configuration, policy, drivers and startup type are preserved. No initialization, enrollment, PIN operation, reader reset or driver installation. Verification covers reported reader configuration and service running state only, not card detection, authentication or application access.

### Biometric support

Inspect biometric device errors, hardware IDs, installed driver evidence and Windows Biometric Service availability. Optional recovery starts only an enabled, stopped service after dependency checks. Existing Windows Hello and application requests may resume under current policy. Biometric templates, Windows Hello data, credentials, device configuration, permissions, policy, drivers and startup type are preserved. No enrollment, template deletion, sensor reset, permission change or driver installation. Verification covers reported sensor configuration and service running state only, not capture, matching, sign-in or application access.

### Human Interface Device Service

Check Human Interface Device Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing hot buttons, media controls and other HID features may resume. Devices, drivers, button mappings, accessibility settings, input settings, policy, service permissions and startup type are preserved. KillerBench selects no device, driver, mapping or input setting operation. Verification covers stable service state only, not button, control or application behavior.

### Portable Device Enumerator Service

Check Portable Device Enumerator Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume detecting and accessing connected portable devices. Devices, drivers, files, libraries, synchronization settings, permissions, policy, service permissions and startup type are preserved. KillerBench selects no device, file, library, synchronization or transfer operation. Verification covers stable service state only, not enumeration, access, synchronization or transfer.

### Windows Image Acquisition

Check Windows Image Acquisition service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing scanner and still-image acquisition requests may resume under current application permissions. Devices, drivers, image data, application permissions, privacy settings, transfer settings, policy, service permissions and startup type are preserved. KillerBench selects no device, driver, image, permission, transfer or acquisition operation. Verification covers stable service state only, not scanner, camera, transfer or application operation.

### Shell Hardware Detection

Inspect bounded storage-controller, disk and portable-device Plug and Play evidence and installed driver references, then check Shell Hardware Detection service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing AutoPlay and hardware-arrival notifications may resume under current policy. Device observations and retained history cannot authorize recovery. Devices, drivers, volumes, mount points, AutoPlay settings, handlers, policy, service permissions and startup type are preserved. No device or setting operation is requested. Verification covers stable service state only, not notification delivery, media readiness, volume access or physical device operation.

### Device Install Service

Check Device Install Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing Windows-requested device installation work may resume under current policy. Devices, drivers, packages, configuration, bindings, class settings, policy, service permissions and startup type are preserved. KillerBench selects and installs nothing. Verification covers stable service state only, not device installation or operation.

### Device Association Service

Check recent device setup and Plug and Play history, then check Device Association Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing Windows-requested device association work may resume. Paired and associated devices, drivers, credentials, metadata, permissions, policy, service permissions and startup type are preserved. KillerBench selects no device, pairing, removal, reset or driver operation. Verification covers stable service state only, not discovery, pairing, association or device operation.

### Display Enhancement Service

Inspect graphics adapter and monitor evidence, then check Display Enhancement Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing brightness, color and enhancement requests may resume. Display configuration, brightness, color profiles, HDR settings, devices, drivers, power settings, policy, service permissions and startup type are preserved. No display setting is changed. Verification covers stable service state only, not enhancement behavior, picture quality or physical display operation.

### Text Input Management Service

Inspect keyboard, mouse and HID device evidence, then check Text Input Management Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing touch keyboard, handwriting, emoji and advanced text-input requests may resume. Dictionaries, handwriting data, language and keyboard settings, accessibility and privacy settings, devices, drivers, policy, service permissions and startup type are preserved. No text input, keystroke or handwriting content is collected. Verification covers stable service state only, not text entry, recognition or device operation.

### Windows Font Cache Service

Check Windows Font Cache Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume using the current font cache. Installed fonts, font files, application data, cache files, policy, service permissions and startup type are preserved. KillerBench installs or removes no font and does not delete or rebuild the cache. Verification covers stable service state only, not text rendering or application behavior.

### Function Discovery Provider Host

Check Function Discovery Provider Host availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume discovering network devices through installed providers. Network profiles, shares, devices, providers, firewall rules, discovery settings, credentials, policy, service permissions and startup type are preserved. KillerBench selects no device, share, provider, firewall rule or discovery setting operation. Verification covers stable service state only, not provider health, device discovery or connectivity.

### Function Discovery Resource Publication

Check Function Discovery Resource Publication availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. This PC may become discoverable through existing network discovery and firewall settings. Network profiles, shares, published resources, firewall rules, discovery settings, credentials, policy, service permissions and startup type are preserved. KillerBench selects no share, resource, firewall rule or discovery setting operation. Verification covers stable service state only, not publication, visibility, access or connectivity.
