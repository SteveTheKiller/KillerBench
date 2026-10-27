# Síť

Síť kontroluje připojení, DNS, trasy, bezdrátové a kabelové přihlášení, sdílení souborů a vzdálený přístup. Nástroje, které se spojují s jiným hostitelem, používají cíl a časový limit, které nastavíte, a nic se nemění, dokud opravu nepotvrdíte.

Návody krok za krokem: [Řešení problémů se sítí a internetem](fix-network.html).

## Nástroje v této sekci

Kontrolujte připojení, DNS, trasy, bezdrátové sítě, sdílení a vzdálený přístup.

### Síť

Kontroluje adaptéry, stav a původ adres, brány, konfiguraci DNS, účinné zásady oboru názvů DNS, nastavení TCP, strukturu nativního katalogu protokolů a zprostředkovatelů oboru názvů Winsock, směrovací tabulku IP a mapování v souboru Hosts. Automatická oprava může spustit povolené služby Klient DNS, Klient DHCP, klient SMB, Rozpoznávání umístění v síti, Seznam sítí a Pomocná služba IP a obnovit připojené fyzické adaptéry DHCP, které nemají použitelnou adresu IPv4. Spuštění služeb identifikace sítě může znovu vypočítat identitu sítě a použít jiný existující profil brány firewall. Pomocná služba IP může aktivovat existující přechodové funkce IPv6, proxy portů a síťových oznámení. Obnovení služby ověřuje spuštěný stav, nikoli úspěšný překlad názvů, výběr trasy, identifikaci sítě ani provoz tunelů. Trasy, záznamy Hosts, servery DNS, profily, pravidla brány firewall, zprostředkovatelé Winsock, přechodová konfigurace, typy spouštění a zásady se zachovávají. Obnovení může změnit zapůjčené adresy a přerušit připojení. Statická nastavení a virtuální adaptéry se zachovávají. Přesné odpovědi brány na ICMP neověřují dosažitelnost internetu.

### Bezdrátové sítě

Kontroluje síťová zařízení a dostupnost Automatické konfigurace WLAN. Volitelné obnovení spustí pouze povolenou a zastavenou službu po kontrolách závislostí; uložené profily se mohou automaticky znovu připojit. Profily, přihlašovací údaje, zásady, nastavení rádia a typy spouštění se zachovávají. Bez resetování adaptéru a instalace ovladačů. Ověření pokrývá spuštěný stav, nikoli bezdrátové ověřování ani připojení.

### Kabelové ověřování (802.1X)

Kontroluje síťová zařízení a Automatickou konfiguraci kabelové sítě pro sítě vyžadující 802.1X. Volitelné obnovení spustí pouze povolenou a zastavenou službu po kontrolách závislostí. Existující profily ověřování se mohou aktivovat a změnit přístup k síti. Certifikáty, přihlašovací údaje, profily, zásady a nastavení spouštění se zachovávají. Ověření pokrývá pouze spuštěný stav, nikoli ověřování ani připojení.

### Síťová připojení

Kontroluje ohraničená aktuální přiřazení síťových profilů, přesnou historii selhání služby a aktuální připravenost služby Síťová připojení. Neúplné důkazy mají přednost. Veřejné profily a historická selhání zůstávají kontextem a nemohou opravňovat k obnovení ani ke změně sítě. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující rozhraní pro správu sítě mohou pokračovat. Adaptéry, adresy, trasy, nastavení DNS, profily připojení, přihlašovací údaje, konfigurace brány firewall, zásady a typ spouštění se zachovávají. Nemění se žádné připojení ani nastavení sítě. Ověření pokrývá pouze stabilní stav služby, nikoli provoz adaptérů, přidělování adres, směrování, překlad názvů ani připojení.

### Automatický proxy server WinHTTP

Kontroluje uložené výchozí hodnoty WinHTTP, internetová nastavení aktuálního účtu, přesnou historii selhání služby a aktuální připravenost Automatického proxy serveru WinHTTP. Neúplné důkazy mají přednost. Konfigurace proxy ani historická selhání nemohou opravňovat k obnovení služby ani ke zjišťování. Zastavená služba spouštěná na vyžádání může být normální. Když je vybrán přesně tento nástroj, volitelné obnovení může po čerstvých kontrolách závislostí spustit pouze zastavenou povolenou službu. Existující aplikace mohou podle aktuálních nastavení obnovit zjišťování WPAD nebo PAC. Definice proxy, adresy URL souborů PAC, přihlašovací údaje, nastavení sítě, konfigurace brány firewall, zásady, oprávnění služby a typ spouštění se zachovávají. Nestahuje se žádná adresa URL a nemění se žádné nastavení proxy ani sítě. Ověření pokrývá pouze stabilní stav služby, nikoli zjišťování, dosažitelnost, ověřování, vlastnictví zásad ani použití aplikacemi.

### Správce připojení systému Windows

Kontroluje ohraničená aktuální přiřazení síťových profilů, přesnou historii selhání služby a aktuální připravenost Správce připojení systému Windows. Neúplné důkazy mají přednost. Veřejné profily a historická selhání zůstávají kontextem a nemohou opravňovat k obnovení ani k rozhodnutí o připojení. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Windows může podle existujících profilů a zásad obnovit automatická rozhodnutí o připojení. Adaptéry, profily, přihlašovací údaje, nastavení měření, nákladů, roamingu, brány firewall, VPN a ověřování a typ spouštění se zachovávají. Nemění se žádný profil, adaptér ani nastavení. Ověření pokrývá pouze stabilní stav služby, nikoli použitelnost profilů, výběr připojení, ověřování, směrování, překlad názvů ani připojení.

### Služba nastavení sítě

Kontroluje ohraničené aktuální vazby protokolů adaptérů, přesnou historii selhání služby a aktuální připravenost Služby nastavení sítě. Neúplné důkazy mají přednost. Stav vazeb ani historická selhání nemohou opravňovat k obnovení služby ani ke změně nastavení. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující čekající nastavení sítě může podle zásad Windows pokračovat. Adaptéry, ovladače, balíčky ovladačů, vazby, adresy, trasy, nastavení DNS, profily, přihlašovací údaje, konfigurace brány firewall, zásady a typ spouštění se zachovávají. KillerBench nevybírá žádnou změnu ovladače ani konfigurace. Ověření pokrývá pouze stabilní stav služby, nikoli dostupnost adaptérů, vhodnost ovladačů, dokončení nastavení, přidělování adres, směrování, překlad názvů ani připojení.

### Sdílení souborů (SMB)

Kontroluje dostupnost serveru SMB systému Windows. Volitelné obnovení spustí pouze povolenou a zastavenou službu Server po čerstvých kontrolách závislostí. Existující sdílené složky a tiskárny se mohou stát dosažitelnými podle svých aktuálních oprávnění a zásad brány firewall. Nemění se žádná sdílená složka, oprávnění, nastavení SMB, pravidlo brány firewall, přihlašovací údaj, zásada ani typ spouštění. Ověření pokrývá pouze stabilní stav služby, nikoli přístup klientů ani přenos souborů.

### Správce připojení vzdáleného přístupu

Kontroluje ohraničenou konfiguraci profilů VPN aktuálního účtu, přesnou historii selhání služby a aktuální připravenost Správce připojení vzdáleného přístupu bez shromažďování tajných údajů. Prázdný inventář může být záměrný a přítomnost profilů, popisky připojení ani historická selhání nemohou opravňovat k obnovení ani k pokusu o připojení. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující požadavky na vytáčené připojení a VPN mohou pokračovat. Profily připojení, přihlašovací údaje, trasy, adaptéry, nastavení ověřování, zásady a typ spouštění se zachovávají. Nezahajuje se žádné připojení. Ověření pokrývá pouze stabilní stav služby, nikoli použitelnost profilů, dosažitelnost serveru, ověřování, vyjednání tunelu, instalaci tras, překlad názvů ani připojení.

### Služba Vzdálená plocha

Kontroluje místní povolení Vzdálené plochy, ověřování na úrovni sítě a port naslouchání, ohraničenou historii připojení a relací, přesnou historii selhání služby a aktuální připravenost služby. Neúplné důkazy mají přednost. Historické záznamy nemohou opravňovat k obnovení služby ani ke změnám konfigurace. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující naslouchací proces se může stát dosažitelným podle aktuálních zásad, NLA, pravidel brány firewall a oprávnění. Povolení Vzdálené plochy, naslouchací procesy, certifikáty, porty, pravidla brány firewall, NLA, oprávnění, relace, zásady, oprávnění služby a typ spouštění se zachovávají. Nevytváří ani nemění se žádný naslouchací proces, pravidlo brány firewall, přístup účtu, certifikát ani relace. Ověření pokrývá pouze stabilní stav služby, nikoli účinné zásady, vazbu naslouchacího procesu, přístup přes bránu firewall, identitu certifikátu, oprávnění, ověřování, dosažitelnost sítě, úspěšnost připojení ani vytvoření relace.

### Vzdálená správa systému Windows

Kontroluje ohraničenou konfiguraci naslouchacích procesů WinRM, provozní historii, přesnou historii selhání služby a aktuální připravenost služby. Neúplné důkazy mají přednost. Zakázané naslouchací procesy nebo aktuální problémy se službou vyžadují kontrolu, zatímco historické záznamy nemohou opravňovat k obnovení ani ke změnám konfigurace. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující naslouchací procesy WS-Management se mohou stát dosažitelnými podle aktuálních nastavení ověřování, certifikátů, brány firewall, TrustedHosts a zásad. Naslouchací procesy, adresy, porty, certifikáty, ověřování, šifrování, TrustedHosts, pravidla brány firewall, oprávnění, kvóty, zásady, oprávnění služby a typ spouštění se zachovávají. Nevytváří ani nemění se žádný naslouchací proces, pravidlo brány firewall, položka důvěryhodnosti, certifikát, přihlašovací údaj ani vzdálená relace. Ověření pokrývá pouze stabilní stav služby, nikoli účinné zásady, vazbu naslouchacích procesů, přístup přes bránu firewall, platnost certifikátů, ověřování, oprávnění, dosažitelnost pro klienty ani vzdálené spouštění příkazů.
