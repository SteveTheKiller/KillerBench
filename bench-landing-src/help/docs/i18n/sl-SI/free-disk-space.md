# Sprostite prostor na disku

Uporabite, ko na pogonu zmanjkuje prostora. KillerBench pokaže porabo prostora, prikaže predogled, kaj bi vsak cilj čiščenja odstranil, in briše šele, ko vklopite trajno čiščenje.

## Preverite preostali prostor

1. Odprite **Shramba** in izberite **Shramba**.
2. Pritisnite **Zaženi diagnostiko**. Poročilo označi vsak nosilec s črko pogona, ki ima manj kot 10 % prostega prostora, ter prikaže dodelitev senčnih kopij, privzete kvote in pravilnik senzorja shrambe.
3. Za izpise zrušitev in staro vsebino posodobitev izvedite tudi **Ključne kazalnike** v **Diagnostiki** ter **Windows Update** v **Posodobitvah**; tam je prikazana velikost predpomnilnika posodobitev.

## Predogled čiščenja

1. Odprite **Vzdrževanje** in izberite **Čiščenje**.
2. Označite želene cilje, na primer **Začasne datoteke trenutnega uporabnika**, **Začasne datoteke sistema Windows**, **Predpomnilniki brskalnikov trenutnega uporabnika**, **Predpomnilnik optimizacije dostave** ali **Koš trenutnega uporabnika**. S kazalcem se pomaknite nad cilj, da preberete, katere mape natančno zajema.
3. Nastavite **Najmanjšo starost (dnevi)** in **Največ datotek na cilj**.
4. Pritisnite **Zaženi diagnostiko** z izklopljeno možnostjo **Po ponovnem preverjanju izvedi izbrano trajno čiščenje**. Poročilo navede kandidate in ocenjeni prostor za vsak cilj. Nič se ne izbriše.

## Odstranite datoteke

1. Vklopite **Po ponovnem preverjanju izvedi izbrano trajno čiščenje** in pritisnite **Zaženi diagnostiko**.
2. KillerBench pred odstranitvijo znova preveri vsakega kandidata, preskoči nedavne, zaščitene ali uporabljane datoteke in zabeleži vsak izid v dnevnik.

Trajno čiščenje obide koš in ga ni mogoče razveljaviti. Odstranjevanje poročil o napakah in izpisov zrušitev zavrže dokaze, ki jih morda potrebujete za preiskavo zrušitev, zato jih najprej preglejte.

## Odstranite prejšnjo namestitev sistema Windows

Po nadgradnji funkcij lahko **Prejšnja namestitev sistema Windows** v **Čiščenju** odstrani kopijo za vrnitev s podprto zahtevo Windows za odstranitev možnosti vrnitve. Izvaja se samostojno, zahteva **Varnostno sem kopiral vse datoteke in nastavitve, ki jih potrebujem iz prejšnje namestitve** in trajno odstrani možnost vrnitve na prejšnjo različico.

## Iz ukazne vrstice

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Glejte [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) in [previous-windows](cli.html#cmd-previous-windows).

## Povezano

- [Razdelek Vzdrževanje](maintenance.html#cleanup)
- [Razdelek Shramba](storage.html)
- [Preverite in popravite pogon](check-a-drive.html)
