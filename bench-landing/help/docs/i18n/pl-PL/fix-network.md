# Rozwiąż problemy z siecią i internetem

Użyj tego przewodnika, gdy komputer nie ma internetu, nie może połączyć się z serwerem lub nazwą albo stale traci połączenie. KillerBench sprawdza po kolei kartę sieciową, adres, bramę, DNS i zaporę, a dzierżawę odnawia lub usługę uruchamia tylko wtedy, gdy świeże dowody pokazują, że jest to potrzebne.

## Sprawdź połączenie

1. Otwórz **Sieć** i wybierz **Sieć**.
2. Aby przetestować coś konkretnego, wypełnij **Wyszukiwanie nazwy hosta (opcjonalne)** albo **Cel TCP (opcjonalny)** i **Port TCP**. Pozostaw je puste, aby sprawdzić tylko konfigurację lokalną. Nie są wysyłane żadne dane aplikacji.
3. Ustaw **Limit czasu na wyszukiwanie lub połączenie (sekundy)**, jeśli sieć jest wolna.
4. Pozostaw wyłączoną opcję **Automatycznie napraw potwierdzone problemy** i naciśnij **Uruchom diagnozę**.
5. Przeczytaj raport. Rozróżnia on brak adresu, brak bramy lub jej niedostępność, problemy z konfiguracją DNS, ustawienia serwera proxy, niezgodność profilu zapory oraz nieudane wyszukiwanie lub połączenie i podaje następny krok dla każdego przypadku.

Odpowiedź bramy nie dowodzi dostępu do internetu, a nieudane wyszukiwanie przez skonfigurowany resolver nie oznacza, że witryna nie działa.

## Napraw to, co wykazał raport

1. Uruchom ponownie **Sieć** z włączoną opcją **Automatycznie napraw potwierdzone problemy**. Może ona uruchomić włączone usługi Klient DNS, Klient DHCP, klient SMB, Rozpoznawanie lokalizacji w sieci, Usługa Lista sieci i Pomocnik IP oraz odnowić podłączone fizyczne karty DHCP bez użytecznego adresu IPv4.
2. Podczas odnawiania połączenie może na chwilę zniknąć. Adresy statyczne i karty wirtualne nigdy nie są zmieniane.
3. W przypadku jednej błędnej nazwy w pamięci podręcznej użyj opróżnienia pamięci podręcznej DNS dla tego konkretnego hosta, zamiast czyścić wszystko.

Trasy, wpisy w pliku Hosts, serwery DNS, reguły zapory, dostawcy Winsock i profile sieciowe są zawsze zachowywane.

## Sieć bezprzewodowa, logowanie przewodowe i serwery proxy

- **Sieć bezprzewodowa** sprawdza kartę i Autokonfigurację sieci WLAN i może uruchomić usługę, jeśli jest zatrzymana.
- **Uwierzytelnianie przewodowe (802.1X)** robi to samo dla Autokonfiguracji sieci przewodowej w sieciach wymagających logowania na porcie.
- **Automatyczny serwer proxy WinHTTP** pokazuje zapisane ustawienia serwera proxy dla komputera i bieżącego konta. KillerBench nigdy nie pobiera adresu URL pliku PAC i nie zmienia ustawień serwera proxy podczas diagnostyki.

## Z wiersza polecenia

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Zobacz [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) i [dns-cache](cli.html#cmd-dns-cache).

## Powiązane

- [Sekcja Sieć](network.html)
- [Rozwiąż problemy z drukowaniem](fix-printing.html)
- [Jak zmiany pozostają bezpieczne](safety.html)
