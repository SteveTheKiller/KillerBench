# Stažení a nastavení

KillerBench 1.0.0 zatím není vydán. Veřejný balíček ani stažení KillerBench zatím nejsou k dispozici. Tato stránka popisuje aktuální požadavky balíčku, abyste se mohli připravit bez stahování neoficiální kopie.

## Windows a modul runtime

- Aktuálním cílem publikování je **Windows x64**. Aktuálním základem nativního ověřování je Windows 11 x64. Nativní distribuce pro ARM64, x86 a Windows Server nejsou k dispozici.
- KillerBench se dodává ve dvou podobách. Instalační program je **závislý na rozhraní** a vyžaduje **.NET Desktop Runtime 10 pro Windows x64**. Samotný .NET Runtime, ASP.NET Core Runtime ani .NET Framework WPF neposkytují. Přenosná edice obsahuje vlastní modul runtime a nevyžaduje samostatnou instalaci. Nástroj příkazového řádku `killerbench-cli.exe` vyžaduje .NET 10 Runtime pro Windows x64. Sada SDK je potřeba pro vývoj, nikoli pro běžné používání.
- Používejte verzi Windows podporovanou [.NET 10 ve Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows). Podpora modulu runtime neznamená, že opravy KillerBench jsou ověřené v každé podporované edici nebo sestavení. Pokrytí Windows 10 zatím čeká a jeho podporované edice omezují aktuální požadavky společnosti Microsoft na životní cyklus.
- Desktop Runtime získejte pouze ze [stránky pro stažení .NET 10 od společnosti Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) a zvolte **.NET Desktop Runtime**, **Windows**, **x64**. Dodržujte pravidla vaší organizace pro instalaci softwaru.

Pokud je k dispozici `dotnet`, příkaz `dotnet --list-runtimes` by měl pro instalaci x64 uvést `Microsoft.WindowsDesktop.App 10.0.x`. Modul runtime nainstalovaný jen pro jinou architekturu instalačnímu balíčku x64 nestačí.

## Až bude balíček zveřejněn

1. Použijte oficiální stažení odkazované z tohoto webu. Žádný odkaz ke stažení KillerBench zatím není aktivní.
2. Přečtěte si požadavky daného balíčku, [licenci GPLv3](../license.txt), pokyny k odpovídajícímu zdrojovému kódu, přiložená oznámení třetích stran a informace o podpisu a kontrolním součtu. Samotný název souboru pravost neprokazuje.
3. Spusťte instalační program pro aktuální účet nebo po výzvě ke zvýšení oprávnění pro všechny uživatele. Přidá zástupce do nabídky Start, volitelného zástupce na plochu a položku v části Přidat nebo odebrat programy. Přenosná edice běží bez instalace.
4. Spusťte KillerBench pod svým běžným účtem a postupujte podle stránky [Vaše první diagnostika](first-run.html). Selhání diagnostického přístupu zůstávají viditelná; zvýšení oprávnění je samostatná záměrná volba.

Neobcházejte bezpečnostní upozornění Windows, abyste spustili neověřenou kopii. Před výběrem jakékoli akce, která mění počítač, mějte aktuální zálohu.

Při spuštění KillerBench zkontroluje na GitHubu novější stabilní vydání a před jeho instalací se zeptá. Stažený soubor musí odpovídat zveřejněnému kontrolnímu součtu SHA-256. Kontrolu vypnete možností **Kontrolovat aktualizace při spuštění** v části O aplikaci.

## Aktuální omezení

Aktuální sestavení má dvanáct sekcí, diagnostiku pouze pro čtení, zprávy a jednotlivě vybírané opravy. Nativní ověřování oprav stále probíhá. Funkční rozhraní, úspěšné sestavení ani výsledek testovací sady neprokazují chování oprav na skutečném počítači. Viz [Jak změny zůstávají bezpečné](safety.html#development-status).

Ilustrační konzole na úvodní stránce zobrazuje simulovaný výstup. První veřejný balíček stále čeká.

## Zdrojový kód, licence a podpora

KillerBench je licencován pod GPLv3. Pokyny k poskytnutí zdrojového kódu a oprávnění k písmům třetích stran pro první veřejný balíček stále čekají. Informace o dostupnosti zdrojového kódu, oznámeních o distribuci, soukromí důkazů a podpoře najdete na stránce [O KillerBench](about.html). [Příručka Engine](../engine/) je určena vývojářům s autorizovaným zdrojovým kódem; neznamená, že je k dispozici veřejný balíček Engine.
