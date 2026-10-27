# Preverite in popravite pogon

Uporabite, ko nosilec sporoča napake, Windows zahteva pregled pogona ali datotek ni mogoče odpreti. KillerBench najprej pregleda nosilec samo za branje in načrtuje ali izvede popravilo datotečnega sistema šele po potrditvi varnostne kopije.

## Pred začetkom

- Varnostno kopirajte pomembne podatke na nosilcu. Popravilo datotečnega sistema lahko spremeni ali odstrani poškodovane datoteke.
- Zaženite KillerBench kot skrbnik.

## Preverite nosilec

1. Odprite **Shramba** in izberite **Shramba**.
2. Izberite pogon v **Nosilec za preverjanje** in pritisnite **Zaženi diagnostiko**.
3. Preberite poročilo. Prikazuje stanje diska in nosilca, oznako umazanosti, samopopravljanje NTFS med delovanjem, napake krmilnikov in diskov, števce zanesljivosti ter nedavne dogodke shrambe, kot so ponastavitve, ponovni poskusi in nepričakovane odstranitve.

Če pregled ne zazna težav, popravilo ni ponujeno. Temperatura diska, obraba in števci neodpravljenih napak V/I so dokazi za pregled in sami po sebi ne sprožijo popravila.

## Popravite datotečni sistem

1. Pritisnite **Preglej popravilo datotečnega sistema** in izberite **Ciljno popravilo** ali **Pregled in popravilo brez povezave**.
2. Za podatkovni nosilec označite **Dovoli odklop nesistemskega nosilca**, če lahko zaprete vse, kar ga uporablja. Nosilca Windows ni mogoče odklopiti, zato se njegovo popravilo načrtuje za naslednji ponovni zagon.
3. Označite **Pomembni podatki na tem nosilcu so varnostno kopirani**.
4. Potrdite popravilo. KillerBench tik pred izvedbo znova preveri točen nosilec in najprej zapiše čakajoči vnos.
5. Za načrtovano popravilo znova zaženite računalnik, ko vam ustreza. KillerBench po ponovnem zagonu preveri rezultat, ne da bi popravilo znova izvedel.

## Optimizirajte ali ponovite TRIM

Isto orodje lahko analizira nosilec in izvede optimizacijo, ki jo priporoča Windows, **Ponovi TRIM za polprevodniško shrambo** ali defragmentacijo trdega diska. Vsako je ločeno potrjeno dejanje na enem točno določenem brezhibnem nosilcu.

## Iz ukazne vrstice

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Glejte [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) in [volume-optimize](cli.html#cmd-volume-optimize).

## Povezano

- [Razdelek Shramba](storage.html)
- [Sprostite prostor na disku](free-disk-space.html)
- [Odpravljanje težav](troubleshooting.html)
