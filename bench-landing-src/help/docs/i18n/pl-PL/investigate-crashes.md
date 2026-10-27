# Zbadaj awarie i niebieskie ekrany

Użyj tego przewodnika, gdy system Windows nieoczekiwanie uruchamia się ponownie, wyświetla błąd zatrzymania albo aplikacja wciąż ulega awarii. KillerBench zbiera w jednym raporcie dowody dotyczące awarii, zamknięć, błędów sprzętu i niezawodności. Opisuje to, co zostało zapisane, i nigdy nie zgaduje przyczyny źródłowej.

## Sprawdź komputer

1. Otwórz **Diagnostyka** i wybierz **Parametry życiowe**.
2. Naciśnij **Uruchom diagnozę**. Raport łączy pamięć, stan dysków, problemy z urządzeniami, historię Monitora niezawodności, zapisy błędów sprzętu, raporty awarii, nieoczekiwane zamknięcia i wyniki Diagnostyki pamięci systemu Windows.
3. Przeczytaj wniosek i dowody pod nim. Historyczne awarie wyjaśniają, co się stało; nie dowodzą, że usterka nadal występuje.

## Zawęź poszukiwania

- **Historia błędów sprzętu** odczytuje zapisy WHEA i diagnostyki pamięci wraz z problemami z urządzeniami i stanem dysków.
- **Historia niezawodności** grupuje awarie aplikacji, zawieszenia i nieoczekiwane zamknięcia z ostatnich 14 dni.
- **Błędy aplikacji** skupia się na programach, które ulegają awarii.
- **Historia instalacji sterowników** pokazuje nieudane instalacje sterowników powiązane z urządzeniami, które są obecnie podłączone.
- **Problemy z urządzeniami** wymienia urządzenia z kodami błędów Menedżera urządzeń.

## Odczytywanie zrzutów awaryjnych

KillerBench wyświetla konfigurację zrzutów i metadane plików zrzutu oraz może przeanalizować strukturę minizrzutów pod kątem wyjątku i nazw modułów. Zawartość zrzutów nigdy nie jest otwierana w aplikacji komputerowej. Po wczytaniu pakietu wiedzy o kodach sprawdzania błędów kody zatrzymania są tłumaczone na ich udokumentowane znaczenie.

Zachowaj zrzuty do czasu zakończenia badania. **Czyszczenie** może je usunąć, ale wtedy odrzuca dowody.

## Z wiersza polecenia

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Zobacz [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) i [device-code](cli.html#cmd-device-code).

## Powiązane

- [Sekcja Diagnostyka](diagnostics.html#vitals)
- [Raporty i dowody](reports.html)
