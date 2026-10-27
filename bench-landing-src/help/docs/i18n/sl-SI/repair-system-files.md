# Popravite sistemske datoteke

Uporabite, ko so komponente Windows poškodovane: funkcij ni mogoče namestiti, posodobitve sporočajo napake komponent ali se sistemska orodja zrušijo. KillerBench izvede DISM in SFC šele, ko trenutno preverjanje potrdi poškodbo, nato preveri rezultat.

## Pred začetkom

- Zaženite KillerBench kot skrbnik.
- Najprej izvedite morebitni čakajoči ponovni zagon.
- Če je računalnik upravljan, preverite, ali pravilnik usmerja DISM na določen vir za popravilo. KillerBench ta pravilnik prikaže in ga ne spremeni.

## Preverite poškodbe

1. Odprite **Popravilo** in izberite **Sistemske datoteke**.
2. Pustite **Samodejno popravi potrjene težave** izklopljeno in pritisnite **Zaženi diagnostiko**.
3. Preberite poročilo. Popiše stanja paketov vzdrževanja, izbirnih funkcij in zmogljivosti, preveri pogoje ter pregleda zaščitene sistemske datoteke. Če ni zaznane težave, popravilo ni ponujeno.

## Popravite potrjene poškodbe

1. Vklopite **Samodejno popravi potrjene težave** in pritisnite **Izvedi popravilo**. KillerBench izbere DISM ali SFC samo, ko trenutno preverjanje potrdi poškodbo.
2. Počakajte na preverjanje. Poročilo prikaže **Preverjeno** samo, ko naknadno preverjanje prebere popravljeno stanje.
3. Če poročilo pove, da shrambe ni mogoče obnoviti ali da vir za popravilo ni na voljo, se ustavite. Ponavljanje popravila ne bo spremenilo izida.

Prekinjeno izvajanje DISM ali SFC se ob naslednjem zagonu uskladi brez ponovitve ukaza vzdrževanja.

## Počistite shrambo komponent

V razdelku **Posodobitve** izberite **Čiščenje shrambe komponent**. Orodje najprej vpraša Windows, ali je čiščenje priporočeno. Izbirno vzdrževanje odstrani nadomeščene komponente in znova analizira stanje. Nikoli ne uporabi ResetBase in nikoli ročno ne izbriše vsebine WinSxS.

## Iz ukazne vrstice

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Glejte [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) in [servicing-logs](cli.html#cmd-servicing-logs). `repair` privzeto prikaže predogled in deluje samo z `--execute`.

## Povezano

- [Popravite Windows Update](fix-windows-update.html)
- [Razdelek Popravilo](repair.html#system-files)
- [Kako spremembe ostanejo varne](safety.html)
