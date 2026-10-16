# Über KillerBench

KillerBench ist eine Werkbank für Windows-Diagnose, -Reparatur und -Wartung von Steve the Killer und gehört zur [Killer Tools-Familie](https://killertools.net). Version 0.1.0 ist unveröffentlicht. Desktop-App, Engine und CLI teilen sich Diagnosenachweise und die unterstützten Reparatur-Workflows.

## Verfügbarkeit und Umfang

Es gibt noch keinen öffentlichen KillerBench-Download und kein Engine-Paket. [Download und Einrichtung](setup.html) beschreibt das aktuelle Ziel Windows x64 und die Anforderungen an die Desktop Runtime. Die native Validierung der Reparaturen läuft noch; dass ein Werkzeug vorhanden ist, belegt nicht, dass jeder Fehler, jede Unterbrechung oder jede Windows-Konfiguration getestet wurde.

## Quellcode und Vertriebshinweise

KillerBench steht unter der **GPLv3**. Seine Vertriebslizenz ist die [GNU General Public License Version 3](../license.txt). Die Lizenz beschreibt die Rechte und Bedingungen für das Kopieren, Ändern und Verbreiten erfasster Werke.

Das öffentliche [KillerBench-Repository](https://github.com/SteveTheKiller/KillerBench) bietet derzeit Projektinformationen. Die vollständige Implementierung ist dort nicht öffentlich verfügbar. Diese Entwicklerhandbücher beschreiben einen autorisierten Quellcode-Checkout; sie bieten keinen öffentlichen Quellcode-Download.

Bevor eine Binärdatei verbreitet wird, muss ihr Herausgeber den korrespondierenden Quellcode auf einem Weg bereitstellen, den [Abschnitt 6 der GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html#section6) erlaubt, einschließlich des Quellcodes und des Build-Materials, das die Lizenz verlangt. Die Hinweise zur Bereitstellung des Quellcodes und die mitgelieferten Hinweise zu Drittanbietern für das erste öffentliche Paket stehen noch aus. Die GPLv3 erlaubt nicht die Weitergabe von Schriftarten Dritter; die aktuellen Genehmigungen für Schriftarten müssen noch geprüft werden. Prüfen Sie Lizenz, Quellcodehinweise und Hinweise des veröffentlichten Pakets, bevor Sie es weitergeben.

## Datenschutz und externe Dienste

Diagnose und Protokollanalyse laufen lokal, ohne KI-Dienst und ohne KillerBench-Konto. Journale und aufbewahrte Nachweise verwenden die Ordner `%LOCALAPPDATA%\KillerBench` des lokalen Kontos. Ausgewählte Aktionen können Windows Update, Software-Downloaddienste, Verzeichnisse oder andere benannte Ziele kontaktieren. Die Updateprüfung beim Start kontaktiert GitHub, und Wissensupdates sowie die Installationsseite von MCP kontaktieren ihre konfigurierten Dienste. Ein KI-Client, der MCP verwendet, kann das zurückgegebene Referenzergebnis erhalten.

Diese Website speichert Design- und Akzentauswahl im lokalen Speicher des Browsers. Ihre Anleitungen führen keine Diagnose auf Ihrem Computer aus. Rohprotokolle, importierte Dateien und exportierte Nachweise können Computernamen, Konten, Pfade oder vertraulichen Quelltext enthalten. Prüfen Sie sie vor dem Weitergeben; fügen Sie keine Wiederherstellungsschlüssel, Anmeldeinformationen oder Kundendaten in einen öffentlichen Bericht ein.

## Support

Projektinformationen finden Sie im öffentlichen Repository. Für Support verwenden Sie die Kontaktdaten auf [der Website des Autors](https://thekiller.net); geben Sie App-Version, Windows-Build, genaues Werkzeug, erwartetes und beobachtetes Ergebnis an. Prüfen und schwärzen Sie Nachweise vor dem Senden und verwenden Sie für Kundeninformationen einen genehmigten privaten Kanal. Behalten Sie die Journale bei einem unsicheren Reparaturergebnis.

## Verwandte Anleitungen

- [Wie Änderungen sicher bleiben](safety.html)
- [Berichte und Nachweise](reports.html)
- [Engine-Entwicklerhandbuch](../engine/)
- [KillerMCP](../mcp.html)
