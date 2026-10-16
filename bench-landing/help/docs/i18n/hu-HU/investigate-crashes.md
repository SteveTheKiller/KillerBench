# Összeomlások és kék képernyők kivizsgálása

Akkor használja, ha a Windows váratlanul újraindul, leállási hibát jelenít meg, vagy egy alkalmazás folyamatosan összeomlik. A KillerBench egyetlen jelentésbe gyűjti az összeomlási, leállítási, hardverhiba- és megbízhatósági bizonyítékokat. Azt írja le, mi lett rögzítve, és soha nem találgatja a kiváltó okot.

## A számítógép ellenőrzése

1. Nyissa meg a **Diagnosztika** szakaszt, és válassza az **Életjelek** eszközt.
2. Nyomja meg a **Diagnózis futtatása** gombot. A jelentés egyesíti a memóriát, a lemezek állapotát, az eszközproblémákat, a Megbízhatósági figyelő előzményeit, a hardverhiba-bejegyzéseket, az összeomlási jelentéseket, a váratlan leállásokat és a Windows Memóriadiagnosztika eredményeit.
3. Olvassa el a következtetést és az alatta lévő bizonyítékokat. A korábbi összeomlások megmagyarázzák, mi történt; nem bizonyítják, hogy a hiba még fennáll.

## A kör szűkítése

- A **Hardverhiba-előzmények** a WHEA- és a memóriadiagnosztikai bejegyzéseket olvassa be az eszközproblémákkal és a lemezek állapotával együtt.
- A **Megbízhatósági előzmények** csoportosítja az elmúlt 14 nap alkalmazás-összeomlásait, lefagyásait és váratlan leállásait.
- Az **Alkalmazáshibák** az összeomló programokra összpontosít.
- Az **Illesztőprogram-telepítési előzmények** a jelenleg is meglévő eszközökhöz rendelt sikertelen illesztőprogram-telepítéseket mutatja.
- Az **Eszközproblémák** felsorolja az Eszközkezelő hibakódjával rendelkező eszközöket.

## Összeomlási memóriaképek olvasása

A KillerBench felsorolja a memóriakép-konfigurációt és a memóriaképfájlok metaadatait, és a kivétel és a modulnevek érdekében elemezni tudja a minidump szerkezetét. A memóriaképek tartalmát az asztali alkalmazás soha nem nyitja meg. Ha be van töltve egy leállási kódokat tartalmazó tudáscsomag, a leállási kódok a dokumentált jelentésükre oldódnak fel.

Őrizze meg a memóriaképeket, amíg be nem fejezi a kivizsgálást. A **Tisztítás** eltávolíthatja őket, de ezzel a bizonyítékokat is eldobja.

## Parancssorból

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Lásd: [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) és [device-code](cli.html#cmd-device-code).

## Kapcsolódó

- [Diagnosztika szakasz](diagnostics.html#vitals)
- [Jelentések és bizonyítékok](reports.html)
