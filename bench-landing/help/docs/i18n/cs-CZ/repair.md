# Oprava

Oprava shromažďuje nástroje, které umějí něco opravit. Každý z nich nejprve diagnostikuje. Oprava je nabídnuta jen tehdy, když čerstvé důkazy ukazují přesně ten problém, který odstraňuje, a spustí se teprve poté, co zapnete **Automaticky opravit potvrzené problémy** nebo sami potvrdíte jednotlivou opravu. Každá oprava se zapisuje do deníku a poté ověřuje.

Návody krok za krokem: [Oprava systémových souborů](repair-system-files.html) a [Oprava služby Windows Update](fix-windows-update.html).

## Automatická a ruční oprava

**Automaticky opravit potvrzené problémy** umožní diagnostickému nástroji po kontrole spustit opravy, jejichž potřebu prokáže. Ve výchozím stavu je vypnuté.

Pro jednotlivou opravu údržby otevřete **Pokročilé: protokoly a ruční opravy** a zvolte **Opravy**. Vyberte **Úložiště komponent (DISM)** nebo **Systémové soubory (SFC)**, přečtěte si náhled, zaškrtněte **Spustit tuto opravu na tomto počítači** a stiskněte **Spustit opravu**. Co se děje před každou změnou a po ní, popisuje stránka [Jak změny zůstávají bezpečné](safety.html).

## Nástroje v této sekci

Použijte je poté, co diagnostika zjistí problém. Opravy vyžadují potvrzené důkazy a výslovné schválení.

### Základní služby Windows

Společně kontroluje služby Napájení, Plug and Play, Správce uživatelů, Klient zásad skupiny, Rozhraní síťového úložiště, Mapovač koncových bodů RPC, Vzdálené volání procedur (RPC), Spouštěč procesů serveru DCOM, Infrastruktura úloh na pozadí a CoreMessaging. Volitelné obnovení spustí pouze přesně ty povolené automatické služby, u kterých čerstvé důkazy potvrdí, že jsou po okně spuštění a úplných kontrolách závislostí zastavené. Služby spouštěné na vyžádání zůstávají v nečinnosti beze změny. Schémata napájení, zařízení, ovladače, účty, přihlašovací údaje, profily, zásady, adaptéry, adresy, trasy, DNS, konfigurace brány firewall, registrace, koncové body, registrace úloh, data aplikací, oprávnění služeb a typy spouštění se zachovávají. Nepožaduje se žádná aktualizace zásad, prohledání zařízení, akce s účtem, změna sítě, změna nastavení napájení, změna registrace ani spuštění úlohy. Ověření pokrývá pouze stabilní stav služeb, nikoli úplné fungování funkcí.

### Systémové soubory

Ohraničeně inventarizuje stavy balíčků údržby, volitelných funkcí a možností Windows, kontroluje předpoklady, prověří chráněné systémové soubory a SFC automaticky zvolí jen tehdy, když živá kontrola potvrdí poškození. Čekající stavy balíčků, funkcí nebo možností vyžadují kontrolu, ale neopravňují k instalaci, odebrání, vyčištění, vrácení zpět, povolení ani zakázání.

### Úložiště WMI

Kontroluje dostupnost služby WMI a potom konzistenci úložiště. Automatická oprava může spustit způsobilou zastavenou službu a zachránit potvrzené poškození a potom každý výsledek ověřit. Nastavení zakázané služby se zachovávají. Čitelné registrace se sloučí; nečitelný obsah může vyžadovat přeinstalaci zprostředkovatele. Klienti správy mohou být přerušeni. Bez resetování, mazání a automatického vrácení zpět.

### Systémová oznámení Windows

Přečte nedávnou historii Správce řízení služeb a potom společně zkontroluje Systém událostí modelu COM+ a Službu oznámení systémových událostí. Volitelné obnovení před spuštěním SENS ověří připravenost Systému událostí modelu COM+. Existující oznámení o přihlášení, síti, napájení a COM+ mohou podle aktuální konfigurace pokračovat. Katalogy COM+, odběry, registrace oznámení, odběratelé, účty, oprávnění, nastavení obnovení, zásady a typy spouštění se zachovávají. Nemění se žádný odběr, registrace, odběratel, účet, oprávnění ani zásada. Ověření pokrývá pouze stabilní stav služeb, nikoli úplné doručování oznámení.

### Služba zásad diagnostiky

Kontroluje ohraničenou provozní historii Zásad diagnostiky, přesnou historii selhání služby a aktuální připravenost služby. Neúplné důkazy mají přednost. Historické záznamy nemohou opravňovat k obnovení služby ani k akci řešení potíží. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující diagnostické scénáře mohou pokračovat. Zásady, data řešení potíží a typ spouštění se zachovávají. Nespouští se žádné poradce při potížích ani navrhovaná oprava. Ověření pokrývá pouze stabilní stav služby, nikoli zjištění závady, dokončení diagnostiky ani výsledek opravy.

### Zasílání zpráv o chybách Windows

Kontroluje konfiguraci výpisů paměti při chybě, ohraničená metadata souborů výpisů, nedávné zprávy o pádech systému, přesnou historii selhání služby Zasílání zpráv o chybách Windows a aktuální připravenost služby. Historické zprávy neurčují aktuální závadu ani hlavní příčinu a nemohou opravňovat k obnovení. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující zprávy ve frontě mohou podle aktuálních zásad pokračovat. Archivy zpráv, výpisy, souhlas, zásady odesílání a typ spouštění se zachovávají. Nepožaduje se žádné odeslání ani odstranění. Ověření pokrývá pouze stabilní stav služby, nikoli vytváření, odesílání ani doručení zpráv, vyřešení problému ani diagnostiku pádu.

### Kolektor událostí systému Windows

Kontroluje ohraničený inventář odběrů Kolektoru událostí systému Windows, přesnou historii selhání služby a aktuální připravenost služby. Prázdný inventář může být záměrný a počet odběrů ani historická selhání nemohou opravňovat k obnovení. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující odběry mohou obnovit vzdálené shromažďování událostí. Odběry, předávané události, přihlašovací údaje, nastavení kanálů, zásady a typ spouštění se zachovávají. Ověření pokrývá pouze stabilní stav služby, nikoli dosažitelnost zdrojů, ověřování, vzdálené doručování, aktuálnost událostí ani stav odběrů.

### Protokol událostí Windows

Kontroluje u čtyř diagnostických kanálů stav povolení, režim uchovávání, aktuální velikost, nastavený limit, počet záznamů a zaplnění a potom zkontroluje dostupnost služby Protokol událostí Windows. Automatická oprava může po kontrole závislostí spustit zastavenou povolenou službu a ověřit spuštěný stav. Samostatně potvrzovaná cílená akce může povolit jeden přesně určený zakázaný a nespravovaný diagnostický kanál a zachovat přitom všechna ostatní hlášená nastavení. Protokoly, oprávnění kanálů, uchovávání, limity velikosti, typy spouštění a spravovaná nastavení se zachovávají. Úspěšné doručování událostí a kapacita archivu zůstávají neověřené.

### Služba Čas systému Windows

Kontroluje konfiguraci, službu a synchronizaci Času systému Windows. Automatická oprava může spustit W32Time, když aktuální stav hlásí přesně chybu zastavené služby a projdou čerstvé kontroly zásad, režimu a závislostí, a potom požádat o novou synchronizaci a ověřit nový stav. Tím se mohou upravit hodiny pomocí již nakonfigurovaných zdrojů. Typ spouštění, konfigurace zdrojů a spravovaná nastavení se zachovávají.

### Služba Plánovač úloh

Ohraničeně inventarizuje stav naplánovaných úloh, výsledky spuštění a počty zmeškaných spuštění a potom je spojí s přesnou historií selhání služby Plánovač úloh, než zkontroluje aktuální dostupnost služby. Zjištění o úlohách ani historická selhání služby nemohou opravňovat ke spuštění služby. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující povolené úlohy se mohou spustit pod nakonfigurovanými účty, včetně zmeškaných aktivačních událostí. Nemění se žádná definice úlohy, objekt zabezpečení, odkaz na přihlašovací údaje, zásada ani typ spouštění a žádná úloha se přímo nespouští ani neodstraňuje. Ověření pokrývá pouze stabilní stav služby, nikoli úspěšnost akcí, platnost přihlašovacích údajů ani budoucí výsledky úloh.

### Služba uživatelských profilů

Kontroluje dostupnost Služby uživatelských profilů. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující požadavky na přihlášení a profily mohou pokračovat. Složky profilů, podregistry, oprávnění, registrace v seznamu profilů, přihlašovací údaje, uživatelská data, zásady, oprávnění služby a typ spouštění se zachovávají. Nepožaduje se žádná operace s profilem. Ověření pokrývá pouze stabilní stav služby, nikoli přihlášení, načtení profilu ani přístup k uživatelským datům.

### Služby zálohování

Kontroluje služby Stínová kopie svazku, Zprostředkovatel softwarových stínových kopií Microsoft, Zálohování Windows a Modul zálohování na úrovni bloků a potom přečte stav zapisovačů VSS, zaregistrované typy zprostředkovatelů, aktuální snímky a přidělení úložiště stínových kopií. Zastavené služby spouštěné na vyžádání mohou být v nečinnosti normální. Volitelné obnovení spustí pouze zastavené automatické služby po kontrolách závislostí a potom ověří spuštěný stav. Existující naplánované nebo přerušené zálohování může pokračovat. Nastavení zakázaného a ručního spouštění, snímky, úložiště stínových kopií, registrace zprostředkovatelů, plány zálohování, katalogy, rozvrhy, cíle, přihlašovací údaje a uchovávání se zachovávají. Žádné zálohování se nevybírá ani nepřekonfiguruje, žádná služba se nerestartuje a žádný snímek ani katalog se neodstraňuje. Registrace zprostředkovatelů, stav zapisovačů, přidělení úložiště ani stabilní stav služeb neověřují dokončení zálohy, přístup k cíli ani úspěšnost obnovení.
