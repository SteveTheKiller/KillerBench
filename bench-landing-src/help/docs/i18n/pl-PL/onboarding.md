# Wdrażanie

Wdrażanie przygotowuje nowy komputer. Przegląda wybrane aplikacje i konfigurację oraz nadaje komputerowi nazwę. Samo niczego nie pobiera ani nie instaluje.

Przewodniki krok po kroku: [Skonfiguruj nowy komputer](set-up-new-pc.html).

## Sprawdzenie instalatora przed uruchomieniem

Wiersz polecenia może zweryfikować instalator producenta przed jego uruchomieniem:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

KillerBench ufa instalatorowi tylko wtedy, gdy ma on ważny podpis Authenticode z dokładną nazwą organizacji producenta. Nigdy nie uruchamia instalatora.

## Narzędzia w tej sekcji

Przygotuj nowy komputer: aplikacje, nazwę i konfigurację.

### Wdrażanie

Przegląda jawnie wybrane aplikacje do wdrożenia i grupy konfiguracji. Bieżące rejestracje odinstalowywania, pakietów aplikacji i Office Click-to-Run są zestawiane bez pobierania i uruchamiania instalatorów. Wybory konfiguracji opisują skutki i kierują do osobnych chronionych procesów, jeśli są dostępne. Nie jest wykonywana żadna akcja pobierania, instalacji, konta, aktualizacji, czyszczenia, zasad, rejestru, usług, profilu ani ponownego uruchomienia.

### Nazwa komputera

Porównuje proponowaną nazwę komputera z bieżącą tożsamością i rolą w domenie. Bez zmiany nazwy, bez zbierania poświadczeń i bez ponownego uruchamiania. Dostępność nazwy, uprawnienia i zależności aplikacji pozostają niezweryfikowane.
