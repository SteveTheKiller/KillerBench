# Zabezpečení

Zabezpečení kontroluje Microsoft Defender, bránu firewall, certifikáty a přihlašovací služby. Zpřístupňuje také stav TPM a zabezpečení platformy, na kterém závisí BitLocker a Windows Hello.

Návody krok za krokem: [Šifrování jednotky nástrojem BitLocker](encrypt-with-bitlocker.html) a [Větší soukromí ve Windows](make-windows-private.html).

## Nástroje v této sekci

Kontrolujte ochranu, bránu firewall, certifikáty a přihlašovací služby.

### Microsoft Defender

Kontroluje ochranu Defenderu a stav signatur. Automatická oprava aktualizuje signatury jen tehdy, když Defender hlásí aktivní ochranu a zastaralé signatury. Používá nakonfigurované zdroje signatur. Bez změn ochrany, výjimek a zásad; bez kontroly malwaru.

### Centrum zabezpečení Windows

Kontroluje ohraničené registrace antivirových programů se surovým stavem zprostředkovatele, přesnou historii selhání služby a aktuální připravenost Centra zabezpečení Windows, aniž by interpretovalo stav ochrany. Prázdný inventář není verdiktem o stavu ochrany a počet registrací, surový stav ani historická selhání nemohou opravňovat k obnovení nebo ke změně zabezpečení. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující sledování stavu zabezpečení a oznámení mohou pokračovat. Konfigurace antivirového programu, registrace zprostředkovatelů, nastavení ochrany, výjimky, oznámení, zásady a typ spouštění se zachovávají. Nepožaduje se žádná kontrola, aktualizace ani změna ochrany. Ověření pokrývá pouze stabilní stav služby, nikoli přítomnost antivirového programu, stav ochrany, aktuálnost signatur, doručování oznámení, vlastnictví registrací ani účinné zásady.

### Služby brány firewall

Přečte z ActiveStore nastavení zapnutí profilů Doména, Soukromá a Veřejná a potom zkontroluje služby Modul základního filtrování a Brána Windows Defender Firewall. Volitelné obnovení spustí pouze povolené a zastavené služby po čerstvých kontrolách závislostí. Existující zásady filtrování se mohou projevit a přerušit síťový nebo vzdálený přístup. Typy spouštění, oprávnění služeb, pravidla brány firewall, profily a nastavení IPsec se zachovávají. Bez resetování, změn pravidel a zastavování služeb. Ověření potvrzuje pouze spuštěný stav služeb; účinná ochrana a připojení zůstávají neověřené.

### Vypršení certifikátů

Kontroluje data platnosti certifikátů a sestavuje místní řetězce důvěryhodnosti pouze z mezipaměti pro osobní úložiště počítače a aktuálního uživatele a potom vrátí jeden ohraničený provozní závěr. Odvolání, prezentace serverem, vazby, registrace, vlastnictví zásadami, vzdálená důvěryhodnost a použitelnost privátních klíčů zůstávají neověřené. Nemění se žádný certifikát, nastavení důvěryhodnosti, registrace, vazba, privátní klíč, aplikace, služba ani zásada.

### Správce přihlašovacích údajů

Kontroluje dostupnost služby Správce přihlašovacích údajů. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující aplikace mohou obnovit požadavky na uložené přihlašovací údaje. Obsah trezoru, hesla, přístupové klíče, certifikáty, tokeny, registrace přihlašovacích údajů, oprávnění, zásady, oprávnění služby a typ spouštění se zachovávají. Nepožaduje se žádný obsah přihlašovacích údajů ani operace s nimi. Ověření pokrývá pouze stabilní stav služby, nikoli načtení přihlašovacích údajů, ověřování ani přístup aplikací.

### Správce webových účtů

Kontroluje historii zprostředkovatele účtů zbavenou datové části a přesnou dostupnost Správce webových účtů. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující aplikace mohou podle aktuálních zásad obnovit požadavky na účty a tokeny. Historické události nemohou opravňovat k obnovení ani určovat aktuální stav účtu. Účty, přihlašovací údaje, tokeny, soubory cookie, registrace aplikací, oprávnění, stav přihlášení, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci s účtem, přihlašovacími údaji, tokenem, souborem cookie, oprávněním ani přihlášením. Ověření pokrývá pouze stabilní stav služby, nikoli ověřování, vydávání tokenů ani přístup aplikací.

### Pomocník pro přihlášení k účtu Microsoft

Kontroluje historii zprostředkovatele účtů zbavenou datové části a přesnou dostupnost Pomocníka pro přihlášení k účtu Microsoft. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující požadavky na přihlášení k účtu Microsoft mohou podle aktuálních zásad pokračovat. Historické události nemohou opravňovat k obnovení ani určovat aktuální stav účtu. Účty, přihlašovací údaje, tokeny, registrace zařízení, data aplikací, oprávnění, stav přihlášení, zásady, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci s účtem, přihlašovacími údaji, tokenem, registrací, oprávněním ani přihlášením. Ověření pokrývá pouze stabilní stav služby, nikoli ověřování, připojení účtu ani přístup aplikací.
