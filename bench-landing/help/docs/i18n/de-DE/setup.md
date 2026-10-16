# Download und Einrichtung

KillerBench 1.0.0 ist unveröffentlicht. Es gibt noch kein öffentliches KillerBench-Paket und keinen Download. Diese Seite beschreibt die aktuellen Paketanforderungen, damit Sie sich vorbereiten können, ohne eine inoffizielle Kopie herunterzuladen.

## Windows und Laufzeit

- Das aktuelle Veröffentlichungsziel ist **Windows x64**. Windows 11 x64 ist die aktuelle Basis für die native Validierung. Native Pakete für ARM64, x86 und Windows Server werden nicht angeboten.
- KillerBench wird auf zwei Arten verpackt. Das Installationsprogramm ist **framework-abhängig** und benötigt die **.NET Desktop Runtime 10 für Windows x64**. Die reine .NET Runtime, die ASP.NET Core Runtime und das .NET Framework enthalten kein WPF. Die portable Ausgabe bringt ihre eigene Laufzeit mit und benötigt keine separate Installation. Das Befehlszeilenwerkzeug `killerbench-cli.exe` benötigt die .NET 10 Runtime für Windows x64. Ein SDK wird nur für die Entwicklung benötigt, nicht für die normale Nutzung.
- Verwenden Sie eine Windows-Version, die von [.NET 10 unter Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows) unterstützt wird. Unterstützung durch die Laufzeit bedeutet nicht, dass KillerBench-Reparaturen auf jeder unterstützten Edition oder jedem Build validiert sind. Die Abdeckung von Windows 10 steht noch aus, und ihre unterstützten Editionen richten sich nach den aktuellen Lebenszyklusvorgaben von Microsoft.
- Beziehen Sie die Desktop Runtime nur von [Microsofts Downloadseite für .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) und wählen Sie **.NET Desktop Runtime**, **Windows**, **x64**. Beachten Sie die Regeln Ihrer Organisation für die Softwareinstallation.

Wenn `dotnet` verfügbar ist, sollte `dotnet --list-runtimes` für die x64-Installation `Microsoft.WindowsDesktop.App 10.0.x` auflisten. Eine Laufzeit, die nur für eine andere Architektur installiert ist, genügt dem x64-Installationspaket nicht.

## Wenn ein Paket veröffentlicht ist

1. Verwenden Sie den offiziellen Download, der auf dieser Website verlinkt ist. Noch ist kein KillerBench-Downloadlink aktiv.
2. Lesen Sie die Anforderungen des Pakets, die [GPLv3-Lizenz](../license.txt), die Hinweise zum korrespondierenden Quellcode, die enthaltenen Hinweise zu Drittanbietern sowie die Angaben zu Signatur und Prüfsumme. Ein Dateiname allein belegt keine Echtheit.
3. Führen Sie das Installationsprogramm für das aktuelle Konto aus oder nach einer Erhöhungsabfrage für alle Benutzer. Es legt eine Verknüpfung im Startmenü, auf Wunsch eine Desktopverknüpfung und einen Eintrag unter „Programme hinzufügen oder entfernen“ an. Die portable Ausgabe läuft ohne Installation.
4. Starten Sie KillerBench mit Ihrem normalen Konto und folgen Sie [Ihre erste Diagnose](first-run.html). Fehlgeschlagene Diagnosezugriffe bleiben sichtbar; die Erhöhung ist eine eigene, bewusste Entscheidung.

Umgehen Sie keine Windows-Sicherheitswarnungen, um eine nicht überprüfte Kopie auszuführen. Halten Sie eine aktuelle Sicherung bereit, bevor Sie eine Aktion auswählen, die den Computer verändert.

Beim Start sucht KillerBench auf GitHub nach einer neueren stabilen Version und fragt nach, bevor es sie installiert. Der Download muss mit der veröffentlichten SHA-256-Prüfsumme übereinstimmen. Mit **Beim Start nach Updates suchen** unter „Info“ schalten Sie die Prüfung aus.

## Aktuelle Grenzen

Der aktuelle Build umfasst zwölf Bereiche, schreibgeschützte Diagnose, Berichte und einzeln auswählbare Reparaturen. Die native Validierung der Reparaturen läuft noch. Eine funktionierende Oberfläche, ein erfolgreicher Build oder ein Testergebnis mit Prüfdaten belegt kein Reparaturverhalten auf einem echten Computer. Siehe [Wie Änderungen sicher bleiben](safety.html#development-status).

Die Beispielkonsole auf der Startseite zeigt simulierte Ausgaben. Endgültige Screenshots der Anwendung und das erste öffentliche Paket stehen noch aus.

## Quellcode, Lizenz und Support

KillerBench steht unter der GPLv3. Hinweise zur Bereitstellung des Quellcodes und die Genehmigungen für Schriftarten von Drittanbietern stehen für das erste öffentliche Paket noch aus. Lesen Sie [Über KillerBench](about.html) zu Quellcodeverfügbarkeit, Vertriebshinweisen, Datenschutz bei Nachweisen und Support. Das [Engine-Handbuch](../engine/) richtet sich an Entwickler mit einem autorisierten Quellcode-Checkout; es bedeutet nicht, dass ein öffentliches Engine-Paket verfügbar ist.
