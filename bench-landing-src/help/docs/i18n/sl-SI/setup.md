# Prenos in namestitev

KillerBench 1.0.0 še ni izdan. Javni paket ali prenos KillerBench še ni na voljo. Ta stran opisuje trenutne zahteve paketa, da se lahko pripravite brez prenosa neuradne kopije.

## Windows in izvajalno okolje

- Trenutni cilj objave je **Windows x64**. Windows 11 x64 je trenutna osnova preverjanja v dejanskem sistemu. Različice za ARM64, x86 in Windows Server niso na voljo.
- KillerBench ima dve obliki paketa. Namestitveni program je **odvisen od ogrodja** in zahteva **.NET Desktop Runtime 10 za Windows x64**. Navadni .NET Runtime, ASP.NET Core Runtime in .NET Framework ne zagotavljajo WPF. Prenosna izdaja vključuje svoje izvajalno okolje in ne potrebuje ločene namestitve. Orodje ukazne vrstice `killerbench-cli.exe` potrebuje .NET 10 Runtime za Windows x64. SDK potrebujete za razvoj, ne za običajno uporabo.
- Uporabite različico sistema Windows, ki jo podpira [.NET 10 v sistemu Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows). Podpora izvajalnega okolja ne pomeni, da so popravila KillerBench preverjena na vsaki podprti izdaji ali gradnji. Preverjanje za Windows 10 še čaka, podprte izdaje pa omejujejo trenutne Microsoftove zahteve življenjskega cikla.
- Desktop Runtime pridobite samo z [Microsoftove strani za prenos .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), kjer izberite **.NET Desktop Runtime**, **Windows**, **x64**. Upoštevajte pravila svoje organizacije za namestitev programske opreme.

Če je `dotnet` na voljo, mora `dotnet --list-runtimes` za namestitev x64 navesti `Microsoft.WindowsDesktop.App 10.0.x`. Izvajalno okolje, nameščeno samo za drugo arhitekturo, ne izpolnjuje zahtev namestitvenega paketa x64.

## Ko bo paket objavljen

1. Uporabite uradni prenos, povezan s tega spletnega mesta. Povezava za prenos KillerBench še ni dejavna.
2. Preberite zahteve paketa, [licenco GPLv3](../license.txt), navodila za pripadajočo izvorno kodo, priložena obvestila tretjih oseb ter podatke o podpisu in kontrolni vsoti. Samo ime datoteke ne dokazuje pristnosti.
3. Zaženite namestitveni program za trenutni račun ali po pozivu za povišanje pravic za vse uporabnike. Doda bližnjico v meni Start, izbirno bližnjico na namizju in vnos v Dodaj ali odstrani programe. Prenosna izdaja deluje brez namestitve.
4. Zaženite KillerBench z običajnim računom in sledite strani [Vaša prva diagnostika](first-run.html). Napake pri dostopu do diagnostičnih podatkov ostanejo vidne; povišanje pravic je ločena zavestna odločitev.

Ne obidite varnostnih opozoril sistema Windows, da bi zagnali nepreverjeno kopijo. Pred izbiro dejanja, ki spremeni računalnik, imejte svežo varnostno kopijo.

Ob zagonu KillerBench preveri, ali je na GitHubu novejša stabilna izdaja, in vas pred namestitvijo vpraša. Prenos se mora ujemati z objavljeno kontrolno vsoto SHA-256. Preverjanje izklopite z možnostjo **Ob zagonu preveri posodobitve** v podatkih o aplikaciji.

## Trenutne omejitve

Trenutna različica ima dvanajst razdelkov, diagnostiko samo za branje, poročila in posamično izbrane možnosti popravil. Preverjanje popravil v dejanskem sistemu še poteka. Delujoč vmesnik, uspešna gradnja ali rezultat nadzorovanega preizkusa ne dokazuje delovanja popravil na dejanskem računalniku. Glejte [Kako spremembe ostanejo varne](safety.html#development-status).

Ponazoritvena konzola na pristajalni strani prikazuje izmišljene rezultate. Prvi javni paket še čaka.

## Izvorna koda, licenca in podpora

KillerBench je licenciran pod GPLv3. Navodila za dostavo izvorne kode in dovoljenja za pisave tretjih oseb za prvi javni paket še niso dokončana. Preberite [O aplikaciji KillerBench](about.html) za razpoložljivost izvorne kode, obvestila o distribuciji, zasebnost dokazov in podatke za podporo. [Vodnik po pogonu](../engine/) je namenjen razvijalcem z dovoljenim dostopom do izvorne kode; ne pomeni, da je javni paket pogona na voljo.
