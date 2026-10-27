# Zaženite KillerBench iz skriptov

Uporabite ukazno vrstico, ko želite dokaze KillerBench v skriptu, opravilu RMM ali oddaljeni konzoli. `killerbench-cli` uporablja isti Engine kot namizna aplikacija in izpisuje JSON.

## Prvi koraki

1. Odprite PowerShell v mapi z `killerbench-cli.exe` ali dodajte njegovo mapo v pot.
2. Zaženite `killerbench-cli help` za seznam vseh ukazov z njihovimi varnostnimi omejitvami.
3. Zaženite `killerbench-cli diagnostic-scopes` za prikaz 11 priporočenih diagnostičnih skupin.

## Izvedite pregled in shranite poročilo

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` je samo diagnostičen in zavrne `--auto-repair`. Ko potrebujete določeno dejanje, uporabite ciljni potek popravila. `--report` zapiše enako tematsko poročilo, kot ga izvozi aplikacija. Za pregled samo nekaterih področij uporabite `--scope`, za združeni JSON brez dnevnika poteka dela pa `common-check`.

## Preberite izhodne kode

| Koda | Pomen |
| --- | --- |
| 0 | Predogled ali preverjeni rezultat je uspel. |
| 1 | Napaka zagona, vnosa ali dnevnika je ustavila ukaz. |
| 2 | Argumenti niso bili prepoznani. |
| 3 | Blokirano, neuspešno ali nepreverjeno. |
| 4 | Potek dela potrebuje ponovni zagon pred preverjanjem. |
| 130 | Ustavili ste ga. |

Izhodna koda 0 pri ukazu samo za branje pomeni, da se je izvedel, ne da je računalnik brezhiben. Za ugotovitve preberite JSON.

## Izvedite spremembo

Vsak ukaz, ki lahko spremeni računalnik, privzeto prikaže predogled. Preberite ga in nato izvedite isti ukaz z `--execute`. Če se zahteva prekine, zaženite ustrezni ukaz `-recover`; sporoči dejanski izid in nikoli ne ponovi spremembe.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Znova odprite stare rezultate

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Uporabite ga iz pomočnika umetne inteligence

[KillerMCP](../mcp.html) lahko pokliče nameščeno ukazno vrstico za iskanje kod Upravitelja naprav in napak sistema Windows.

## Povezano

- [Referenčni vodnik ukazne vrstice](cli.html)
- [Vodnik za razvijalce Engine](../engine/index.html)
- [Odpravljanje težav](troubleshooting.html)
