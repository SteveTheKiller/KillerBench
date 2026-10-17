# Systemdateien reparieren

Verwenden Sie diese Anleitung, wenn Windows-Komponenten beschädigt sind: Features lassen sich nicht installieren, Updates melden Komponentenfehler oder Systemwerkzeuge stürzen ab. KillerBench führt DISM und SFC erst aus, nachdem eine Live-Prüfung eine Beschädigung bestätigt hat, und überprüft dann das Ergebnis.

## Bevor Sie beginnen

- Führen Sie KillerBench als Administrator aus.
- Erledigen Sie zuerst einen ausstehenden Neustart.
- Ist der Computer verwaltet, prüfen Sie, ob eine Richtlinie DISM auf eine bestimmte Reparaturquelle verweist. KillerBench meldet diese Richtlinie und ändert sie nicht.

## Auf Beschädigung prüfen

1. Öffnen Sie **Reparatur** und wählen Sie **Systemdateien**.
2. Lassen Sie **Bestätigte Probleme automatisch reparieren** aus und drücken Sie **Diagnose ausführen**.
3. Lesen Sie den Bericht. Er erfasst die Zustände von Wartungspaketen, optionalen Features und Funktionen, prüft Voraussetzungen und durchsucht geschützte Systemdateien. Ein sauberes Ergebnis bedeutet, dass keine Reparatur angeboten wird.

## Bestätigte Beschädigung reparieren

1. Schalten Sie **Bestätigte Probleme automatisch reparieren** ein und drücken Sie **Reparatur ausführen**. KillerBench wählt DISM oder SFC nur, wenn die Live-Prüfung eine Beschädigung bestätigt.
2. Warten Sie die Überprüfung ab. Der Bericht zeigt **Überprüft** nur, wenn die Folgeprüfung einen reparierten Zustand zurückliest.
3. Meldet der Bericht, dass der Komponentenspeicher nicht wiederherstellbar oder die Reparaturquelle nicht verfügbar ist, hören Sie dort auf. Eine Wiederholung der Reparatur ändert das Ergebnis nicht.

Ein unterbrochener DISM- oder SFC-Lauf wird beim nächsten Start abgeglichen, ohne den Wartungsbefehl zu wiederholen.

## Den Komponentenspeicher bereinigen

Wählen Sie unter **Updates** die **Komponentenspeicher-Bereinigung**. Sie fragt Windows zuerst, ob eine Bereinigung empfohlen wird. Die optionale Wartung entfernt ersetzte Komponenten und analysiert erneut. Sie verwendet nie ResetBase und löscht nie WinSxS-Inhalte von Hand.

## Über die Befehlszeile

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Siehe [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) und [servicing-logs](cli.html#cmd-servicing-logs). `repair` zeigt standardmäßig eine Vorschau und handelt nur mit `--execute`.

## Verwandte Themen

- [Windows Update reparieren](fix-windows-update.html)
- [Bereich Reparatur](repair.html#system-files)
- [Wie Änderungen sicher bleiben](safety.html)
