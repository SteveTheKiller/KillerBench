# Laufwerk mit BitLocker verschlüsseln

Verwenden Sie diese Anleitung, um BitLocker einzuschalten, den Wiederherstellungsschlüssel zu sichern und zu bestätigen, dass das Laufwerk geschützt ist. KillerBench sichert und überprüft das Wiederherstellungskennwort, bevor die Verschlüsselung beginnt, und zeigt oder protokolliert nie ein Wiederherstellungskennwort.

## Bevor Sie beginnen

- Führen Sie KillerBench als Administrator aus.
- Das Windows-Laufwerk benötigt eine TPM-Schutzvorrichtung und eine Schutzvorrichtung mit Wiederherstellungskennwort. Ein Datenlaufwerk benötigt nur ein Wiederherstellungskennwort.
- Legen Sie fest, wohin die Sicherung des Wiederherstellungsschlüssels geht: in eine Datei außerhalb des zu verschlüsselnden Laufwerks und optional in Active Directory oder Microsoft Entra ID.

## Das Laufwerk prüfen

1. Öffnen Sie **BitLocker** und wählen Sie das Laufwerk unter **Volume**.
2. Lassen Sie den Vorgang auf **Prüfen** und drücken Sie **Diagnose ausführen**. Der Bericht zeigt Verschlüsselungszustand, Schutzstatus und die Kennungen der Schutzvorrichtungen.
3. Ist das TPM vorhanden, aber nicht bereit, bereiten Sie es zuerst unter **Sicherheit** vor. KillerBench löscht nie ein TPM.

## Die benötigten Schutzvorrichtungen hinzufügen

1. Öffnen Sie **Weitere BitLocker-Aktionen**.
2. Wählen Sie **TPM-Schutzvorrichtung hinzufügen** oder **Wiederherstellungskennwort hinzufügen**, drücken Sie **Vorschau**, markieren Sie **Diese Änderung am gewählten Laufwerk vornehmen, wenn ich Aktion ausführen drücke** und drücken Sie dann **Aktion ausführen**.
3. Sichern Sie ein neues Wiederherstellungskennwort sofort. KillerBench zeigt es nicht an.

## Den Wiederherstellungsschlüssel sichern und verschlüsseln

1. Drücken Sie **Wiederherstellungsschutz suchen** und wählen Sie die Schutzvorrichtung mit Wiederherstellungskennwort.
2. Geben Sie einen vollständigen Pfad für die Sicherungsdatei außerhalb des zu verschlüsselnden Laufwerks ein.
3. Wählen Sie **Verschlüsseln**, wählen Sie die Verschlüsselungsmethode und markieren Sie **Ausgewählten Wiederherstellungsschlüssel sichern und dann die Verschlüsselung starten, wenn ich „Diagnose ausführen“ wähle**.
4. Drücken Sie **Diagnose ausführen**. KillerBench schreibt die Sicherung, überprüft sie und startet dann die Verschlüsselung genau einmal.
5. Die Verschlüsselung läuft im Hintergrund weiter. Führen Sie **Prüfen** später aus, um zu bestätigen, dass das Laufwerk vollständig verschlüsselt ist und der Schutz eingeschaltet ist.

## In Active Directory oder Entra ID sichern

Wählen Sie unter **Weitere BitLocker-Aktionen** die Verzeichnissicherung für die gewählte Schutzvorrichtung. KillerBench meldet, dass Windows die Anfrage angenommen hat, kann die Kopie im Verzeichnis aber nicht lesen. Bestätigen Sie sie daher in Active Directory oder im Entra Admin Center.

## Datenlaufwerke und automatische Entsperrung

Nachdem ein Datenlaufwerk verschlüsselt ist, sorgt **Automatische Entsperrung aktivieren** dafür, dass es beim Start von Windows entsperrt wird. Das Windows-Laufwerk muss bereits vollständig verschlüsselt sein und den Schutz eingeschaltet haben.

## Über die Befehlszeile

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Siehe [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) und [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Zu jedem gibt es einen passenden `-recover`-Befehl für eine unterbrochene Anfrage.

## Verwandte Themen

- [Bereich BitLocker](bitlocker.html)
- [Bereich Sicherheit](security.html)
- [Wie Änderungen sicher bleiben](safety.html)
