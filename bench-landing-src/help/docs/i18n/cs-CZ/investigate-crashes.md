# Zkoumání pádů a modrých obrazovek

Tento postup použijte, když se Windows nečekaně restartuje, zobrazí chybu zastavení nebo některá aplikace opakovaně padá. KillerBench shromáždí důkazy o pádech, vypnutích, hardwarových chybách a spolehlivosti do jedné zprávy. Popisuje, co bylo zaznamenáno, a hlavní příčinu nikdy neodhaduje.

## Kontrola počítače

1. Otevřete **Diagnostika** a zvolte **Životní funkce**.
2. Stiskněte **Spustit diagnostiku**. Zpráva spojuje paměť, stav disků, problémy se zařízeními, historii Sledování spolehlivosti, záznamy hardwarových chyb, zprávy o pádech, neočekávaná vypnutí a výsledky Diagnostiky paměti Windows.
3. Přečtěte si závěr a důkazy pod ním. Historické pády vysvětlují, co se stalo; neprokazují, že závada stále trvá.

## Zúžení příčiny

- **Historie chyb hardwaru** čte záznamy WHEA a diagnostiky paměti spolu s problémy se zařízeními a stavem disků.
- **Historie spolehlivosti** seskupuje pády a zamrznutí aplikací a neočekávaná vypnutí za posledních 14 dní.
- **Chyby aplikací** se zaměřuje na padající programy.
- **Historie instalace ovladačů** zobrazuje neúspěšné instalace ovladačů přiřazené k zařízením, která jsou nyní přítomná.
- **Problémy se zařízeními** uvádí zařízení s kódy chyb Správce zařízení.

## Čtení výpisů paměti

KillerBench uvádí konfiguraci výpisů a metadata souborů výpisů a umí analyzovat strukturu minivýpisů, aby zjistil výjimku a názvy modulů. Obsah výpisů se v desktopové aplikaci nikdy neotevírá. Když je načten balíček znalostí kódů chyb zastavení, kódy zastavení se přeloží na svůj zdokumentovaný význam.

Výpisy si ponechte, dokud zkoumání nedokončíte. **Úklid** je umí odebrat, tím však důkazy zahodíte.

## Z příkazového řádku

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Viz [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) a [device-code](cli.html#cmd-device-code).

## Související

- [Sekce Diagnostika](diagnostics.html#vitals)
- [Zprávy a důkazy](reports.html)
