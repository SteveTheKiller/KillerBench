# Druckprobleme beheben

Verwenden Sie diese Anleitung, wenn Aufträge in der Warteschlange hängen, ein Drucker offline angezeigt wird oder der falsche Drucker als Standard eingestellt ist. KillerBench liest zuerst Spooler, Warteschlangen, Aufträge, Treiber und Anschlüsse und lässt Sie dann genau eine Warteschlange oder einen Auftrag reparieren.

## Die Drucker prüfen

1. Öffnen Sie **Geräte** und wählen Sie **Drucken**.
2. Drücken Sie **Lokale Drucker finden** und wählen Sie die Warteschlange, die Probleme macht. Um alle Warteschlangen zu prüfen, wählen Sie **Allgemeine Druckprüfungen verwenden**.
3. Lassen Sie **Bestätigte Probleme automatisch reparieren** aus und drücken Sie **Diagnose ausführen**.
4. Lesen Sie den Bericht. Er zeigt den Zustand der Druckwarteschlange (Spooler), angehaltene Warteschlangen, den Offlinemodus, sichtbare Aufträge, Treiberzustände, Anschlussregistrierungen, aktuelle PrintService-Fehler und für Netzwerkdrucker einen Verbindungsversuch zur zugewiesenen Adresse des Druckers.

Eine erfolgreiche Verbindung oder ein sauberer Zustandscode belegt nicht, dass der Drucker Papier, Toner oder eine funktionierende Mechanik hat.

## Die Warteschlange reparieren

1. Führen Sie **Drucken** erneut mit eingeschaltetem **Bestätigte Probleme automatisch reparieren** aus. Das Werkzeug kann den aktivierten Spooler starten, genau eine angehaltene Warteschlange mit wartenden Aufträgen fortsetzen und den Offlinemodus aufheben.
2. Das Aufheben des Offlinemodus kann wartende Aufträge freigeben. Stellen Sie sicher, dass diese Aufträge noch gedruckt werden sollen.
3. Eine Warteschlange, die jemand absichtlich angehalten hat und in der nichts wartet, bleibt angehalten.

## Einen hängenden Auftrag löschen

1. Drücken Sie **Blockierte Druckaufträge finden**.
2. Wählen Sie den Auftrag mit dem Zustand „Fehler“ oder „Blockiert“ und markieren Sie **Nur diesen ausgewählten Auftrag abbrechen**.
3. Drücken Sie **Diagnose ausführen**. Nur dieser Auftrag wird abgebrochen, und das Ergebnis wird zurückgelesen.

## Den Standarddrucker reparieren

Wählen Sie die Warteschlange, die Standard sein soll, markieren Sie **Fehlenden oder widersprüchlichen Standard mit dieser Warteschlange reparieren** und drücken Sie dann **Diagnose ausführen**. Das greift nur, wenn der Standard fehlt oder widersprüchlich ist.

## Über die Befehlszeile

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Siehe [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) und [default-printer-set](cli.html#cmd-default-printer-set).

## Verwandte Themen

- [Bereich Geräte](devices.html#printing)
- [Netzwerk- und Internetprobleme beheben](fix-network.html)
