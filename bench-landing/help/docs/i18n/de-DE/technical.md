# Die Technik von KillerBench

Ein Blick unter die Haube: wie KillerBench einen Windows-Computer liest, seine Nachweise ehrlich hält und entscheidet, wann eine Änderung erlaubt ist.

## Technik im Überblick

KillerBench ist eine native Windows-App. Die Desktop-App, die Befehlszeile und die darunterliegende Engine teilen sich dieselben Diagnosenachweise und Reparatur-Workflows.

| Komponente | Detail |
| --- | --- |
| Oberfläche | WPF auf .NET 10 (`net10.0-windows`), Windows x64, eigener Fensterrahmen |
| Engine | `Engine.Core`, `Engine` und `Engine.Windows`, ohne Verweis auf WPF oder Desktop-Steuerelemente |
| Befehlszeile | `killerbench-cli`, ein eigener Host auf derselben Engine |
| Windows-Anbieter | .NET, Windows-APIs, PowerShell und Windows-Befehlszeilenwerkzeuge, jeweils mit festen Grenzen für Zeit, Einträge und Größe |
| Berichte | Rohausgabe, Lesbarer Bericht, HTML, Text und JSON |
| Referenzdaten | Versionierte Wissenspakete für Fehlercodes, Hardwarekennungen, Hersteller und Supportverweise |
| Paketierung | Installationsprogramm: framework-abhängig, benötigt die .NET Desktop Runtime 10 für Windows x64. Portabel: eigenständig, keine Laufzeitinstallation |
| Erscheinungsbild | 13 Familiendesigns und 19 Sprachen |

## Installation, Ordner und Daten

KillerBench gibt es in zwei Formen. Das Installationsprogramm ist der Standarddownload und unterstützt eine Installation pro Benutzer oder für alle Benutzer. Die portable Ausgabe bringt ihre eigene .NET-Laufzeit mit und läuft ohne Installation.

KillerBench läuft mit den Rechten des Kontos, das es gestartet hat. Es erhöht sich nicht selbst. Starten Sie es mit **Als Administrator ausführen**, wenn geschützte Nachweise, etwa BitLocker-Schutzvorrichtungen, oder eine Reparatur Administratorzugriff benötigen. Eine Erhöhung wählt nie eine Reparatur aus und genehmigt keine.

### Was das Installationsprogramm tut

Das Installationsprogramm überprüft die Anwendung und entpackt sie nach `%LOCALAPPDATA%\Programs\KillerBench\` oder nach einer Erhöhungsabfrage für alle Benutzer nach `%ProgramFiles%\KillerBench\`. Es legt eine Verknüpfung im Startmenü, auf Wunsch eine Desktopverknüpfung und einen Eintrag unter „Programme hinzufügen oder entfernen“ mit normaler und stiller Deinstallation an. Die portable Ausgabe überprüft und entpackt ihre Nutzdaten für den aktuellen Lauf unter `%LOCALAPPDATA%\KillerBench\Portable\` und räumt verwaiste Kopien beim nächsten Start auf.

### Wo was liegt

| Ort | Inhalt |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Design, Akzent, Oberflächenskalierung und Sprache |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Ein JSON-Journal pro Workflow-Lauf: Entscheidungen, begrenzte Befehlsausgaben und Reparaturergebnisse |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Ausführungsjournale für Wartungsreparaturen wie DISM und SFC |
| Funktionsordner unter `%LOCALAPPDATA%\KillerBench\` | Ausstehende Schutzeinträge und Ergebnisse einzelner abgesicherter Aktionen, zum Beispiel `BitLockerBackup`, `Time`, `Firewall` und `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | Die unter „Info“ gewählte Reparaturstufe für KillerMCP |
| `HKCU` oder `HKLM\Software\KillerBench` | Der Installationspfad für den jeweiligen Installationsbereich |
| Eine `.kbench`-Datei Ihrer Wahl | Eine gespeicherte Erfassung mit ihren aufbewahrten Nachweisen |

Nichts wird hochgeladen. Löschen Sie ein Journal erst, wenn Sie die Nachweise für ein unsicheres Ergebnis nicht mehr brauchen.

## Zuerst diagnostizieren

KillerBench behandelt Diagnose und Änderung als getrennte Aufgaben. Die Diagnose liest nur. Eine Änderung geschieht nur, wenn Sie eine bestimmte Reparatur oder Aktion auswählen, ihre Optionen und Voraussetzungen prüfen und sie bestätigen.

Die vollständige Systemprüfung dient nur der Diagnose, auch wenn ein gezielter Workflow die automatische Reparatur aktiviert hat. Gespeicherte Erfassungen, importierte Protokolle und historische Ereignisse berechtigen nie zu einer Reparatur.

### Begrenzte Erfassung

Jeder native Befehl und jeder Windows-Anbieter läuft mit festen Grenzen für Zeit, Einträge und Größe. Ein Anbieter hält Exitstatus, Zeitüberschreitung, Kürzung, Standardausgabe und Fehlerausgabe fest. Ein fehlgeschlagener oder gekürzter Anbieter kann nie als saubere, leere Antwort gelesen werden.

### Minimale Aufbewahrung

Sammler behalten, was ein Befund braucht. Viele Ergebnisse behalten Namen, Kategorien, Zählwerte, Zustände und Zeitstempel und lassen Anmeldeinformationen, Befehlszeilen, vollständige Pfade, Kontoidentitäten, Netzwerkendpunkte und nicht zugehörige Ereignisdaten weg.

## Nachvollziehbare Nachweise

Jeder Befund ist mit den Nachweisen verknüpft, aus denen er entstanden ist, und der Bericht trennt immer, was nachgewiesen wurde, von dem, was nur beobachtet wurde.

| Zustand | Bedeutung |
| --- | --- |
| Zu prüfen | Die Nachweise stützen ein Problem, das Aufmerksamkeit verdient. |
| Information | Nachweise wurden gesammelt und dienen als Kontext. |
| Unvollständig oder Unbekannt | Eine Prüfung konnte nicht abgeschlossen werden, oder die Nachweise waren nicht verfügbar. Das gilt nie als fehlerfrei. |
| Historisch | Ein Ereignis aus der Vergangenheit. Es erklärt den Verlauf, belegt aber keinen aktuellen Fehler. |
| Überprüft | Eine Reparatur oder Änderung wurde durch Zurücklesen des Ergebnisses bestätigt. |

### Unbekannt ist ein Ergebnis

Unbekannt bedeutet, dass der Zugriff verweigert wurde, eine Quelle fehlte, ein Anbieter fehlschlug, die Ausgabe fehlerhaft war, eine Grenze überschritten wurde oder die Nachweise einander widersprachen. KillerBench zeigt diese Einschränkung und den nächsten sinnvollen Schritt, statt sie zu verbergen.

### Verlauf bleibt Verlauf

Ereigniseinträge und importierte Protokolle beschreiben, was zu einem früheren Zeitpunkt aufgezeichnet wurde. Sie können auf einen Fehlercode oder den nächsten Diagnosebereich verweisen. Sie belegen nicht, dass der Fehler noch besteht.

## Abgesicherte Reparaturen

Reparaturen sind typisierte Vorgänge mit Regeln für Eignung, Ausführung, Überprüfung und Wiederherstellung. Sie sind keine Befehlszeichenfolgen, die aus einer Protokollzeile herausgesucht werden. Jede unterstützte Reparatur folgt derselben Abfolge:

1. Vollständige aktuelle Nachweise sammeln.
2. Genau ein unterstütztes Rezept zuordnen.
3. Erforderliche Optionen und Folgen prüfen.
4. Administrative und richtlinienbezogene Voraussetzungen bestätigen.
5. Die entscheidenden Nachweise unmittelbar vor der Änderung erneut sammeln.
6. Einen dauerhaften Vorbereitungseintrag schreiben.
7. Genau eine begrenzte native Aktion ausführen.
8. Aktuelle Überprüfungsnachweise sammeln.
9. Ein überprüftes, blockiertes, gestopptes oder unsicheres Ergebnis festhalten.

Ändern sich die entscheidenden Nachweise, bevor die Aktion läuft, stoppt KillerBench, ohne sie auszuführen.

### Ausdrückliche Auswahl

Reparaturen, die von einer Option abhängen, brauchen eine genaue Auswahl von Ihnen, etwa ein Volume, eine Druckerwarteschlange, ein Ziel für den Wiederherstellungsschlüssel, einen Energiewert, eine Paketversion oder eine Reparaturquelle. KillerBench erfindet diese Werte nie.

### Verwaltete Computer

Gruppenrichtlinien und Geräteverwaltung können eine lokale Änderung wirkungslos oder unangemessen machen. Unterstützte Reparaturen prüfen, wer eine Einstellung verwaltet, und stoppen, wenn diese Zuständigkeit verwaltet oder unbekannt ist. Einstellungen, die eine Richtlinie verwaltet, bleiben unverändert.

### Automatische Reparatur

Die automatische Reparatur lässt nur geeignete Rezepte fortfahren. Sie umgeht keine Nachweise, wählt keine fehlenden Optionen, ignoriert keine Richtlinien, wiederholt keine unsichere Aktion, startet Windows nicht neu und macht aus einem Befund, der geprüft werden muss, keine Reparatur.

## Journale und Unterbrechungen

Bevor KillerBench Windows auffordert, etwas zu ändern, schreibt es auf, was es tun wird. Jede Änderung wird in einem Journal unter `%LOCALAPPDATA%\KillerBench` gespeichert.

Nach der Änderung liest KillerBench das Ergebnis zurück und meldet **Überprüft** nur, wenn Windows den erwarteten Zustand zeigt. Meldet Windows Erfolg, ohne dass sich der Zustand geändert hat, sagt das Ergebnis das, und nichts wird wiederholt.

Ein Prozess kann anhalten, nachdem Windows eine Anfrage erhalten hat, aber bevor die Überprüfung abgeschlossen ist. Der ausstehende Eintrag bleibt bestehen, und ein späterer Abgleich liest den aktuellen Zustand, um herauszufinden, was passiert ist. Der Abgleich ist kein automatisches Zurücksetzen und wiederholt nie die ursprüngliche Änderung. Behalten Sie das Journal, wenn ein Ergebnis unsicher ist.

## Konstanten und Grenzen

| Spezifikation | Wert |
| --- | --- |
| Diagnoseabdeckung | Die vollständige Systemprüfung umfasst 49 Diagnosebereiche und prüft 96 abgesicherte Reparaturrezepte |
| Lesen einer Textquelle | 1.000.000 Zeichen pro Quelle, 2.000 angezeigte passende Zeilen |
| Sitzungsquellen | 12 in der Seitenleiste; eine Erfassung fasst bis zu 24 Quellen und 64 MB |
| Ereignisabfragen | Die neuesten 200 Ereignisse pro Kanal aus den letzten 7 Tagen |
| Windows Update-Prüfung | 15 Sekunden pro Quelle, insgesamt zwei Minuten |
| Protokolluntersuchung | 10.000 ausgewertete Einträge, 256 verschiedene Codes, 8 Auszüge zu je 320 Zeichen pro Code |
| Komprimiertes CBS-Archiv | 256 MiB Archiv, 512 MiB Protokolldatei, 30 Sekunden zum Entpacken |
| Schreibgeschütztes CHKDSK | Zehn Minuten; eine Reparatur ist auf 65 Minuten begrenzt |
| DNS- und TCP-Prüfungen | Standardmäßig zehn Sekunden, höchstens eine Minute |
| Dienstbestand | Bis zu 1.024 installierte Dienste |
| Wissenspakete | Bis zu 32 gleichzeitig geöffnet |
| Prüfung großer Dateien | Dateien ab 100 MB |
| Wartungsaktivitätsmonitor | Intervall von 1 bis 60 Sekunden, 1 bis 120 Momentaufnahmen |
| Entwürfe geplanter Aufgaben | Ausführungsgrenze von 1 bis 4.320 Minuten |
| Eigene Zeitquellen | Bis zu acht NTP-Peers |
| Aktivitätsexport | Bis zu 150 aufbewahrte Einträge |
| Sprachen und Designs | 19 Sprachen, 13 Designs mit Akzentvarianten |

## Was KillerBench nicht tut

- Ein TPM löschen, Schutzvorrichtungen entfernen, die es nicht selbst angelegt hat, oder Wiederherstellungskennwörter anzeigen.
- Eine Einstellung ändern, die eine Gruppenrichtlinie oder Geräteverwaltung steuert.
- Eine Reparatur ausführen, nur weil ein historisches Ereignis oder ein importierter Bericht ein Problem erwähnt.
- Fehlende, teilweise oder fehlgeschlagene Nachweise in ein Urteil „fehlerfrei“ verwandeln.

## Berichte

Jeder Durchlauf erzeugt einen Bericht, den Sie lesen, filtern und exportieren können. Ein Bericht trennt:

1. Was untersucht wurde
2. Welches Problem gefunden wurde
3. Welche Nachweise den Befund stützen
4. Welche Aktion vorgeschlagen oder ausgeführt wurde
5. Wie das Ergebnis überprüft wurde
6. Was unbekannt bleibt oder einen Neustart braucht

**Rohausgabe** bewahrt die technischen Nachweise mit ihren ursprünglichen Zeilennummern auf, damit Sie jeden Befund bis zu seiner Quelle zurückverfolgen können. **Lesbarer Bericht** fasst denselben Durchlauf in verständlicher Sprache zusammen, mit dem nächsten sicheren Schritt zuerst. HTML-Berichte verwenden das Designsystem von Killer Tools und bleiben auch ohne die App lesbar. Text eignet sich für die Weitergabe, und JSON bewahrt strukturierte Felder für ein anderes Programm.

## Gespeicherte Protokolle und Erfassungen

KillerBench kann Nachweise untersuchen, ohne etwas auf dem Computer auszuführen. Ziehen Sie ein Textprotokoll auf das Fenster oder fügen Sie seinen Pfad ein und wählen Sie **Protokoll öffnen**. CBS-, CSI- und Setup-Zeilen werden mit ihren Zeitstempeln ausgewertet, und Befunde verweisen auf die genaue ursprüngliche Zeile. EVTX- und Ereignis-XML-Dateien lassen sich auf dieselbe Weise importieren.

**Windows Update prüfen** ist eine schreibgeschützte Erfassung auf Abruf von CBS-, DISM-, Panther- und Rollback-Setup-Protokollen, Ereigniskanälen für Updates und Wartung, Zuständen der Update-Dienste, freiem Speicher, Betriebszeit, Anzeigen ausstehender Neustarts und ausgewählten Update-Richtlinien. **Erfassung speichern** schreibt die aufbewahrten Nachweise in eine `.kbench`-Datei. **Erfassung öffnen** öffnet sie ohne die ursprünglichen Dateien erneut und bewertet sie mit den installierten Regeln neu, sodass dieselben Regeln dieselben Befunde liefern.

## Demomodus

Starten Sie `KillerBench.exe --demo`, um **Diagnose ausführen** an einer erfundenen Arbeitsstation auszuprobieren. Jeder Workflow erzeugt simulierte Nachweise und zeitlich gesteuerten Fortschritt. Kein Windows-Anbieter wird aufgerufen, kein Reparaturbefehl läuft und kein Journal wird geschrieben. Eigene Werkzeugschaltflächen außerhalb von **Diagnose ausführen** bleiben aktiv; verwenden Sie sie daher nicht als Demoaktionen.

`KillerBench.exe --demo-screenshot` öffnet einen reinen Ansichtsbericht einer vollständigen Systemprüfung für einen festen, erfundenen Computer. Klicks im Arbeitsbereich, Tastenkürzel und das Ablegen von Dateien sind blockiert.

## Wissenspakete

Umfangreiche und häufig aktualisierte Referenzdaten liegen in Wissenspaketen statt in der Diagnoselogik: Fehlercodes von Windows und Setup, Hersteller- und Gerätekennungen, PCI-, USB-, ACPI- und UEFI-Referenzen, OUI-Herstellerdaten für Netzwerke, Treiber- und Supportorte sowie ausgewählte Supportlinks.

Jedes Paket nennt Format, Schemaversion, Quelle, Quellrevision, Erstellungszeit und Inhaltshash, und KillerBench prüft diese vor der Verwendung. Ein unbekanntes oder ungültiges Paket bleibt nicht verfügbar, statt auf eine Vermutung auszuweichen. Ein Update wird an einen temporären Ort heruntergeladen, geprüft und ersetzt dann das aktive Paket in einem Schritt, sodass ein fehlgeschlagenes Update das bisherige Paket an seinem Platz lässt.

Wissensdaten können eine Kennung erklären oder vorschlagen, wo als Nächstes nachzusehen ist. Sie können keinen historischen Code in einen aktuellen Fehler verwandeln, keinen Treiber automatisch auswählen und keine Reparatur genehmigen.

## Updates und Signatur

Beim Start fragt KillerBench GitHub mit einer Grenze von vier Sekunden nach der neuesten stabilen Version und bietet ein Update nur an, wenn diese Version neuer ist. Sie können die Prüfung mit **Beim Start nach Updates suchen** unter „Info“ oder in der Updateabfrage ausschalten. Heruntergeladen wird das Paket, das zur laufenden Kopie passt, Installationsprogramm oder portabel, und sein SHA-256 muss mit der Datei `SHA256SUMS.txt` der Version übereinstimmen, bevor etwas installiert wird. Die App schließt sich und öffnet sich zum Abschluss erneut. Es gibt keinen Hintergrunddienst.

Die Infokarte zeigt Version, Herausgeber und Fingerabdruck der Codesignatur sowie den SHA-256 der laufenden ausführbaren Datei, damit Sie Ihre Kopie prüfen können.

## KillerMCP

KillerBench enthält keinen KI-Agenten. [KillerMCP](../mcp.html) kann einen Agenten mit derselben Engine verbinden. Die **KillerMCP-Optionen** unter „Info“ legen fest, was dieser Agent tun darf:

| Einstellung | Wirkung |
| --- | --- |
| Aus (Standard) | Der Agent kann keine Reparatur anfordern. |
| Sichere Reparaturen erlauben | Der Agent kann Reparaturen anfordern, die als sicher eingestuft sind. |
| Auch störende Reparaturen erlauben | Störende Reparaturen werden verfügbar, und jede braucht weiterhin eine Genehmigung. |

Nur eine Person an diesem Computer kann die Stufe anheben. Nach einem unsicheren Reparaturergebnis schreibt KillerMCP eine Sperre, und Reparaturen durch den Agenten bleiben blockiert, bis jemand sie unter „Info“ aufhebt.

## Lokalisierung

Die Oberfläche ist in 19 Sprachen verfügbar: Englisch, Bengalisch, Tschechisch, Deutsch, Spanisch, Französisch, Ungarisch, Italienisch, Japanisch, Kasachisch, Norwegisch, Polnisch, Portugiesisch (Brasilien), Russisch, Türkisch, Ukrainisch, Vietnamesisch sowie vereinfachtes und traditionelles Chinesisch. Ein Sprachwechsel wirkt sofort, auch für die Namen von Bereichen und Workflows, den Begrüßungstext und einen geöffneten lesbaren Bericht. Fehlender Text fällt auf Englisch zurück.

Nachweise, Befunde der Engine, Journale und die Rohausgabe bleiben auf Englisch, genau wie ein Windows-Protokoll, damit ein Bericht auf jedem Computer gleich lautet und nach genauem Text durchsucht werden kann.

## Befehlszeile und Exitcodes

`killerbench-cli` führt dieselbe Engine ohne die Desktop-App aus. Der Fortschritt geht an die Standardfehlerausgabe, JSON an die Standardausgabe. Die meisten Befehle zeigen standardmäßig eine Vorschau, und eine Änderung benötigt `--execute`. Passende `-recover`-Befehle prüfen eine unterbrochene Aktion, ohne sie zu wiederholen. Siehe [KillerBench aus Skripten ausführen](use-the-cli.html) und die [Befehlsreferenz](cli.html).

| Exitcode | Workflow-Befehle | Prüfen und Öffnen |
| --- | --- | --- |
| 0 | Kein offenes Problem in den geprüften Bereichen | Abdeckung abgeschlossen |
| 1 | Start- oder Journalfehler | Fehler |
| 2 | Ungültige Argumente | Ungültige Argumente |
| 3 | Aufmerksamkeit erforderlich | Teilweise Erfassung |
| 4 | Neustart vor der Überprüfung erforderlich | |
| 130 | Gestoppt | Abgebrochen oder Gesamtzeit überschritten |

Kein Exitcode bescheinigt, dass ein Computer fehlerfrei ist.

## Architektur

Die Engine ist in drei Projekte ohne Oberfläche aufgeteilt. `Engine.Core` liefert Grundbausteine und deklarative Rezepte, `Engine` enthält gemeinsame Workflows und Nachweise, und `Engine.Windows` registriert die nativen Anbieter. Die Desktop-App, die CLI und die Tests sind eigene Hosts.

| Schicht | Aufgabe |
| --- | --- |
| Fähigkeitssammler | Begrenzten Windows-Zustand für genau einen Bereich lesen |
| Nachweisparser | Form, Identität, Vollständigkeit und Grenzen prüfen |
| Diagnostische Auswertung | Nachweise in ausdrückliche Workflow-Zustände umwandeln |
| Workflow-Steuerung | Prüfungen, Fortschritt, Rezepte und Journale zusammenführen |
| Reparaturkoordinatoren | Eignung erneut prüfen, vorbereiten, ändern, überprüfen und abgleichen |
| Berichte | Probleme, Aktionen, Ergebnisse und Einschränkungen darstellen |
| Wissenspakete | Versionierte Referenzdaten auflösen, ohne die Logik zu ändern |

Sammler schlagen geschlossen fehl, wenn die Identität mehrdeutig ist, die Ausgabe fehlerhaft ist, eine Grenze überschritten wird oder eine erforderliche Berechtigung nicht geprüft werden kann. Die Engine richtet sich derzeit an Windows und ist nicht plattformübergreifend. Das [Engine-Entwicklerhandbuch](../engine/index.html) behandelt die API, die CLI und das Einbetten der Engine in einen anderen .NET-Host.

## Datenschutz und Netzwerk

Diagnose und Protokollanalyse laufen lokal, ohne KI-Dienst und ohne KillerBench-Konto. Journale und aufbewahrte Nachweise bleiben in den Ordnern `%LOCALAPPDATA%\KillerBench` des lokalen Kontos.

Aktionen, die Sie auswählen, können ihr Ziel kontaktieren, etwa Windows Update, Software-Downloaddienste oder die Sicherung von Wiederherstellungsschlüsseln in Active Directory oder Entra ID. Die Updateprüfung beim Start kontaktiert GitHub, und Wissensupdates sowie das KillerMCP-Installationsprogramm kontaktieren ihre konfigurierten Dienste. Ein KI-Client, der [KillerMCP](../mcp.html) verwendet, kann das angeforderte Referenzergebnis erhalten.

Rohprotokolle, importierte Dateien und exportierte Nachweise können Computernamen, Konten, Pfade oder vertraulichen Quelltext enthalten. Prüfen Sie sie vor dem Weitergeben und fügen Sie nie Wiederherstellungsschlüssel, Anmeldeinformationen oder Kundendaten in einen öffentlichen Bericht ein.

## Entwicklungsstand

KillerBench befindet sich in Entwicklung, und es gibt noch keinen öffentlichen Download. Die Reparaturabdeckung wächst noch, und die native Validierung der Reparaturen läuft. Ein erfolgreicher Build oder eine simulierte Prüfung belegt kein natives Reparaturverhalten. Wegwerfbare virtuelle Windows-Maschinen dienen dazu, jede native Änderung über Windows-Versionen und Fehlerzustände hinweg zu validieren. Das vollständige Sicherheitsmodell beschreibt [Wie Änderungen sicher bleiben](safety.html).

## Glossar

Verständliche Erklärungen der Begriffe auf dieser Seite, in alphabetischer Reihenfolge des englischen Originals.

| Begriff | Bedeutung |
| --- | --- |
| BITS | Background Intelligent Transfer Service, der Windows-Dienst, der Updates und andere Dateien im Hintergrund herunterlädt. |
| Erfassung | Eine gespeicherte `.kbench`-Datei mit aufbewahrten Nachweisen, die sich ohne den ursprünglichen Computer erneut prüfen lässt. |
| CBS | Component-Based Servicing, das Windows-Subsystem, das Updates und Features installiert. Sein Protokoll ist `CBS.log`. |
| DISM | Deployment Image Servicing and Management, das Windows-Werkzeug, das den Komponentenspeicher prüft und repariert. |
| Erhöhung | Ausführung mit Administratorrechten nach einer Windows-Abfrage. KillerBench erhöht sich nie selbst. |
| Nachweis | Was KillerBench tatsächlich aus Windows gelesen hat, zusammen mit seiner Quelle aufbewahrt, damit sich ein Befund zurückverfolgen lässt. |
| Geschlossenes Fehlschlagen | Sind Nachweise mehrdeutig, fehlend oder zu groß, lautet das Ergebnis „Unbekannt“, und keine Änderung ist erlaubt. |
| Journal | Ein lokaler JSON-Eintrag, der vor einer Änderung geschrieben und danach aktualisiert wird, damit sich eine unterbrochene Aktion später prüfen lässt. |
| Wissenspaket | Eine versionierte, geprüfte Datei mit Referenzdaten wie Fehlercodes oder Hardwarekennungen. |
| Panther | Der Ordner, in den Windows Setup seine Upgradeprotokolle schreibt. |
| Ausstehender Schutzeintrag | Der Eintrag, der eine Änderung als möglicherweise an Windows gesendet kennzeichnet. Er blockiert eine Wiederholung, bis der Abgleich das Ergebnis liest. |
| Zuständige Richtlinie | Gruppenrichtlinie oder Geräteverwaltung, die eine Einstellung steuert. KillerBench lässt solche Einstellungen unverändert. |
| Rezept | Eine typisierte Reparatur mit eigenen Regeln für Eignung, Ausführung, Überprüfung und Wiederherstellung. |
| Abgleich | Das Lesen des aktuellen Zustands nach einer Unterbrechung, um herauszufinden, was passiert ist, ohne die Änderung zu wiederholen. |
| SFC | System File Checker, das Windows-Werkzeug, das geschützte Systemdateien prüft und wiederherstellt. |
| TPM | Trusted Platform Module, der Sicherheitschip, mit dem BitLocker den Laufwerksschlüssel schützt. |
| Überprüft | Das Ergebnis wurde aus Windows zurückgelesen und entsprach dem, was die Änderung bewirken sollte. |
| Workflow | Ein Lauf eines Werkzeugs: die Prüfungen, die es sammelt, die Rezepte, die es verwenden darf, und der Bericht, den es erzeugt. |
