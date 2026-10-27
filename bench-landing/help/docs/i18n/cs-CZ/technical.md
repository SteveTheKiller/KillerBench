# Technologie KillerBench

Pohled pod kapotu: jak KillerBench čte počítač s Windows, udržuje důkazy poctivé a rozhoduje, kdy je změna povolena.

## Technologický základ

KillerBench je nativní aplikace pro Windows. Desktopová aplikace, příkazový řádek i Engine pod nimi sdílejí stejné diagnostické důkazy a postupy oprav.

| Komponenta | Podrobnosti |
| --- | --- |
| Rozhraní | WPF na .NET 10 (`net10.0-windows`), Windows x64, vlastní rámeček okna |
| Engine | `Engine.Core`, `Engine` a `Engine.Windows`, bez odkazů na WPF ani ovládací prvky desktopu |
| Příkazový řádek | `killerbench-cli`, samostatný hostitel nad stejným Engine |
| Zprostředkovatelé Windows | .NET, rozhraní API Windows, PowerShell a nástroje příkazového řádku Windows, každý spouštěný v pevných limitech času, počtu záznamů a velikosti |
| Zprávy | Surový výstup, Čitelná zpráva, HTML, text a JSON |
| Referenční data | Verzované balíčky znalostí s kódy chyb, identifikátory hardwaru, výrobci a odkazy na podporu |
| Balení | Instalační program: závislý na rozhraní, vyžaduje .NET Desktop Runtime 10 pro Windows x64. Přenosná edice: samostatná, bez instalace modulu runtime |
| Vzhled | 13 motivů rodiny a 20 jazyků |

## Instalace, složky a data

KillerBench se dodává ve dvou podobách. Instalační program je standardní stažení a podporuje instalaci pro jednoho uživatele nebo pro všechny uživatele. Přenosná edice obsahuje vlastní modul runtime .NET a běží bez instalace.

KillerBench běží s oprávněními účtu, který jej spustil. Sám si oprávnění nezvyšuje. Pokud chráněné důkazy, například ochrany BitLocker, nebo oprava vyžadují přístup správce, spusťte jej pomocí **Spustit jako správce**. Zvýšení oprávnění nikdy nevybírá ani neschvaluje opravu.

### Co dělá instalační program

Instalační program ověří a rozbalí aplikaci do `%LOCALAPPDATA%\Programs\KillerBench\` nebo po výzvě ke zvýšení oprávnění pro všechny uživatele do `%ProgramFiles%\KillerBench\`. Přidá zástupce do nabídky Start, volitelného zástupce na plochu a položku v části Přidat nebo odebrat programy s běžnou i tichou odinstalací. Přenosná edice ověří a rozbalí svůj obsah do `%LOCALAPPDATA%\KillerBench\Portable\` pro aktuální běh a při dalším spuštění opuštěné kopie uklidí.

### Kde co najdete

| Umístění | Obsah |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Motiv, barva zvýraznění, měřítko rozhraní a jazyk |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Jeden deník JSON na každý běh postupu: rozhodnutí, ohraničený výstup příkazů a výsledky oprav |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Deníky provedení oprav údržby, například DISM a SFC |
| Složky funkcí v `%LOCALAPPDATA%\KillerBench\` | Čekající záznamy a výsledky jednotlivých chráněných akcí, například `BitLockerBackup`, `Time`, `Firewall` a `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | Úroveň oprav KillerMCP zvolená v části O aplikaci |
| `HKCU` nebo `HKLM\Software\KillerBench` | Cesta instalace pro odpovídající rozsah instalace |
| Soubor `.kbench`, který zvolíte | Uložený záznam s uchovanými důkazy |

Deníky zůstávají v tomto počítači a KillerBench je neodesílá. Deník odstraňte, až když důkazy k nejistému výsledku už nebudete potřebovat.

## Nejprve diagnostika

KillerBench považuje diagnostiku a změnu za dvě odlišné úlohy. Diagnostika pouze čte. Ke změně dojde jen tehdy, když vyberete konkrétní opravu nebo akci, zkontrolujete její možnosti a předpoklady a potvrdíte ji.

Úplná kontrola systému je pouze diagnostická, i když má některý cílený postup zapnutou automatickou opravu. Uložené záznamy, importované protokoly a historické události nikdy neopravňují k opravě.

### Ohraničené shromažďování

Každý nativní příkaz a každý zprostředkovatel Windows běží v pevných limitech času, počtu záznamů a velikosti. Zprostředkovatel zaznamená návratový stav, vypršení časového limitu, zkrácení, standardní výstup i chybový výstup. Zprostředkovatel, který selhal nebo jehož výstup byl zkrácen, nelze nikdy číst jako čistou prázdnou odpověď.

### Minimální uchovávání

Kolektory uchovávají to, co nález potřebuje. Mnoho výsledků uchovává názvy, kategorie, počty, stavy a časová razítka a vynechává přihlašovací údaje, příkazové řádky, úplné cesty, identity účtů, síťové koncové body a nesouvisející datovou část událostí.

## Důkazy, které můžete sledovat

Každý nález je propojen s důkazy, které k němu vedly, a zpráva vždy odděluje to, co bylo prokázáno, od toho, co bylo pouze pozorováno.

| Stav | Význam |
| --- | --- |
| Ke kontrole | Důkazy podporují problém, který si zaslouží pozornost. |
| Informace | Důkazy byly shromážděny a zobrazují se jako kontext. |
| Neúplné nebo Neznámé | Kontrolu nebylo možné dokončit nebo důkazy nebyly k dispozici. Nikdy se to nepočítá jako zdravý stav. |
| Historické | Událost z minulosti. Vysvětluje historii, ale neprokazuje aktuální závadu. |
| Ověřeno | Oprava nebo změna byla potvrzena opětovným přečtením výsledku. |

### Neznámé je také výsledek

Neznámé znamená, že byl odepřen přístup, chyběl zdroj, zprostředkovatel selhal, výstup byl poškozený, byl překročen limit nebo si důkazy odporovaly. KillerBench toto omezení i další užitečný krok zobrazí, místo aby je skrýval.

### Historie zůstává historií

Záznamy událostí a importované protokoly popisují, co bylo zaznamenáno v minulosti. Mohou ukázat na kód chyby nebo na další rozsah diagnostiky. Neprokazují, že závada stále trvá.

## Chráněné opravy

Opravy jsou typované operace s pravidly způsobilosti, provedení, ověření a obnovení. Nejsou to řetězce příkazů vybrané z řádku protokolu. Každá podporovaná oprava probíhá ve stejném pořadí:

1. Shromáždí úplné aktuální důkazy.
2. Najde shodu s jedním podporovaným receptem.
3. Zkontroluje požadované možnosti a důsledky.
4. Potvrdí předpoklady týkající se oprávnění správce a zásad.
5. Těsně před změnou znovu shromáždí rozhodující důkazy.
6. Zapíše trvalý záznam o přípravě.
7. Provede jednu ohraničenou nativní akci.
8. Shromáždí čerstvé důkazy pro ověření.
9. Zaznamená výsledek: ověřeno, zablokováno, zastaveno nebo nejisté.

Pokud se rozhodující důkazy změní dříve, než se akce provede, KillerBench se zastaví a akci neprovede.

### Výslovné volby

Opravy, které závisejí na některé možnosti, vyžadují vaši přesnou volbu, například svazek, tiskovou frontu, cíl klíče pro obnovení, hodnotu napájení, verzi balíčku nebo zdroj opravy. KillerBench si tyto hodnoty nikdy nevymýšlí.

### Spravované počítače

Zásady skupiny a správa zařízení mohou způsobit, že místní změna bude neúčinná nebo nevhodná. Podporované opravy zjišťují, kdo nastavení vlastní, a zastaví se, když je toto vlastnictví spravované nebo neznámé. Nastavení, která vlastní zásady, zůstávají beze změny.

### Automatická oprava

Automatická oprava pouze umožní pokračovat způsobilým receptům. Neobchází důkazy, nevybírá chybějící možnosti, neignoruje zásady, neopakuje nejistou akci, nerestartuje Windows a nemění nález, který vyžaduje kontrolu, na opravu.

## Deníky a přerušení

Než KillerBench požádá Windows o změnu, zapíše si, co se chystá udělat. Každá změna se ukládá do deníku v `%LOCALAPPDATA%\KillerBench`.

Po změně KillerBench výsledek znovu přečte a hlásí **Ověřeno** jen tehdy, když Windows ukazuje očekávaný stav. Pokud Windows hlásí úspěch, ale stav se nezměnil, výsledek to uvede a nic se neopakuje.

Proces se může zastavit poté, co Windows požadavek přijme, ale dříve, než se dokončí ověření. Čekající záznam zůstane a pozdější sladění přečte čerstvý stav, aby zjistilo, co se stalo. Sladění není automatické vrácení změn a původní změnu nikdy neopakuje. Pokud je výsledek nejistý, deník si ponechte.

## Konstanty a limity

| Parametr | Hodnota |
| --- | --- |
| Rozsah diagnostiky | Úplná kontrola systému prochází 49 diagnostických oblastí a kontroluje 96 chráněných receptů oprav |
| Čtení textového zdroje | 1 000 000 znaků na zdroj, zobrazeno 2 000 odpovídajících řádků |
| Zdroje relace | 12 v bočním panelu; záznam pojme až 24 zdrojů a 64 MB |
| Dotazy na události | 200 nejnovějších událostí na kanál za posledních 7 dní |
| Kontrola Windows Update | 15 sekund na zdroj, celkem dvě minuty |
| Zkoumání protokolů | 10 000 zpracovaných položek, 256 různých kódů, 8 výňatků po 320 znacích na kód |
| Komprimovaný archiv CBS | Archiv 256 MiB, položka protokolu 512 MiB, rozbalení 30 sekund |
| CHKDSK pouze pro čtení | Deset minut; oprava je omezena na 65 minut |
| Kontroly DNS a TCP | Ve výchozím stavu deset sekund, nejvýše jedna minuta |
| Inventář služeb | Až 1 024 nainstalovaných služeb |
| Balíčky znalostí | Až 32 otevřených najednou |
| Kontrola velkých souborů | Soubory o velikosti alespoň 100 MB |
| Monitor aktivity údržby | Interval 1 až 60 sekund, 1 až 120 snímků |
| Koncepty naplánovaných úloh | Limit doby spuštění 1 až 4 320 minut |
| Vlastní zdroje času | Až osm partnerů NTP |
| Export činnosti | Až 150 uchovaných položek |
| Jazyky a motivy | 20 jazyků, 13 motivů s variantami zvýraznění |

## Co neudělá

- Nevymaže TPM, neodstraní ochrany, které sám nevytvořil, a nezobrazí hesla pro obnovení.
- Nezmění nastavení, které řídí zásady skupiny nebo správa zařízení.
- Nespustí opravu jen proto, že historická událost nebo importovaná zpráva zmiňuje problém.
- Nezmění chybějící, částečné ani neúspěšné důkazy na verdikt o zdravém stavu.

## Zprávy

Každý běh vytvoří zprávu, kterou můžete číst, filtrovat a exportovat. Zpráva odděluje:

1. Co bylo zkontrolováno
2. Jaký problém byl nalezen
3. Jaké důkazy nález podporují
4. Jaká akce byla navržena nebo provedena
5. Jak byl výsledek ověřen
6. Co zůstává neznámé nebo vyžaduje restart

**Surový výstup** uchovává technické důkazy s původními čísly řádků, takže každý nález můžete dohledat ke zdroji. **Čitelná zpráva** shrnuje stejný běh srozumitelným jazykem a na prvním místě uvádí další bezpečný krok. Zprávy HTML používají systém motivů Killer Tools a zůstávají čitelné i bez aplikace. Text upřednostňuje přenositelnost a JSON uchovává strukturovaná pole pro jiný program.

## Uložené protokoly a záznamy

KillerBench umí zkoumat důkazy, aniž by na počítači cokoli spouštěl. Přetáhněte textový protokol do okna nebo vložte jeho cestu a zvolte **Otevřít protokol**. Řádky CBS, CSI a Setup se zpracují i s časovými razítky a nálezy odkazují na přesný původní řádek. Stejným způsobem lze importovat soubory EVTX a XML událostí.

**Prověřit Windows Update** je shromažďování na vyžádání pouze pro čtení: protokoly CBS, DISM, Panther a protokoly vrácení zpět Setup, kanály událostí aktualizací a údržby, stavy služeb aktualizací, volné místo, doba provozu, indikátory čekajícího restartu a vybrané zásady aktualizací. **Uložit záznam** zapíše uchované důkazy do souboru `.kbench`. **Otevřít záznam** jej znovu otevře bez původních souborů a znovu vyhodnotí nainstalovanými pravidly, takže stejná pravidla dají stejné nálezy.

## Ukázkový režim

Spuštěním `KillerBench.exe --demo` si vyzkoušíte **Spustit diagnostiku** na smyšlené pracovní stanici. Každý postup vytváří syntetické důkazy a časovaný průběh. Nevolá se žádný zprostředkovatel Windows, nespouští se žádný příkaz opravy a nezapisuje se žádný deník. Samostatná tlačítka nástrojů mimo **Spustit diagnostiku** zůstávají aktivní, proto je nepoužívejte jako ukázkové akce.

`KillerBench.exe --demo-screenshot` otevře zprávu Úplné kontroly systému pouze pro zobrazení pro pevně daný smyšlený počítač. Kliknutí v pracovním prostoru, klávesové zkratky a přetahování souborů jsou zablokované.

## Balíčky znalostí

Rozsáhlá a často aktualizovaná referenční data jsou uložena v balíčcích znalostí, nikoli v diagnostické logice: kódy chyb Windows a Setup, identifikátory výrobců a zařízení, reference PCI, USB, ACPI a UEFI, data výrobců OUI síťových karet, umístění ovladačů a podpory a vybrané odkazy na podporu.

Každý balíček uvádí svůj formát, verzi schématu, zdroj, revizi zdroje, čas vygenerování a hodnotu hash obsahu a KillerBench je před použitím ověří. Neznámý nebo neplatný balíček zůstane nedostupný, místo aby vedl k odhadu. Aktualizace se stáhne do dočasného umístění, ověří se a potom v jednom kroku nahradí aktivní balíček, takže neúspěšná aktualizace ponechá předchozí balíček na místě.

Data znalostí mohou vysvětlit identifikátor nebo navrhnout, kde hledat dál. Nemohou z historického kódu udělat aktuální závadu, automaticky vybrat ovladač ani opravňovat k opravě.

## Aktualizace a podepisování

Při spuštění se KillerBench zeptá GitHubu na nejnovější stabilní vydání s limitem čtyř sekund a aktualizaci nabídne jen tehdy, když je toto vydání novější. Kontrolu můžete vypnout možností **Kontrolovat aktualizace při spuštění** v části O aplikaci nebo ve výzvě k aktualizaci. Stahuje se balíček odpovídající spuštěné kopii, tedy instalační program nebo přenosná edice, a jeho SHA-256 musí odpovídat souboru `SHA256SUMS.txt` daného vydání, než se cokoli nainstaluje. Aplikace se k dokončení sama zavře a znovu otevře. Žádná služba na pozadí neexistuje.

Karta O aplikaci zobrazuje verzi, vydavatele a kryptografický otisk certifikátu pro podepisování kódu a SHA-256 spuštěného spustitelného souboru, takže můžete zkontrolovat kopii, kterou máte.

## KillerMCP

KillerBench neobsahuje agenta AI. [KillerMCP](../mcp.html) k němu může připojit agenta prostřednictvím stejného Engine. **Možnosti KillerMCP** v části O aplikaci určují, co smí agent dělat:

| Nastavení | Účinek |
| --- | --- |
| Vypnuto (výchozí) | Agent nemůže požádat o opravu. |
| Povolit KillerMCP spouštět bezpečné opravy | Agent může žádat o opravy klasifikované jako bezpečné. |
| Povolit i rušivé opravy | Zpřístupní se rušivé opravy a každá z nich stále vyžaduje schválení. |

Úroveň může zvýšit jen osoba u tohoto počítače. Po nejistém výsledku opravy zapíše KillerMCP zámek a opravy agenta zůstanou zablokované, dokud jej někdo neodstraní v části O aplikaci.

## Lokalizace

Rozhraní je k dispozici v 20 jazycích: angličtině, bengálštině, češtině, němčině, španělštině, francouzštině, maďarštině, italštině, japonštině, kazaštině, norštině, polštině, portugalštině (Brazílie), ruštině, slovinštině, turečtině, ukrajinštině, vietnamštině a zjednodušené a tradiční čínštině. Změna jazyka se projeví okamžitě, včetně názvů sekcí a postupů, uvítacího textu a otevřené čitelné zprávy. Chybějící text se zobrazí anglicky.

Důkazy, nálezy Engine, deníky a Surový výstup zůstávají v angličtině, stejně jako protokol Windows, takže zpráva vypadá na každém počítači stejně a lze v ní vyhledávat přesný text.

## Příkazový řádek a návratové kódy

`killerbench-cli` spouští stejný Engine bez desktopové aplikace. Průběh se vypisuje na standardní chybový výstup a JSON na standardní výstup. Většina příkazů ve výchozím stavu zobrazí náhled a změna vyžaduje `--execute`. Odpovídající příkazy `-recover` zkontrolují přerušenou akci, aniž by ji opakovaly. Viz [Spouštění KillerBench ze skriptů](use-the-cli.html) a [referenční příručku příkazů](cli.html).

| Návratový kód | Příkazy postupů | Kontrola a otevření |
| --- | --- | --- |
| 0 | Ve zkontrolovaných rozsazích žádný nevyřešený problém | Úplné pokrytí |
| 1 | Chyba spuštění nebo deníku | Selhání |
| 2 | Neplatné argumenty | Neplatné argumenty |
| 3 | Vyžaduje pozornost | Částečné shromáždění |
| 4 | Před ověřením je nutný restart | |
| 130 | Zastaveno | Zrušeno nebo vypršel celkový časový limit |

Žádný návratový kód neosvědčuje, že je počítač v pořádku.

## Architektura

Engine je rozdělen do tří projektů bez uživatelského rozhraní. `Engine.Core` poskytuje primitiva a deklarativní recepty, `Engine` vlastní sdílené postupy a důkazy a `Engine.Windows` registruje nativní zprostředkovatele. Desktopová aplikace, CLI a testy jsou samostatní hostitelé.

| Vrstva | Odpovědnost |
| --- | --- |
| Kolektory schopností | Čtou ohraničený stav Windows pro jeden přesný rozsah |
| Analyzátory důkazů | Ověřují tvar, identitu, úplnost a limity |
| Diagnostická interpretace | Převádí důkazy na výslovné stavy postupů |
| Orchestrace postupů | Spojuje kontroly, průběh, recepty a deníky |
| Koordinátory oprav | Znovu kontrolují způsobilost, připravují, mění, ověřují a slaďují |
| Zprávy | Prezentují problémy, akce, výsledky a omezení |
| Balíčky znalostí | Překládají verzovaná referenční data beze změny logiky |

Kolektory se při nejistotě zablokují, když je identita nejednoznačná, výstup je poškozený, je překročen limit nebo nelze zkontrolovat požadovaná oprávnění. Engine je dnes určen pro Windows a není multiplatformní. [Vývojářská příručka Engine](../engine/index.html) popisuje API, CLI a začlenění Engine do jiného hostitele .NET.

## Soukromí a síť

Diagnostika a analýza protokolů probíhají místně, bez služby AI a bez účtu KillerBench. Deníky a uchované důkazy zůstávají ve složkách `%LOCALAPPDATA%\KillerBench` místního účtu.

Akce, které vyberete, mohou kontaktovat svůj cíl, například Windows Update, služby pro stahování softwaru nebo zálohování klíče pro obnovení do Active Directory nebo Entra ID. Kontrola aktualizací při spuštění kontaktuje GitHub a aktualizace znalostí i instalační program KillerMCP kontaktují své nakonfigurované služby. Klient AI, který používá [KillerMCP](../mcp.html), může obdržet referenční výsledek, o který požádal.

Surové protokoly, importované soubory a exportované důkazy mohou obsahovat názvy počítačů, účty, cesty nebo citlivý zdrojový text. Před sdílením je zkontrolujte a do veřejné zprávy nikdy nevkládejte klíče pro obnovení, přihlašovací údaje ani data zákazníků.

## Stav vývoje

KillerBench je ve vývoji a veřejné stažení zatím není k dispozici. Rozsah oprav se stále rozšiřuje a nativní ověřování oprav stále probíhá. Úspěšné sestavení nebo simulovaná kontrola neprokazuje nativní chování oprav. K ověření každé nativní změny v různých verzích Windows a stavech selhání se používají jednorázové virtuální počítače s Windows. Úplný model bezpečnosti popisuje stránka [Jak změny zůstávají bezpečné](safety.html).

## Slovníček

Srozumitelné definice pojmů používaných na této stránce v abecedním pořadí.

| Pojem | Význam |
| --- | --- |
| Balíček znalostí | Verzovaný a ověřený soubor referenčních dat, například kódů chyb nebo identifikátorů hardwaru. |
| BITS | Služba inteligentního přenosu na pozadí, služba Windows, která na pozadí stahuje aktualizace a další soubory. |
| Blokování při nejistotě | Když jsou důkazy nejednoznačné, chybějící nebo příliš velké, výsledkem je Neznámé a žádná změna není povolena. |
| CBS | Component-Based Servicing, subsystém Windows, který instaluje aktualizace a funkce. Jeho protokolem je `CBS.log`. |
| Čekající záznam | Záznam, který označuje, že změna mohla být odeslána do Windows. Brání jejímu opakování, dokud sladění nepřečte výsledek. |
| Deník | Místní záznam JSON zapsaný před změnou a aktualizovaný po ní, aby bylo možné přerušenou akci později zkontrolovat. |
| DISM | Deployment Image Servicing and Management, nástroj Windows, který kontroluje a opravuje úložiště komponent. |
| Důkazy | To, co KillerBench skutečně přečetl z Windows, uchované i se zdrojem, aby bylo možné nález dohledat. |
| Ověřeno | Výsledek byl znovu přečten z Windows a odpovídá tomu, čeho měla změna dosáhnout. |
| Panther | Složka, do které Instalační program systému Windows zapisuje protokoly upgradu. |
| Postup | Jeden běh nástroje: kontroly, které shromažďuje, recepty, které může použít, a zpráva, kterou vytváří. |
| Recept | Typovaná oprava s vlastními pravidly způsobilosti, provedení, ověření a obnovení. |
| SFC | System File Checker, nástroj Windows, který ověřuje a obnovuje chráněné systémové soubory. |
| Sladění | Přečtení čerstvého stavu po přerušení, aby se zjistilo, co se stalo, bez opakování změny. |
| TPM | Trusted Platform Module, bezpečnostní čip, který BitLocker používá k ochraně klíče jednotky. |
| Vlastník zásad | Zásady skupiny nebo správa zařízení, které řídí nastavení. KillerBench vlastněná nastavení nemění. |
| Záznam | Uložený soubor `.kbench` s uchovanými důkazy, který lze znovu zkontrolovat bez původního počítače. |
| Zvýšení oprávnění | Spuštění s oprávněními správce po výzvě Windows. KillerBench si oprávnění nikdy sám nezvyšuje. |
