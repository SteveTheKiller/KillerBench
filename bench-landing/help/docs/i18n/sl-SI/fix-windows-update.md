# Popravite Windows Update

Uporabite, ko posodobitve ne uspejo, obstanejo ali se nenehno znova nameščajo. KillerBench najprej prebere verigo posodobitev, pojasni napako z dokazi in zažene storitev ali popravi shrambo komponent samo, ko sveži dokazi potrdijo točno to težavo.

## Pred začetkom

- Zaženite KillerBench kot skrbnik. Za popolne dokaze o storitvah posodobitev, shrambi komponent in zaščitenih sistemskih datotekah so potrebne povišane pravice.
- Če Windows že čaka na ponovni zagon, ga najprej znova zaženite. Številna popravila posodobitev so blokirana, dokler zahteva po ponovnem zagonu ni razrešena.
- Med vzdrževanjem naj bo računalnik priključen na električno omrežje.

## Ugotovite, zakaj posodobitve ne uspejo

1. Odprite **Posodobitve** in v stranski vrstici orodij izberite **Windows Update**.
2. Pri prvem pregledu pustite **Samodejno popravi potrjene težave** izklopljeno in pritisnite **Zaženi diagnostiko**.
3. Preberite **Pregledno poročilo**. Navede ustavljene storitve posodobitev, čakajoče ponovne zagone, premalo prostega prostora, poškodbe shrambe komponent in točne kode napak, ki jih je zabeležil Windows, s priporočenim naslednjim korakom.
4. Če poročilo navede kodo napake, se ustrezna dodatna preverjanja izvedejo samodejno. Kodo lahko poiščete tudi sami z `killerbench-cli update-code 0x80070002`.

## Spremljajte posodobitev, ki je videti ustavljena

V razdelku **Posodobitve** izberite **Spremljanje dejavnosti vzdrževanja** in pritisnite **Zaženi diagnostiko**. Orodje večkrat zajame stanje posodobitev, namestitve in programa Installer, skupne količine prenosov ter kazalnike ponovnega zagona. Dejavnost je prikazana samo kot dokaz; sama po sebi nikoli ne potrjuje zastoja ali napake.

## Popravite ugotovitve poročila

1. Znova zaženite **Windows Update** z vklopljeno možnostjo **Samodejno popravi potrjene težave**. Lahko zažene omogočene storitve posodobitev, za katere je potrjeno, da so ustavljene, DISM ali SFC pa izvede samo, ko trenutno preverjanje potrdi poškodbo.
2. Za posamezno storitev izberite njeno ciljno orodje, na primer **Storitev BITS**, **Kriptografske storitve**, **Namestitveni program modulov Windows** ali **Storitev usklajevanja posodobitev**. Vsako ponudi obnovitev samo za točno to storitev.
3. Ko poročilo zahteva ponovni zagon, računalnik znova zaženite pred drugimi opravili. KillerBench zabeleži izide, ki zahtevajo ponovni zagon, in odloži nadaljnje delo do njegovega zaključka.

Vsako popravilo se zabeleži v dnevnik, rezultat pa se znova prebere. Celotno zaporedje opisuje [Kako spremembe ostanejo varne](safety.html).

## Namestite določeno posodobitev

1. V razdelku **Posodobitve** izberite **Razpoložljive posodobitve** in pritisnite **Zaženi diagnostiko**, da odkrijete posodobitve iz nastavljenega vira.
2. Izberite želene posodobitve in pritisnite **Predogled izbranih**. Predogled natančno navede, kaj bi se namestilo, in morebitno zahtevo po ponovnem zagonu.
3. Namestite šele po pregledu predogleda. KillerBench nikoli ne izbere ali odobri posodobitve namesto vas.

## Iz ukazne vrstice

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Glejte [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) in [update-plan](cli.html#cmd-update-plan). Če je bila namestitev prekinjena, zaženite [update-recover](cli.html#cmd-update-recover); preveri izid in nikoli ne ponovi posodobitve.

## Povezano

- [Razdelek Posodobitve](updates.html)
- [Popravite sistemske datoteke](repair-system-files.html)
- [Odpravljanje težav](troubleshooting.html)
