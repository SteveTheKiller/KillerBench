# Aplikace

Aplikace kontroluje nainstalované programy, balíčky Store, Office a Outlook, rozšíření prohlížečů, aplikace při spuštění a vyhledávání. Nástroje pro odebrání a resetování vždy nejprve zobrazí kontrolu a změní jen to, co vyberete.

Návody krok za krokem: [Nastavení nového počítače](set-up-new-pc.html).

## Nástroje v této sekci

Kontrolujte nainstalované aplikace, balíčky Store, Office a Outlook, rozšíření prohlížečů a vyhledávání.

### Nainstalované aplikace

Čte registrace odinstalace aplikací ve vybraném rozsahu počítače nebo aktuálního účtu a potom tento rozsah spojí s ohraničenými důkazy z protokolu Aplikace, Sledování spolehlivosti a restartů. Ostatní účty a přenosné aplikace jsou vyloučeny. Registrace a obecná historie nejsou přesným důkazem stavu ani vlastnictví aplikace. Nevybírá se žádná akce instalačního programu, odinstalačního programu, aplikace, procesu, služby, souboru, registrace, účtu, zásad ani restartu.

### Balíčky aplikací

Inventarizuje zaregistrované balíčky Appx/MSIX pro vybraný rozsah účtů. Výchozí je aktuální účet; všichni uživatelé vyžadují přístup správce. Uvádí přesné identity, příznaky frameworku a neodebratelnosti a počty závislostí. Zřízené balíčky, aplikace Win32, spravované vlastnictví a data aplikací se nevyhodnocují. Žádné balíčky se neodebírají.

### Rozšíření prohlížeče

Ohraničeně inventarizuje identity rozšíření Chrome, Edge, Brave a Firefox z aktuálního účtu. Historie prohlížení, přihlašovací údaje, nastavení rozšíření, obsah manifestů, příkazy a hodnoty zásad jsou vyloučeny. Přítomnost neprokazuje povolení, důvěryhodnost, vlastnictví ani bezpečné odebrání. Žádný prohlížeč, rozšíření ani zásada se nemění.

### Aplikace při spuštění

Inventarizuje názvy registrací Run a RunOnce počítače a aktuálního účtu, názvy souborů ve složce Po spuštění, stavy schválení spouštění Windows a názvy a stavy naplánovaných úloh při spuštění nebo přihlášení a potom vrátí jeden závěr o prostředí spouštění. Příkazové řádky, cílové cesty, binární časová razítka schválení, akce úloh, argumenty, účty a přihlašovací údaje se neshromažďují. Registrace neprokazuje, že se položka spustila, způsobila zpoždění, je nežádoucí nebo že ji lze bezpečně zakázat. Stav schválení neprokazuje, že existuje odpovídající registrace. Nevybírá se žádná změna registrace, souboru, aplikace, úlohy, služby ani zásad.

### Odebrání aplikací Microsoft

Kontroluje vybrané registrace OneDrive, Outlook, Office/M365, Project a Teams s volbou rozsahu účtů a uchování. Inventáře počítače, aktuálního účtu a zaregistrovaných balíčků se vzájemně porovnávají; kontrola pro všechny uživatele zahrnuje také balíčky zřízené pro nové uživatele. Podporovaný balíček aktuálního účtu může v desktopové aplikaci nebo CLI dostat samostatný přesný plán zabezpečení. Odebrání tohoto jednoho balíčku je samostatná chráněná akce v desktopové aplikaci s vlastními potvrzeními. Samotná kontrola nemění žádný proces, aplikaci, balíček, soubor, přihlašovací údaj, umístění složky, hodnotu registru ani uživatelská data.

### Reset Outlooku

Kontroluje nový Outlook, klasický Outlook nebo oba s výslovnými volbami pro mezipaměti, nastavení profilu, ověřování, soubory OST, doplňky a přeinstalaci nového Outlooku. Kontrola aktuálního účtu prověřuje ohraničená známá umístění, přesné registrace a připravenost uzavřených procesů. Samostatně potvrzovaná akce pouze pro mezipaměť uchová přesné adresáře mezipaměti pod jedinečnými názvy pro obnovení a ověří každé přesunutí. Kontrola pro všechny uživatele a ostatní oblasti resetu zůstávají pouze ke kontrole. Neodebírá se žádná uchovaná mezipaměť, profil, soubor OST, přihlašovací údaj, doplněk, balíček ani nastavení.

### Inventář Foxit

Inventarizuje produkty Foxit nainstalované v počítači a hodnoty registru aktualizačního programu v obou zobrazeních. Uživatelské instalace, účinné zásady a vlastnictví produktů se neověřují. Žádné aplikace ani nastavení se nemění.

### Služba Windows Installer

Před obnovením služby kontroluje zásady omezení, vrácení zpět, zvýšení oprávnění, protokolování a ladění Installeru, značky probíhajících operací, binární soubory modulu a viditelnost zaregistrovaných balíčků v mezipaměti. Nedávné neúspěšné a úspěšné výsledky porovnává jen pro přesně hlášené identity produktů. Pokud předpoklady projdou, automatická oprava může spustit povolenou službu a ověřit stav Spuštěno. Historické výsledky, protokolování, viditelnost mezipaměti a zjištění ladění neopravňují ke změnám balíčků ani zásad. Zásady, protokoly, značky transakcí, typy spouštění a balíčky v mezipaměti se zachovávají. Instalace konkrétního balíčku se netestuje.

### Windows Search

Kontroluje dostupnost služby Windows Search, aktuální aktivitu SystemIndex a nedávnou historii událostí Search. Automatická oprava může spustit zastavenou povolenou službu a ověřit spuštěný stav. Samostatná výslovná volba opětovného sestavení indexu uchová předchozí index, spustí nový index a ověří přesný stav služby a adresáře. Indexovaná umístění, zásady, typ spouštění a uchovaná data se zachovávají. Spuštění služby nebo opětovné sestavení indexu může obnovit indexování na pozadí. Úplnost výsledků vyhledávání zůstává neotestovaná.

### Připravenost aplikací

Kontroluje dostupnost služby Připravenost aplikací. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující první přihlášení a zřizování aplikací mohou podle aktuálních zásad pokračovat. Balíčky, registrace, licence, uživatelské profily, data aplikací, zásady a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci s balíčky. Ověření pokrývá pouze stabilní stav služby, nikoli přihlášení, zřizování ani spuštění aplikace.

### Služba úložiště stavu

Kontroluje dostupnost Služby úložiště stavu. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující požadavky na stav aplikačního modelu mohou pokračovat. Registrace, balíčky, licence, profily, data prohlížečů a aplikací, obsah úložiště, zásady a typ spouštění se zachovávají. Nepožaduje se žádný reset, opětovná registrace ani odstranění. Ověření pokrývá pouze stabilní stav služby, nikoli konzistenci úložiště ani provoz aplikací.

### Služba nasazení AppX

Kontroluje dostupnost Služby nasazení AppX. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující požadavky na nasazení, registraci nebo údržbu mohou podle aktuálních zásad pokračovat. Balíčky, registrace, licence, profily, data aplikací, obsah úložiště, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci s balíčky. Ověření pokrývá pouze stabilní stav služby, nikoli nasazení, registraci ani spuštění aplikace.

### Služba licencí klienta

Kontroluje dostupnost Služby licencí klienta. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující požadavky na licence aplikací Store mohou podle aktuálních zásad pokračovat. Balíčky, registrace, licence, tokeny, účty, profily, data aplikací, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci s balíčky, licencemi, tokeny ani účty. Ověření pokrývá pouze stabilní stav služby, nikoli ověření licence ani spuštění aplikace.

### Instalační služba Microsoft Store

Kontroluje nedávnou historii instalací Microsoft Store a přesnou dostupnost instalační služby Store. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující požadavky na stahování a instalaci ze Store mohou podle aktuálních zásad pokračovat. Balíčky, registrace, licence, účty, data aplikací, stav stahování, zdroje, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci s balíčky, stahováním, licencemi, účty, zdroji ani instalací. Ověření pokrývá pouze stabilní stav služby, nikoli výsledky stahování, instalace, licencí ani aplikací.

### Služba Správce licencí systému Windows

Kontroluje dostupnost Služby Správce licencí systému Windows. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující kontroly licencí aplikací mohou podle aktuálních zásad pokračovat. Balíčky, registrace, licence, tokeny, účty, data aplikací, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci s balíčky, licencemi, tokeny, účty, registracemi ani nároky. Ověření pokrývá pouze stabilní stav služby, nikoli ověření licence, nároku ani spuštění aplikace.

### Služba PushToInstall systému Windows

Kontroluje dostupnost Služby PushToInstall systému Windows. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující dříve schválený požadavek na vzdálenou instalaci může podle aktuálních zásad pokračovat. Balíčky, registrace, licence, účty, data aplikací, požadavky ve frontě, zdroje, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevytváří, neschvaluje ani nevybírá žádný požadavek, balíček, zdroj, účet ani instalaci. Ověření pokrývá pouze stabilní stav služby, nikoli zpracování požadavků, stahování, instalaci ani výsledky aplikací.
