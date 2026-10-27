# Új számítógép beállítása

Akkor használja, amikor új vagy frissen alaphelyzetbe állított PC-t készít elő. A KillerBench áttekinti a számítógépet, a telepíteni tervezett alkalmazásokat és a gépnek szánt nevet, majd a változtatásokat végző védett eszközökhöz irányítja.

## Kezdjen kiindulási állapottal

1. Futtassa a **Diagnosztika** szakasz **Teljes rendszervizsgálat** eszközét, és őrizze meg a jelentést a számítógép kiindulási állapotának nyilvántartásaként.
2. Telepítse a függő frissítéseket. Ha valamelyik meghiúsul, lásd: [A Windows Update javítása](fix-windows-update.html).

## Alkalmazások és konfiguráció áttekintése

1. Nyissa meg az **Üzembe helyezés** szakaszt, és válassza az **Üzembe helyezés** eszközt.
2. Válassza ki a beállítani tervezett alkalmazásokat és konfigurációs csoportokat, és nyomja meg a **Diagnózis futtatása** gombot. Az áttekintés összeveti őket a már telepítettekkel. A KillerBench nem tölt le és nem futtat telepítőket.
3. Egy gyártói telepítő futtatása előtt ellenőrizze azt a parancssorból. A KillerBench csak akkor bízik meg benne, ha az adott gyártó pontos szervezeti nevére kiállított érvényes aláírással rendelkezik.

## A számítógép elnevezése

1. Válassza a **Számítógépnév** eszközt, adja meg a nevet, és nyomja meg a **Diagnózis futtatása** gombot, hogy összevesse az aktuális azonosítóval és a tartományi szereppel.
2. Munkacsoportos számítógépen az átnevezéshez jelölje be az **Ennek a munkacsoportos PC-nek az átnevezése friss ellenőrzések után** beállítást. A befejezéshez újraindítás szükséges.

## Védelem és rendrakás

- [Titkosítsa a meghajtót BitLockerrel](encrypt-with-bitlocker.html), és készítsen biztonsági mentést a helyreállítási kulcsról.
- [Erősítse a Windows adatvédelmét](make-windows-private.html), és tekintse át a **Karbantartás** szakasz **Windows rendbetétele** eszközét.
- Ha minden a helyén van, hozzon létre visszaállítási pontot az **Egy visszaállítási pont előkészítése** lehetőséggel.

## Parancssorból

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Lásd: [workflow](cli.html#cmd-workflow) és [restore-point](cli.html#cmd-restore-point).

## Kapcsolódó

- [Üzembe helyezés szakasz](onboarding.html)
- [Az első diagnózis](first-run.html)
