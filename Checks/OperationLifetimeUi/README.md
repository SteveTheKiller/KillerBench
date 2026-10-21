# Operation lifetime UI regression

This isolated fixture compiles the real window sources and XAML. It replaces only the awaited profile copy and task registration execution calls with harmless release gates. No actual profile copy, Scheduler registration, updater, or native repair runs.

Each operation is exercised through its public routed button event with success, failure, Stop cancellation, and deferred Close. The fixture checks the actual window's About update-hold contract during execution and after cancellation, verifies Close waits until the boundary completes, then verifies cleanup and control availability. Profile inventory setup follows the existing ProfileBackupUi fixture and seeds a field only; no private methods are invoked.

Before running, close KillerBench and use a scratch directory for temporary files and build artifacts. Build this project with `dotnet build Fixture.csproj --artifacts-path <scratch-directory>`, then run the resulting KillerBench.dll with dotnet. The launcher creates and verifies a separate non-input desktop through the existing IsolatedDesktop helper. It refuses to run while KillerBench is open or desktop isolation cannot be verified.

These assertions establish window lifetime and update deferral around the UI's awaited execution boundary. They do not certify native cancellation, child-process draining, recovery journals, or installation behavior.
