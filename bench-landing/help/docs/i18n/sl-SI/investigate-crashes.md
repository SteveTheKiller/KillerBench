# Raziščite zrušitve in modre zaslone

Uporabite, ko se Windows nepričakovano znova zažene, prikaže zaustavitveno napako ali se aplikacija nenehno sesuva. KillerBench v eno poročilo zbere dokaze o zrušitvah, zaustavitvah, napakah strojne opreme in zanesljivosti. Opiše, kaj je bilo zabeleženo, in nikoli ne ugiba o osnovnem vzroku.

## Preverite računalnik

1. Odprite **Diagnostika** in izberite **Ključni kazalniki**.
2. Pritisnite **Zaženi diagnostiko**. Poročilo združi pomnilnik, stanje diskov, težave naprav, zgodovino Nadzornika zanesljivosti, zapise napak strojne opreme, poročila o zrušitvah, nepričakovane zaustavitve in rezultate diagnostike pomnilnika Windows.
3. Preberite sklep in dokaze pod njim. Pretekle zrušitve pojasnjujejo, kaj se je zgodilo; ne dokazujejo, da je napaka še prisotna.

## Zožite preiskavo

- **Zgodovina napak strojne opreme** prebere zapise WHEA in diagnostike pomnilnika skupaj s težavami naprav ter stanjem diskov.
- **Zgodovina zanesljivosti** združi zadnjih 14 dni zrušitev aplikacij, neodzivnosti in nepričakovanih zaustavitev.
- **Napake aplikacij** se osredotoči na programe, ki se sesuvajo.
- **Zgodovina namestitev gonilnikov** pokaže neuspešne namestitve gonilnikov, povezane s trenutno prisotnimi napravami.
- **Težave z napravami** navede naprave s kodami napak Upravitelja naprav.

## Preberite izpise zrušitev

KillerBench navede nastavitve izpisov in metapodatke njihovih datotek ter lahko analizira strukturo majhnega izpisa za izjemo in imena modulov. Vsebina izpisov se v namizni aplikaciji nikoli ne odpre. Če je naložen paket znanja za zaustavitvene napake, se kode razrešijo v dokumentirani pomen.

Ohranite izpise do konca preiskave. **Čiščenje** jih lahko odstrani, vendar s tem zavrže dokaze.

## Iz ukazne vrstice

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Glejte [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) in [device-code](cli.html#cmd-device-code).

## Povezano

- [Razdelek Diagnostika](diagnostics.html#vitals)
- [Poročila in dokazi](reports.html)
