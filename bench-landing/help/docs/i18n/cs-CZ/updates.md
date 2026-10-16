# Aktualizace

Aktualizace pokrývá Windows Update, služby za ním a úložiště komponent. Uvidíte, co je nainstalováno, co čeká a proč aktualizace selhává, a dostupné aktualizace můžete zkontrolovat dříve, než některé vyberete k instalaci.

Návody krok za krokem: [Oprava služby Windows Update](fix-windows-update.html) a [Oprava systémových souborů](repair-system-files.html).

## Nástroje v této sekci

Sledujte aktivitu údržby, kontrolujte dostupné aktualizace a opravujte řetězec Windows Update.

### Monitor aktivity údržby

Sleduje ohraničenou aktivitu údržby Windows Update a Setup v jednom nebo více snímcích. Možnosti určují časování obnovování a to, zda se zahrnou události aktualizací, historie aktualizací, přesná naplánovaná úloha Windows Update, přesné stavy služeb, indikátory restartu, přenosy na pozadí aktuálního účtu, aktivita Optimalizace doručení a z hlediska soukromí ohraničená aktivita Windows Installeru. ID procesů, příkazové řádky, uživatelé a cesty jsou vyloučeny. Aktivita neprokazuje průběh, dokončení, zaseknutí ani závadu. Nepožaduje se žádná akce aktualizace, instalace, přenosu, úlohy, služby, procesu, zásad, restartu ani opravy.

### Dostupné aktualizace

Vyhledává vybrané aktualizace softwaru a ovladačů pomocí nakonfigurovaného zdroje, Windows Update nebo již zaregistrované služby Microsoft Update. Připravenost zdroje se kontroluje před vyhledáváním. Bez instalace, přijetí licence, registrace zdroje a změn zásad. Je to oddělené od oprav údržby.

### Windows Update

Ohraničeně inventarizuje stavy balíčků údržby, volitelných funkcí a možností Windows, kontroluje indikátory restartu, volné místo a služby aktualizací, sleduje Optimalizaci doručení, aniž by ji spouštěl, a potom prověří úložiště komponent a chráněné systémové soubory. Čekající stavy balíčků, funkcí nebo možností vyžadují kontrolu, ale neopravňují k žádné akci s komponentami. Automatická oprava může spustit povolené služby Windows Update, BITS, Kryptografické služby, Instalační služba modulů Windows a Update Orchestrator. Spuštění Update Orchestrator může podle aktuálních zásad obnovit existující naplánovanou aktivitu aktualizací. Balíčky, volitelné funkce, možnosti, mezipaměť Optimalizace doručení, partnerské uzly, šířka pásma, zásady, registrace zdroje, plány a nastavení spouštění se zachovávají. DISM nebo SFC vyžadují živé důkazy o poškození. Tento postup nevybírá ani neschvaluje žádnou aktualizaci.

### Služba Windows Update

Kontroluje přesný stav Služby Windows Update, její zaregistrovanou knihovnu DLL služby, ohraničenou aktuální aktivitu aktualizací a přesnou historii selhání služby. Pozorování knihovny DLL, aktivita aktualizací ani historická selhání nemohou opravňovat ke spuštění služby nebo k operaci aktualizace. Volitelné obnovení spustí pouze zastavenou automatickou službu po čerstvých kontrolách závislostí a registrace spustitelného souboru. Zastavená služba spouštěná na vyžádání zůstává pouze informací. Existující aktivita aktualizací může podle aktuálních zásad pokračovat. Zásady aktualizací, registrace zdroje, plány, nastavení restartu, balíčky, úlohy BITS, oprávnění služby a typ spouštění se zachovávají. KillerBench žádnou aktualizaci nevybírá, neschvaluje ani neinstaluje. Ověření pokrývá pouze stabilní stav služby, nikoli dosažitelnost zdroje, použitelnost, stažení, instalaci, dokončení ani připravenost k restartu.

### Optimalizace doručení

Kontroluje přesný stav služby Optimalizace doručení. Zastavená služba spouštěná na vyžádání zůstává pouze informací. Obnovení může nabídnout jen přesný stav Pozastaveno. Volitelné obnovení zopakuje kontrolu stavu, odešle jeden ohraničený požadavek na pokračování a vyžaduje dvě souhlasná pozorování stavu Spuštěno. Typ spouštění, obsah mezipaměti, partnerské uzly, šířka pásma, zdroj aktualizací a spravované zásady se zachovávají. Aktivita přenosů, stav mezipaměti, průběh a dokončení aktualizací zůstávají neověřené.

### Služba BITS

Kontroluje přesný stav Služby inteligentního přenosu na pozadí, přenosové úlohy aktuálního účtu a ohraničenou přesnou historii selhání služby. Historická selhání a snímky přenosů nemohou opravňovat ke spuštění služby. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující aplikace mohou obnovit přenosy na pozadí ve frontě. Úlohy, priority, vlastníci, cíle, nastavení proxy, zásady a typ spouštění se zachovávají. Ověření pokrývá pouze stabilní stav služby, nikoli průběh přenosu, vlastnictví aplikací ani dokončení.

### Přenosy na pozadí (BITS)

Čte přenosové úlohy BITS ve vybraném rozsahu účtů a kontroluje přesné identity úloh. Názvy front neurčují, které aplikaci úloha patří. Žádné přenosy se neobnovují, nedokončují, neruší, nemažou ani nepřiřazují znovu.

### Kryptografické služby

Kontroluje přesný stav Kryptografických služeb, jejich zaregistrovanou knihovnu DLL služby a ohraničenou přesnou historii selhání služby. Historická selhání a pozorování knihovny DLL nemohou opravňovat ke spuštění služby ani k výměně komponent. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující operace s katalogy, certifikáty a aktualizacemi mohou pokračovat. Certifikáty, klíče, katalogy, nastavení důvěryhodnosti, zásady a typ spouštění se zachovávají. Ověření pokrývá pouze stabilní stav služby, nikoli ověření certifikátů, podpisů, katalogů ani důvěryhodnosti.

### Instalační služba modulů Windows

Kontroluje přesný stav Instalační služby modulů Windows, ohraničené přechody balíčků údržby a přesnou historii selhání služby. Historická selhání a stavy balíčků nemohou opravňovat ke spuštění služby ani k operaci s balíčky. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující aktivita údržby může pokračovat. Balíčky, obsah úložiště komponent, zdroje, zásady a typ spouštění se zachovávají. Ověření pokrývá pouze stabilní stav služby, nikoli stav úložiště komponent, použitelnost balíčků, dokončení údržby ani připravenost k restartu.

### Služba Windows Update Medic

Kontroluje přesný stav Služby Windows Update Medic, její zaregistrovanou knihovnu DLL služby, ohraničenou aktuální aktivitu aktualizací a přesnou historii selhání služby. Pozorování knihovny DLL, aktivita aktualizací ani historická selhání nemohou opravňovat ke spuštění služby ani k akci s komponentami. Zastavená služba spouštěná na vyžádání může být normální. Když je vybrán přesně tento nástroj, volitelné obnovení může po čerstvých kontrolách závislostí spustit pouze zastavenou povolenou službu. Náprava ze strany Windows může podle aktuálních zásad pokračovat a může opravit komponenty aktualizací nebo vyvolat aktivitu aktualizací. Zásady aktualizací, registrace zdroje, plány, balíčky, úlohy BITS, nastavení restartu, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou aktualizaci a nemění žádné nastavení aktualizací. Ověření pokrývá pouze stabilní stav služby, nikoli nápravu, stav komponent, použitelnost, stažení, instalaci ani připravenost k restartu.

### Služba Update Orchestrator

Kontroluje přesný stav Služby Update Orchestrator, přesnou vestavěnou úlohu Windows Update, ohraničenou aktuální aktivitu a přesnou historii selhání služby. Výsledky úloh, aktivita aktualizací ani historická selhání nemohou opravňovat ke spuštění služby nebo k operaci aktualizace. Volitelné obnovení spustí pouze zastavenou automatickou službu po čerstvých kontrolách závislostí a registrace spustitelného souboru. Zastavená služba spouštěná na vyžádání zůstává pouze informací. Existující naplánovaná aktivita aktualizací může podle aktuálních zásad pokračovat a může stáhnout nebo nainstalovat již schválené aktualizace. Zásady aktualizací, registrace zdroje, plány, nastavení restartu, balíčky, úlohy BITS, oprávnění služby a typ spouštění se zachovávají. KillerBench žádnou aktualizaci nevybírá ani neschvaluje. Ověření pokrývá pouze stabilní stav služby, nikoli použitelnost, stažení, instalaci, dokončení ani připravenost k restartu.

### Vyčištění úložiště komponent

Analyzuje, zda Windows doporučuje vyčištění úložiště komponent. Volitelná údržba okamžitě odebere nahrazené komponenty a potom provede analýzu znovu. Bez ResetBase a bez ručního mazání WinSxS. Bez automatického vrácení zpět; uvolněné místo na disku se neměří.
