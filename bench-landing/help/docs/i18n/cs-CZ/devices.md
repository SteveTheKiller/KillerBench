# Zařízení

Zařízení pokrývá tiskárny, zvuk, kamery, Bluetooth, vstupní zařízení a služby Windows, na kterých závisí připojený hardware.

Návody krok za krokem: [Řešení problémů s tiskem](fix-printing.html) a [Zkoumání pádů a modrých obrazovek](investigate-crashes.html).

## Nástroje v této sekci

Tisk, zvuk, kamery, Bluetooth, vstupní zařízení a služby, které stojí za připojeným hardwarem.

### Tisk

Kontroluje službu Zařazování tisku, pozastavení místních front, viditelné tiskové úlohy, nastavení offline a stavy hlášené ovladači a potom inventarizuje registrace front, ovladačů, portů, standardních koncových bodů TCP/IP, tiskových procesorů a monitorů tisku. Nedávné problémy PrintService porovnává jen tehdy, když je přítomna jedna přesná identita aktuální fronty. Přiřazené standardní koncové body TCP/IP dostanou jeden ohraničený pokus o připojení bez dat aplikací, SNMP a tiskové úlohy. Automatická oprava může spustit povolenou službu zařazování, obnovit pouze přesně určené pozastavené fronty s čerstvými důkazy o čekajících úlohách a zrušit režim offline. Záměrná pozastavení bez úloh zůstávají pouze ke kontrole. Vzdálená připojení, řetězce komunity SNMP, registrace rozšíření a obsah úloh se zachovávají. Stavový kód ani úspěšné připojení neurčují identitu tiskárny, spotřební materiál, mechaniku, přijetí do fronty, úspěšné vykreslení ani fyzický tisk.

### Zvuk

Kontroluje identity a sdílené formáty výchozího multimediálního vstupu a výstupu, stav zvukových zařízení a ovladačů, službu Tvůrce koncových bodů zvuku systému Windows a službu Zvuk systému Windows. Automatická oprava spustí zastavené povolené služby a ověří spuštěný stav. Nastavení spouštění, ovladače, vybraná zařízení, výhradní režim a úrovně hlasitosti se zachovávají. Přehrávání a nahrávání zvuku a směrování aplikací zůstávají neotestované.

### Podpora kamer

Kontroluje chyby kamer a zobrazovacích zařízení, ID hardwaru, důkazy o nainstalovaných ovladačích a dostupnost Serveru snímků kamery Windows. Volitelné obnovení spustí pouze povolenou a zastavenou službu po kontrolách závislostí. Aplikace, která již žádá o přístup ke kameře, může pokračovat se stávajícími oprávněními. Nastavení soukromí kamery, oprávnění, konfigurace zařízení, zásady, ovladače a typ spouštění se zachovávají. Bez resetování, změny oprávnění a instalace ovladačů. Ověření pokrývá pouze hlášenou konfiguraci zařízení a spuštěný stav služby, nikoli pořízení snímku, přístup podle nastavení soukromí ani fyzickou funkci.

### Podpora Bluetooth

Kontroluje chyby zařízení Bluetooth, ID hardwaru, důkazy o nainstalovaných ovladačích a dostupnost Služby podpory Bluetooth. Volitelné obnovení spustí pouze povolenou a zastavenou službu po kontrolách závislostí; spárovaná zařízení se mohou znovu připojit. Párování, nastavení rádia, konfigurace zařízení, zásady a typ spouštění se zachovávají. Bez resetování, odebrání zařízení a instalace ovladačů. Ověření pokrývá pouze hlášenou konfiguraci zařízení a spuštěný stav služby, nikoli zjišťování, párování, zvuk ani přenos dat.

### Podpora čipových karet

Kontroluje chyby čteček čipových karet, ID hardwaru, důkazy o nainstalovaných ovladačích a dostupnost služby Čipová karta. Volitelné obnovení spustí pouze povolenou a zastavenou službu po kontrolách závislostí. Aplikace mohou podle stávajících zásad obnovit rozpoznávání vložených karet. Certifikáty, kódy PIN, přihlašovací údaje, konfigurace čteček a karet, zásady, ovladače a typ spouštění se zachovávají. Bez inicializace, registrace, operací s kódem PIN, resetování čtečky a instalace ovladačů. Ověření pokrývá pouze hlášenou konfiguraci čtečky a spuštěný stav služby, nikoli rozpoznání karty, ověřování ani přístup aplikací.

### Podpora biometriky

Kontroluje chyby biometrických zařízení, ID hardwaru, důkazy o nainstalovaných ovladačích a dostupnost Biometrické služby systému Windows. Volitelné obnovení spustí pouze povolenou a zastavenou službu po kontrolách závislostí. Existující požadavky Windows Hello a aplikací mohou podle aktuálních zásad pokračovat. Biometrické šablony, data Windows Hello, přihlašovací údaje, konfigurace zařízení, oprávnění, zásady, ovladače a typ spouštění se zachovávají. Bez registrace, odstranění šablon, resetování snímače, změny oprávnění a instalace ovladačů. Ověření pokrývá pouze hlášenou konfiguraci snímače a spuštěný stav služby, nikoli snímání, porovnávání, přihlášení ani přístup aplikací.

### Služba zařízení HID

Kontroluje dostupnost Služby zařízení HID. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující rychlá tlačítka, ovládání médií a další funkce HID mohou pokračovat. Zařízení, ovladače, mapování tlačítek, nastavení usnadnění přístupu, nastavení vstupu, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci se zařízeními, ovladači, mapováním ani nastavením vstupu. Ověření pokrývá pouze stabilní stav služby, nikoli chování tlačítek, ovládacích prvků ani aplikací.

### Služba výčtu přenosných zařízení

Kontroluje dostupnost Služby výčtu přenosných zařízení. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující aplikace mohou obnovit rozpoznávání připojených přenosných zařízení a přístup k nim. Zařízení, ovladače, soubory, knihovny, nastavení synchronizace, oprávnění, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci se zařízeními, soubory, knihovnami, synchronizací ani přenosem. Ověření pokrývá pouze stabilní stav služby, nikoli výčet, přístup, synchronizaci ani přenos.

### Získávání obrázků systému Windows (WIA)

Kontroluje dostupnost služby Získávání obrázků systému Windows. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující požadavky skenerů a pořizování statických obrázků mohou pokračovat podle aktuálních oprávnění aplikací. Zařízení, ovladače, obrazová data, oprávnění aplikací, nastavení soukromí, nastavení přenosu, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci se zařízeními, ovladači, obrázky, oprávněními, přenosem ani pořizováním. Ověření pokrývá pouze stabilní stav služby, nikoli funkci skeneru, kamery, přenosu ani aplikací.

### Rozpoznávání hardwaru prostředí

Kontroluje ohraničené důkazy Plug and Play o řadičích úložiště, discích a přenosných zařízeních a odkazy na nainstalované ovladače a potom zkontroluje dostupnost služby Rozpoznávání hardwaru prostředí. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující oznámení Automatického přehrávání a připojení hardwaru mohou podle aktuálních zásad pokračovat. Pozorování zařízení a uchovaná historie nemohou opravňovat k obnovení. Zařízení, ovladače, svazky, body připojení, nastavení Automatického přehrávání, obslužné rutiny, zásady, oprávnění služby a typ spouštění se zachovávají. Nepožaduje se žádná operace se zařízením ani nastavením. Ověření pokrývá pouze stabilní stav služby, nikoli doručení oznámení, připravenost médií, přístup ke svazkům ani fyzickou funkci zařízení.

### Služba instalace zařízení

Kontroluje dostupnost Služby instalace zařízení. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující instalace zařízení vyžádaná systémem Windows může podle aktuálních zásad pokračovat. Zařízení, ovladače, balíčky, konfigurace, vazby, nastavení tříd, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nic nevybírá ani neinstaluje. Ověření pokrývá pouze stabilní stav služby, nikoli instalaci ani funkci zařízení.

### Služba přidružení zařízení

Kontroluje nedávnou historii nastavení zařízení a Plug and Play a potom dostupnost Služby přidružení zařízení. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující přidružování zařízení vyžádané systémem Windows může pokračovat. Spárovaná a přidružená zařízení, ovladače, přihlašovací údaje, metadata, oprávnění, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci se zařízením, párováním, odebráním, resetováním ani ovladačem. Ověření pokrývá pouze stabilní stav služby, nikoli zjišťování, párování, přidružení ani funkci zařízení.

### Služba vylepšení zobrazení

Kontroluje důkazy o grafických adaptérech a monitorech a potom dostupnost Služby vylepšení zobrazení. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující požadavky na jas, barvy a vylepšení mohou pokračovat. Konfigurace zobrazení, jas, barevné profily, nastavení HDR, zařízení, ovladače, nastavení napájení, zásady, oprávnění služby a typ spouštění se zachovávají. Nemění se žádné nastavení zobrazení. Ověření pokrývá pouze stabilní stav služby, nikoli chování vylepšení, kvalitu obrazu ani fyzickou funkci displeje.

### Služba správy zadávání textu

Kontroluje důkazy o klávesnicích, myších a zařízeních HID a potom dostupnost Služby správy zadávání textu. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující požadavky dotykové klávesnice, rukopisu, emoji a pokročilého zadávání textu mohou pokračovat. Slovníky, data rukopisu, nastavení jazyka a klávesnice, nastavení usnadnění přístupu a soukromí, zařízení, ovladače, zásady, oprávnění služby a typ spouštění se zachovávají. Neshromažďuje se žádný zadaný text, stisky kláves ani obsah rukopisu. Ověření pokrývá pouze stabilní stav služby, nikoli zadávání textu, rozpoznávání ani funkci zařízení.

### Služba mezipaměti písem systému Windows

Kontroluje dostupnost Služby mezipaměti písem systému Windows. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující aplikace mohou znovu používat aktuální mezipaměť písem. Nainstalovaná písma, soubory písem, data aplikací, soubory mezipaměti, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench neinstaluje ani neodebírá žádná písma a mezipaměť nemaže ani znovu nesestavuje. Ověření pokrývá pouze stabilní stav služby, nikoli vykreslování textu ani chování aplikací.

### Hostitel zprostředkovatele zjišťování funkcí

Kontroluje dostupnost Hostitele zprostředkovatele zjišťování funkcí. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující aplikace mohou obnovit zjišťování síťových zařízení prostřednictvím nainstalovaných zprostředkovatelů. Síťové profily, sdílené složky, zařízení, zprostředkovatelé, pravidla brány firewall, nastavení zjišťování, přihlašovací údaje, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci se zařízeními, sdílenými složkami, zprostředkovateli, pravidly brány firewall ani nastavením zjišťování. Ověření pokrývá pouze stabilní stav služby, nikoli stav zprostředkovatelů, zjišťování zařízení ani připojení.

### Publikování prostředků zjišťování funkcí

Kontroluje dostupnost Publikování prostředků zjišťování funkcí. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Tento počítač se může stát zjistitelným prostřednictvím stávajících nastavení zjišťování sítě a brány firewall. Síťové profily, sdílené složky, publikované prostředky, pravidla brány firewall, nastavení zjišťování, přihlašovací údaje, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci se sdílenými složkami, prostředky, pravidly brány firewall ani nastavením zjišťování. Ověření pokrývá pouze stabilní stav služby, nikoli publikování, viditelnost, přístup ani připojení.
