# Odpravite težave s tiskanjem

Uporabite, ko opravila obtičijo v vrsti, tiskalnik kaže stanje brez povezave ali je privzeti tiskalnik napačen. KillerBench najprej prebere tiskanje v ozadju, čakalne vrste, opravila, gonilnike in vrata, nato omogoči popravilo ene točno določene vrste ali opravila.

## Preverite tiskalnike

1. Odprite **Naprave** in izberite **Tiskanje**.
2. Pritisnite **Poišči lokalne tiskalnike** in izberite vrsto s težavami. Za pregled vseh vrst izberite **Uporabi splošna preverjanja tiskanja**.
3. Pustite **Samodejno popravi potrjene težave** izklopljeno in pritisnite **Zaženi diagnostiko**.
4. Preberite poročilo. Prikazuje stanje storitve Tiskalni posrednik, začasno ustavljene vrste, način brez povezave, vidna opravila, stanja gonilnikov, registracije vrat, nedavne napake PrintService in pri omrežnih tiskalnikih en poskus povezave z dodeljenim naslovom tiskalnika.

Uspešna povezava ali koda stanja brez napak ne dokazuje, da ima tiskalnik papir, toner ali delujoč mehanizem.

## Popravite čakalno vrsto

1. Znova zaženite **Tiskanje** z vklopljeno možnostjo **Samodejno popravi potrjene težave**. Lahko zažene omogočeno storitev tiskanja v ozadju, nadaljuje točno določeno začasno ustavljeno vrsto s čakajočimi opravili in odpravi način brez povezave.
2. Odprava načina brez povezave lahko sprosti čakajoča opravila, zato se prepričajte, da jih je še treba natisniti.
3. Vrsta, ki jo je nekdo namenoma začasno ustavil in v njej nič ne čaka, ostane ustavljena.

## Odstranite eno obtičalo opravilo

1. Pritisnite **Poišči blokirana tiskalna opravila**.
2. Izberite opravilo s stanjem Napaka ali Blokirano in označite **Prekliči samo to izbrano opravilo**.
3. Pritisnite **Zaženi diagnostiko**. Prekliče se samo to opravilo, rezultat pa se znova prebere.

## Popravite privzeti tiskalnik

Izberite vrsto, ki naj bo privzeta, označite **S to čakalno vrsto popravi manjkajočo ali nasprotujočo si privzeto izbiro** in pritisnite **Zaženi diagnostiko**. Dejanje se izvede samo, če privzeta izbira manjka ali je v navzkrižju.

## Iz ukazne vrstice

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Glejte [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) in [default-printer-set](cli.html#cmd-default-printer-set).

## Povezano

- [Razdelek Naprave](devices.html#printing)
- [Odpravite težave z omrežjem in internetom](fix-network.html)
