# Neuen Computer einrichten

Verwenden Sie diese Anleitung, wenn Sie einen neuen oder frisch zurückgesetzten PC vorbereiten. KillerBench prüft den Computer, die Anwendungen, die Sie installieren möchten, und den Namen, den Sie ihm geben wollen, und verweist dann auf die abgesicherten Werkzeuge, die jede Änderung vornehmen.

## Mit einer Ausgangsbasis beginnen

1. Führen Sie unter **Diagnose** die **Vollständige Systemprüfung** aus und behalten Sie den Bericht als Aufzeichnung des Ausgangszustands des Computers.
2. Installieren Sie ausstehende Updates. Siehe [Windows Update reparieren](fix-windows-update.html), falls welche fehlschlagen.

## Anwendungen und Konfiguration prüfen

1. Öffnen Sie **Einrichtung** und wählen Sie **Einrichtung**.
2. Wählen Sie die Anwendungen und Konfigurationsgruppen aus, die Sie einrichten möchten, und drücken Sie **Diagnose ausführen**. Die Prüfung vergleicht sie mit dem, was bereits installiert ist. KillerBench lädt keine Installationsprogramme herunter und führt keine aus.
3. Bevor Sie ein Installationsprogramm eines Herstellers ausführen, überprüfen Sie es über die Befehlszeile. KillerBench vertraut ihm nur, wenn es eine gültige Signatur mit dem genauen Organisationsnamen dieses Herstellers trägt.

## Den Computer benennen

1. Wählen Sie **Computername**, geben Sie den Namen ein und drücken Sie **Diagnose ausführen**, um ihn mit der aktuellen Identität und Domänenrolle abzugleichen.
2. Auf einem Arbeitsgruppencomputer markieren Sie **Diesen Arbeitsgruppen-PC nach frischen Prüfungen umbenennen**, um ihn umzubenennen. Zum Abschluss ist ein Neustart nötig.

## Schützen und aufräumen

- [Laufwerk mit BitLocker verschlüsseln](encrypt-with-bitlocker.html) und den Wiederherstellungsschlüssel sichern.
- [Windows datenschutzfreundlicher machen](make-windows-private.html) und unter **Wartung** **Windows entrümpeln** prüfen.
- Mit **Einen Wiederherstellungspunkt vorbereiten** einen Wiederherstellungspunkt anlegen, sobald alles eingerichtet ist.

## Über die Befehlszeile

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Siehe [workflow](cli.html#cmd-workflow) und [restore-point](cli.html#cmd-restore-point).

## Verwandte Themen

- [Bereich Einrichtung](onboarding.html)
- [Ihre erste Diagnose](first-run.html)
