# Diagnostika

Diagnostikou by měla začínat každá návštěva. Každý nástroj přečte počítač a vytvoří zprávu. Nic zde Windows nemění. Výchozí nástroj, úplná kontrola systému, spustí širokou sadu kontrol pouze pro čtení a seřadí, co vyžaduje pozornost.

Návody krok za krokem: [Zkoumání pádů a modrých obrazovek](investigate-crashes.html) a [Oprava systémových souborů](repair-system-files.html).

## Nástroje v této sekci

Nejprve zkontrolujte počítač a potom si ve zprávě přečtěte, co vyžaduje pozornost a jaký je další bezpečný krok.

### Úplná kontrola systému

Zkontroluje 49 diagnostických oblastí a potom vyhodnotí každý nainstalovaný recept na obnovení služeb spolu s recepty pro údržbu, DHCP, tisk, Defender, čas a WMI. Kontroly mohou trvat několik minut. Automatická oprava je volitelná a vyžaduje aktuální důkazy, předpoklady počítače a kontroly závislostí specifické pro každý recept. Způsobilé opravy mohou obnovit chybějící zapůjčení DHCP, uvolnit tiskové úlohy z fronty, znovu synchronizovat hodiny, aktualizovat signatury Defenderu, obnovit způsobilé služby a opravit potvrzené poškození Windows. Zastavené automatické služby mohou být spuštěny po ověření jejich okna spuštění a závislostí. Zastavená služba spouštěná na vyžádání zůstává beze změny, pokud je jediným důkazem stav nečinnosti; úzce zaměřené postupy ji mohou vyžadovat jen tehdy, když spuštění podporují aktuální důkazy o funkci. Typy spouštění, definice úloh, zprávy, výpisy, odběry, přihlašovací údaje, nastavení kanálů, zásady brány firewall a spravovaná nastavení se zachovávají. Historické snímky, snímky záloh a přenosů jsou pouze pro čtení. Obsah výpisů se neotevírá. Kontrola příznaku nečistoty neplánuje opravu ani restart. Tento postup neinstaluje aktualizace, nemění ovladače, nemaže soubory a neprovádí úplný test hardwaru.

### Životní funkce

Zaznamená dostupnou paměť, stav disků hlášený zprostředkovatelem a stav ochrany BitLocker a potom porovná aktuální životní funkce s problémy se zařízeními, historií spolehlivosti, záznamy hardwarových chyb, důkazy o pádech, neočekávanými vypnutími a výsledky Diagnostiky paměti Windows. Indikátory restartu, volné místo na systémovém disku, zvýšení oprávnění a konfigurace služby aktualizací zůstávají viditelné. Hesla pro obnovení se neshromažďují. V tomto postupu se nespouštějí žádné opravy. Kombinovaný výsledek není úplným testem hardwaru, softwaru ani stability.

### Historie spolehlivosti

Přečte záznamy Sledování spolehlivosti Windows za posledních 14 dní spolu s nedávnými chybami v protokolu Aplikace a událostmi neočekávaného vypnutí a potom vrátí jeden ohraničený historický závěr. Historické záznamy neurčují aktuální stav ani hlavní příčinu. Nevybírá se žádná oprava a nemění se žádné aplikace, služby ani nastavení.

### Výkon spouštění

Přečte metadata událostí výkonu spouštění a vypínání Windows za posledních 14 dní, přičemž uchová jen identitu zprostředkovatele a ohraničená číselná pole doby trvání, a potom je porovná s podporovanými indikátory čekajícího restartu. Cesty, názvy procesů, účty a ostatní datová část událostí se před uložením důkazů odstraní. Historické záznamy o zhoršení neurčují aktuální úzké hrdlo ani neopravňují ke změnám spouštění, služeb, aplikací, ovladačů, úloh, napájení, restartu nebo zásad.

### Historie chyb hardwaru

Přečte historii Windows Hardware Error Architecture a diagnostiky paměti za posledních 14 dní, aktuální problémy se zařízeními Plug and Play, stav fyzických disků a čítače spolehlivosti zprostředkovatele a potom vrátí jeden ohraničený závěr o hardwaru. Záznamy je třeba porovnat s jejich datovou částí, diagnostikou firmwaru a hardwarovými testy dodavatele. Neodvozuje se žádná vadná komponenta ani hlavní příčina, nevybírá se žádná oprava a nemění se žádný hardware, firmware, ovladač, disk, fond, svazek, soubor, služba, napájení, restart ani nastavení.

### Historie instalace ovladačů

Přečte uchovaný konec historie instalace zařízení Windows SetupAPI, porovná neúspěšné oddíly s přesnými aktuálními identitami Plug and Play a vrátí jeden ohraničený závěr. Historické oddíly protokolu neprokazují, že je zařízení stále přítomné ani že je selhání aktuální. Žádný ovladač se neinstaluje, neodebírá, nevrací ani neaktualizuje.

### Body Obnovení systému

Vypíše až 256 dostupných bodů Obnovení systému, zkontroluje výslovnou způsobilost k vytvoření bodu a vrátí jeden ohraničený závěr o připravenosti. Inventář neprokazuje, že je ochrana zapnutá pro každý svazek ani že obnovení proběhne úspěšně. Během diagnostiky se žádný bod obnovení nevytváří, nemaže ani nepoužívá.

### Prostředí Windows Recovery

Přečte aktuální konfiguraci Prostředí Windows Recovery pomocí REAgentC, porovná povolené místní umístění s přesným aktuálním diskem a oddílem a vrátí jeden ohraničený závěr o připravenosti. Volitelná automatická oprava může povolit existující zakázanou konfiguraci a potom ověřit hlášený stav. Bitová kopie pro obnovení se nestahuje, nepřesouvá ani nenahrazuje, nespouští se žádné spuštění do režimu obnovení a Windows se nerestartuje. Stav povoleno a přítomnost oddílu neprokazují, že bitová kopie pro obnovení dokáže tento počítač spustit nebo opravit.

### Aktivace Windows

Přečte aktuální stav licence Windows bez shromažďování úplných nebo částečných kódů Product Key, uchová anonymizovaná metadata událostí upozornění, chyb a kritických událostí Ochrany softwaru za posledních sedm dní a potom vrátí jeden ohraničený provozní závěr. Datová část událostí se před uložením důkazů odstraní. Historické záznamy zůstávají kontextem, když aktuální licencování hlásí stav Licencováno. Nevybírá se žádná akce aktivace, kódu Product Key, tokenu, licence, účtu, služby ani zásad. Stav zprostředkovatele a historie událostí neprokazují nárok na předplatné, dosažitelnost aktivačního serveru, integritu úložiště tokenů, budoucí platnost ani hlavní příčinu.

### Služby Windows

Inventarizuje názvy nainstalovaných služeb, zobrazované názvy, stavy, režimy spouštění a z hlediska soukromí ohraničené typy akcí obnovení bez shromažďování účtů, cest ke spustitelným souborům, příkazů obnovení nebo zpráv o restartu. Kontroluje soudržnost režimů spouštění, identity a cykly přímých závislostí, opakuje přesné čekající stavy, kontroluje podporované služby a závislosti zastavených služeb a potom porovná nedávná selhání Správce řízení služeb jen tehdy, když je přítomen jeden přesný název služby nebo jedinečný zobrazovaný název. Použití skupin pořadí načítání se počítá bez rozbalení členů a nejednoznačné identity zůstávají nevyřešené. Zastavené služby spouštěné na vyžádání mohou být normální. Žádná služba se nespouští a nastavení spouštění, závislostí a obnovení se zachovávají. Když konkrétní funkce potřebuje obnovení, použijte odpovídající postup v sekci Oprava.

### Chyby aplikací

Přečte nedávné chybové a kritické události z protokolu Aplikace spolu s ohraničenou historií Sledování spolehlivosti a potom vrátí jeden závěr o historii stability. Historické záznamy neprokazují aktuální závadu ani hlavní příčinu a pouze nasměrují užší diagnostiku. Žádné aplikace se nezastavují, neopravují, neodebírají ani nepřekonfigurují.

### Zobrazovací zařízení

Kontroluje grafické adaptéry a monitory z hlediska hlášených chyb zařízení, ID hardwaru a verzí nainstalovaných ovladačů. Bez instalace ovladačů, resetování zařízení a změny rozlišení. Kvalita obrazu a fyzická připojení se netestují.

### Vstupní zařízení

Kontroluje chyby klávesnic, myší a zařízení HID a inventář ovladačů. Nezaznamenávají se žádné stisky kláves, zařízení se neresetují a ovladače se nemění. Fyzická funkce vstupu se netestuje.

### Problémy se zařízeními

Najde nenulové kódy chyb konfigurace Plug and Play napříč třídami zařízení a potom porovná důkazy o přítomných zařízeních s ohraničenou historií SetupAPI týkající se instalace ovladačů, nastavení zařízení a Plug and Play. Samotné historické záznamy neurčují aktuální závadu. Zkontrolujte přesnou identitu zařízení a důkazy o schválených ovladačích bez změny hardwaru, ovladačů nebo konfigurace. Chybějící zařízení a fyzická funkce se neověřují.
