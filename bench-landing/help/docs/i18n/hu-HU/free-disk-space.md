# Lemezterület felszabadítása

Akkor használja, ha egy meghajtón fogy a hely. A KillerBench megmutatja, mi foglalja a helyet, előnézetben mutatja, mit távolítana el az egyes tisztítási célok közül, és csak a végleges tisztítás bekapcsolása után töröl.

## Mennyi hely maradt

1. Nyissa meg a **Tárolás** szakaszt, és válassza a **Tárolás** eszközt.
2. Nyomja meg a **Diagnózis futtatása** gombot. A jelentés megjelöl minden meghajtóbetűjeles kötetet, amelyen 10%-nál kevesebb a szabad hely, és megmutatja az árnyékmásolatok helyfoglalását, a kvóta-alapértékeket és a Tárolási érzékelő házirendjét.
3. Az összeomlási memóriaképek és a régi frissítési tartalom miatt futtassa a **Diagnosztika** szakasz **Életjelek** eszközét és a **Frissítések** szakasz **Windows Update** eszközét is; ott jelenik meg a frissítési gyorsítótár mérete.

## A tisztítás előnézete

1. Nyissa meg a **Karbantartás** szakaszt, és válassza a **Tisztítás** eszközt.
2. Jelölje be a kívánt célokat, például: **Jelenlegi felhasználó ideiglenes fájljai**, **Windows ideiglenes fájlok**, **Jelenlegi felhasználó böngésző-gyorsítótárai**, **Delivery Optimization gyorsítótár** vagy **Jelenlegi felhasználó Lomtára**. Ha az egérmutatót egy cél fölé viszi, elolvashatja, pontosan mely mappákra vonatkozik.
3. Állítsa be a **Minimális kor (nap)** és a **Célonkénti maximális fájlszám** értékét.
4. Nyomja meg a **Diagnózis futtatása** gombot kikapcsolt **A kiválasztott végleges tisztítás végrehajtása friss ellenőrzések után** beállítással. A jelentés célonként felsorolja a jelölteket és a becsült helyet. Semmi sem törlődik.

## A fájlok eltávolítása

1. Kapcsolja be **A kiválasztott végleges tisztítás végrehajtása friss ellenőrzések után** beállítást, és nyomja meg a **Diagnózis futtatása** gombot.
2. A KillerBench minden jelöltet újra ellenőriz az eltávolítás előtt, kihagyja a friss, védett vagy használatban lévő fájlokat, és minden eredményt rögzít a naplóban.

A végleges tisztítás megkerüli a Lomtárat, és nem vonható vissza. A hibajelentések és az összeomlási memóriaképek eltávolításával olyan bizonyítékokat dob el, amelyekre az összeomlások kivizsgálásához szüksége lehet, ezért ezeket előbb tekintse át.

## A korábbi Windows-telepítés eltávolítása

Egy szolgáltatásfrissítés után a **Tisztítás** **Korábbi Windows-telepítés** eleme a Windows támogatott visszaállításeltávolítási kérésével eltávolíthatja a visszaállítási másolatot. Önállóan fut, a **Biztonsági mentést készítettem minden szükséges fájlról és beállításról a korábbi telepítésből** jelölést igényli, és véglegesen megszünteti a korábbi verzióra való visszatérés lehetőségét.

## Parancssorból

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Lásd: [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) és [previous-windows](cli.html#cmd-previous-windows).

## Kapcsolódó

- [Karbantartás szakasz](maintenance.html#cleanup)
- [Tárolás szakasz](storage.html)
- [Meghajtó ellenőrzése és javítása](check-a-drive.html)
