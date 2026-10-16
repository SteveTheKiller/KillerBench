# Meghajtó ellenőrzése és javítása

Akkor használja, ha egy kötet hibákat jelez, a Windows meghajtóvizsgálatot kér, vagy fájlok nem nyílnak meg. A KillerBench először csak olvasási módban ellenőrzi a kötetet, és fájlrendszer-javítást csak azután ütemez vagy futtat, hogy Ön megerősítette a biztonsági mentést.

## Mielőtt elkezdené

- Készítsen biztonsági mentést a köteten lévő fontos adatokról. A fájlrendszer-javítás módosíthatja vagy eltávolíthatja a sérült fájlokat.
- Futtassa a KillerBenchet rendszergazdaként.

## A kötet ellenőrzése

1. Nyissa meg a **Tárolás** szakaszt, és válassza a **Tárolás** eszközt.
2. Válassza ki a meghajtót az **Ellenőrizendő kötet** mezőben, és nyomja meg a **Diagnózis futtatása** gombot.
3. Olvassa el a jelentést. Megmutatja a lemez és a kötet állapotát, a piszkos bitet, az online NTFS-öngyógyítást, a vezérlő- és lemezeszközhibákat, a megbízhatósági számlálókat és a legutóbbi tárolási eseményeket, például az alaphelyzetbe állításokat, az újrapróbálkozásokat és a váratlan eltávolítást.

Tiszta ellenőrzés esetén nem kínál javítást. A lemez hőmérséklete, kopása és a nem javított I/O-számlálók áttekintendő bizonyítékok, és önmagukban nem indítanak javítást.

## A fájlrendszer javítása

1. Nyomja meg a **Fájlrendszer-javítás áttekintése** gombot, és válassza a **Célzott javítás** vagy az **Offline vizsgálat és javítás** lehetőséget.
2. Adatkötet esetén jelölje be a **Nem rendszerkötet offline állapotba helyezésének engedélyezése** beállítást, ha mindent be tud zárni, ami használja. A Windows-kötet nem helyezhető offline állapotba, ezért annak javítása a következő újraindításra ütemeződik.
3. Jelölje be **A köteten lévő fontos adatok biztonsági mentése megtörtént** beállítást.
4. Erősítse meg a javítást. A KillerBench közvetlenül a művelet előtt újra ellenőrzi a pontos kötetet, és először egy függő bejegyzést rögzít.
5. Ütemezett javítás esetén indítsa újra a gépet, amikor alkalmas. A KillerBench az újraindítás után a javítás ismételt futtatása nélkül ellenőrzi az eredményt.

## Optimalizálás vagy retrimelés

Ugyanez az eszköz elemezni tudja a kötetet, és futtathatja a Windows által ajánlott optimalizálást, a **Szilárdtest-tároló retrimelése** műveletet vagy merevlemez esetén a töredezettségmentesítést. Mindegyik külön megerősített művelet egyetlen pontos, egészséges köteten.

## Parancssorból

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Lásd: [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) és [volume-optimize](cli.html#cmd-volume-optimize).

## Kapcsolódó

- [Tárolás szakasz](storage.html)
- [Lemezterület felszabadítása](free-disk-space.html)
- [Hibaelhárítás](troubleshooting.html)
