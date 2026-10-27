# Kontrola a oprava jednotky

Tento postup použijte, když svazek hlásí chyby, Windows žádá o kontrolu jednotky nebo se soubory nedaří otevřít. KillerBench svazek nejprve zkontroluje pouze pro čtení a opravu souborového systému naplánuje nebo spustí až poté, co potvrdíte zálohu.

## Než začnete

- Zálohujte důležitá data na svazku. Oprava souborového systému může poškozené soubory změnit nebo odebrat.
- Spusťte KillerBench jako správce.

## Kontrola svazku

1. Otevřete **Úložiště** a zvolte **Úložiště**.
2. V poli **Svazek ke kontrole** vyberte jednotku a stiskněte **Spustit diagnostiku**.
3. Přečtěte si zprávu. Zobrazuje stav disku a svazku, příznak nečistoty, online samoopravu NTFS, chyby řadičů a diskových zařízení, čítače spolehlivosti a nedávné události úložiště, například resetování, opakované pokusy a neočekávané odebrání.

Čistá kontrola znamená, že žádná oprava nabídnuta není. Teplota disku, opotřebení a čítače neopravených V/V operací jsou důkazy ke kontrole a samy o sobě opravu nespouštějí.

## Oprava souborového systému

1. Stiskněte **Zkontrolovat opravu souborového systému** a zvolte **Rychlá oprava** nebo **Offline sken a oprava**.
2. U datového svazku zaškrtněte **Povolit odpojení nesystémového svazku**, pokud můžete zavřít vše, co jej používá. Svazek Windows nelze odpojit, proto se jeho oprava naplánuje na příští restart.
3. Zaškrtněte **Důležitá data na tomto svazku jsou zálohována**.
4. Potvrďte opravu. KillerBench přesný svazek znovu zkontroluje těsně před zásahem a nejprve zapíše čekající záznam.
5. U naplánované opravy restartujte, až se vám to bude hodit. KillerBench po restartu ověří výsledek, aniž by opravu spouštěl znovu.

## Optimalizace nebo retrim

Stejný nástroj umí svazek analyzovat a spustit optimalizaci doporučenou systémem Windows, **Retrim paměti SSD** nebo defragmentaci pevného disku. Každá z nich je samostatná potvrzovaná akce na jednom přesném zdravém svazku.

## Z příkazového řádku

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Viz [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) a [volume-optimize](cli.html#cmd-volume-optimize).

## Související

- [Sekce Úložiště](storage.html)
- [Uvolnění místa na disku](free-disk-space.html)
- [Řešení potíží](troubleshooting.html)
