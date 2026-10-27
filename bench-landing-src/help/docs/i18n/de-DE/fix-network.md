# Netzwerk- und Internetprobleme beheben

Verwenden Sie diese Anleitung, wenn ein Computer kein Internet hat, einen Server oder Namen nicht erreicht oder die Verbindung immer wieder verliert. KillerBench prüft Adapter, Adresse, Gateway, DNS und Firewall der Reihe nach und erneuert eine Lease oder startet einen Dienst nur, wenn aktuelle Nachweise belegen, dass es nötig ist.

## Die Verbindung prüfen

1. Öffnen Sie **Netzwerk** und wählen Sie **Netzwerk**.
2. Um etwas Bestimmtes zu testen, füllen Sie **Hostname-Suche (optional)** oder **TCP-Ziel (optional)** und **TCP-Port** aus. Lassen Sie die Felder leer, um nur die lokale Konfiguration zu prüfen. Es werden keine Anwendungsdaten gesendet.
3. Legen Sie **Zeitlimit pro Suche oder Verbindung (Sekunden)** fest, wenn das Netzwerk langsam ist.
4. Lassen Sie **Bestätigte Probleme automatisch reparieren** aus und drücken Sie **Diagnose ausführen**.
5. Lesen Sie den Bericht. Er unterscheidet eine fehlende Adresse, ein fehlendes oder nicht erreichbares Gateway, Probleme der DNS-Konfiguration, Proxyeinstellungen, nicht passende Firewallprofile und eine fehlgeschlagene Suche oder Verbindung und nennt für jeden Fall den nächsten Schritt.

Eine Antwort des Gateways belegt keinen Internetzugang, und eine fehlgeschlagene Suche über Ihren konfigurierten Resolver bedeutet nicht, dass die Website nicht erreichbar ist.

## Reparieren, was der Bericht gefunden hat

1. Führen Sie **Netzwerk** erneut mit eingeschaltetem **Bestätigte Probleme automatisch reparieren** aus. Das Werkzeug kann die aktivierten Dienste DNS-Client, DHCP-Client, SMB-Client, Netzwerkstandorterkennung, Netzwerklistendienst und IP-Hilfsdienst starten und verbundene physische DHCP-Adapter ohne nutzbare IPv4-Adresse erneuern.
2. Rechnen Sie bei einer Erneuerung mit einer kurzen Unterbrechung der Verbindung. Statische Adressen und virtuelle Adapter werden nie geändert.
3. Für einen einzelnen fehlerhaft zwischengespeicherten Namen verwenden Sie das Leeren des DNS-Caches für genau diesen Host, statt alles zu löschen.

Routen, Hosts-Einträge, DNS-Server, Firewallregeln, Winsock-Anbieter und Netzwerkprofile bleiben immer erhalten.

## WLAN, kabelgebundene Anmeldung und Proxys

- **Drahtlosnetzwerk** prüft den Adapter und die automatische WLAN-Konfiguration und kann den Dienst starten, wenn er angehalten ist.
- **Kabelgebundene Authentifizierung (802.1X)** tut dasselbe für die automatische Konfiguration (verkabelt) in Netzwerken, die eine Anmeldung am Port verlangen.
- **WinHTTP-Auto-Proxy** zeigt die gespeicherten Proxyeinstellungen für den Computer und das aktuelle Konto. KillerBench ruft während der Diagnose nie eine PAC-URL ab und ändert keine Proxyeinstellungen.

## Über die Befehlszeile

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Siehe [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) und [dns-cache](cli.html#cmd-dns-cache).

## Verwandte Themen

- [Bereich Netzwerk](network.html)
- [Druckprobleme beheben](fix-printing.html)
- [Wie Änderungen sicher bleiben](safety.html)
