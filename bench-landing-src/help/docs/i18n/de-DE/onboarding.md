# Einrichtung

Der Bereich Einrichtung bereitet einen neuen Computer vor. Er prüft die Anwendungen und die Konfiguration, die Sie auswählen, und benennt den Computer. Er lädt nichts herunter und installiert nichts von sich aus.

Schritt-für-Schritt-Anleitungen: [Neuen Computer einrichten](set-up-new-pc.html).

## Ein Installationsprogramm vor dem Ausführen überprüfen

Über die Befehlszeile können Sie das Installationsprogramm eines Herstellers überprüfen, bevor Sie es ausführen:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

KillerBench vertraut einem Installationsprogramm nur, wenn es eine gültige Authenticode-Signatur mit dem genauen Organisationsnamen des Herstellers trägt. Es führt das Installationsprogramm nie aus.

## Werkzeuge in diesem Bereich

Einen neuen Computer vorbereiten: Anwendungen, Name und Konfiguration.

### Einrichtung

Prüft ausdrücklich ausgewählte Anwendungen und Konfigurationsgruppen für die Bereitstellung. Aktuelle Deinstallations-, App-Paket- und Office-Klick-und-Los-Registrierungen werden abgeglichen, ohne Installationsprogramme herunterzuladen oder auszuführen. Konfigurationsauswahlen beschreiben die Folgen und verweisen auf eigene abgesicherte Workflows, sobald diese umgesetzt sind. Es findet keine Aktion für Download, Installation, Konto, Update, Bereinigung, Richtlinie, Registrierung, Dienst, Profil oder Neustart statt.

### Computername

Gleicht den vorgeschlagenen Computernamen mit der aktuellen Identität und der Domänenrolle ab. Keine Umbenennung, keine Erfassung von Anmeldeinformationen und kein Neustart. Verfügbarkeit des Namens, Berechtigungen und Anwendungsabhängigkeiten bleiben ungeprüft.
