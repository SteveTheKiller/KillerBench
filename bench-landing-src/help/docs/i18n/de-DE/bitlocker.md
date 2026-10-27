# BitLocker

Der Bereich BitLocker untersucht Laufwerksverschlüsselung, Schutzvorrichtungen und Wiederherstellungsinformationen und hilft Ihnen dann, ein Laufwerk zu sichern, zu schützen oder zu ändern. Er arbeitet nur auf diesem Computer.

Schritt-für-Schritt-Anleitungen: [Laufwerk mit BitLocker verschlüsseln](encrypt-with-bitlocker.html).

## Das BitLocker-Werkzeug verwenden

Wählen Sie den Laufwerksbuchstaben und einen **Vorgang** und drücken Sie dann **Diagnose ausführen**.

- **Prüfen** liest Verschlüsselungszustand, Schutzstatus und die Kennungen der Schutzvorrichtungen. Dies ist die Standardeinstellung und ändert nichts.
- **Wiederherstellung sichern** schreibt das gewählte Wiederherstellungskennwort in eine neue Datei außerhalb des geschützten Laufwerks und überprüft sie. Drücken Sie zuerst **Wiederherstellungsschutz suchen**, wählen Sie die Schutzvorrichtung und bestätigen Sie dann die Sicherung.
- **Verschlüsseln** sichert das gewählte Wiederherstellungskennwort, überprüft die Sicherung und startet dann die Verschlüsselung genau einmal mit der von Ihnen gewählten Methode. Das Windows-Laufwerk benötigt eine TPM-Schutzvorrichtung und ein Wiederherstellungskennwort. Ein Datenlaufwerk benötigt nur ein Wiederherstellungskennwort.
- **Schutz fortsetzen** schaltet den Schutz für ein vollständig verschlüsseltes Laufwerk wieder ein, bei dem er angehalten wurde.

Jede dieser Aktionen benötigt ihr eigenes Bestätigungsfeld, und KillerBench prüft das Laufwerk unmittelbar vor der Aktion erneut.

## Weitere BitLocker-Aktionen

Öffnen Sie **Weitere BitLocker-Aktionen** für Änderungen außerhalb von **Diagnose ausführen**. Wählen Sie eine Aktion, drücken Sie **Vorschau**, um genau zu lesen, was sie erfordert, bestätigen Sie dann und drücken Sie **Aktion ausführen**.

- **Automatische Entsperrung aktivieren** sorgt dafür, dass ein verschlüsseltes Datenlaufwerk beim Start von Windows entsperrt wird. Das Windows-Laufwerk muss vollständig verschlüsselt sein und den Schutz eingeschaltet haben.
- **TPM-Schutzvorrichtung hinzufügen** oder **Wiederherstellungskennwort hinzufügen** legt eine neue Schutzvorrichtung an. KillerBench zeigt oder protokolliert nie ein neues Wiederherstellungskennwort; sichern Sie es daher sofort.
- **Wiederherstellungskennwort in Active Directory sichern** oder **Microsoft Entra ID** sendet die oben gewählte Schutzvorrichtung. KillerBench meldet, dass Windows die Anfrage angenommen hat. Die Kopie im Verzeichnis kann es nicht lesen, bestätigen Sie sie daher dort.
- **Dieses Laufwerk entschlüsseln** startet die Entschlüsselung, etwa vor einer erneuten Verschlüsselung mit einer geforderten Methode. Sie benötigt eine zweite Bestätigung, weil das Laufwerk danach ungeschützt ist, und verweigert das Windows-Laufwerk, solange andere Laufwerke über es automatisch entsperrt werden.

## TPM und FIPS

Ist das TPM vorhanden, aber nicht bereit, kann der Bereich Sicherheit es vorbereiten, ohne es zu löschen. KillerBench löscht nie ein TPM, weil dabei Schlüssel von BitLocker und Windows Hello verloren gehen. Der Schalter für die FIPS-Algorithmusrichtlinie steht als ausdrückliche, umkehrbare Reparatur für Organisationen bereit, die FIPS-konforme BitLocker-PINs benötigen.

## Werkzeuge in diesem Bereich

Verschlüsselung, Schutzvorrichtungen und Wiederherstellungsinformationen prüfen, dann sichern oder das Laufwerk schützen.

### BitLocker

Zeigt die BitLocker-Optionen in der Vorschau und untersucht das gewählte Volume. Dieser Workflow führt keine Verschlüsselung und keine Sicherung der Wiederherstellungsinformationen durch.
