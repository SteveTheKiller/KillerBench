# Úložiště

Kontrolujte disky, svazky, Prostory úložiště a služby údržby jednotek. Diagnostika nevybírá žádnou operaci s diskem; každá podporovaná akce, která mění počítač, potřebuje vlastní přesný cíl, předpoklady a potvrzení.

Návody krok za krokem: [Kontrola a oprava jednotky](check-a-drive.html) a [Uvolnění místa na disku](free-disk-space.html).

## Nástroje v této sekci

### Úložiště

Mapuje ohraničené identity disků, oddílů a svazků s písmenem jednotky, vyhodnocuje nedostatek volného místa pod 10 % na každém svazku s písmenem jednotky, kontroluje přidělení úložiště stínových kopií VSS a výchozí kvóty jednotlivých svazků, shrnuje průběh aktuálních úloh modulu Storage, kontroluje vybraný svazek NTFS a jeho stav online samoopravy a kontroluje disková zařízení a řadiče v tomto počítači. Prahové hodnoty kapacity, nastavení kvót a stavy úloh jsou důkazy ke kontrole a nevybírají úklid, změnu velikosti, změnu kvót ani řízení úloh. Mapování písmen jednotek platí pro daný okamžik a nepokrývá každou cestu připojení. Nálezy zařízení se nemapují na vybraný svazek. Výchozím svazkem je svazek Windows. Nemění se žádné soubory, snímky, úlohy ani ovladače, nemění se žádné nastavení, žádný svazek se neodpojuje a nenaplánuje se žádný restart.

### Prostory úložiště

Inventarizuje fondy Prostorů úložiště, které nejsou primordiální, a virtuální disky včetně stavu, provozního stavu, kapacity, přidělení a odolnosti a potom vrátí jeden ohraničený provozní závěr. Windows může při odpovědi na tento dotaz pouze pro čtení spustit na vyžádání svého zprostředkovatele správy Prostorů úložiště. Fondy, virtuální disky, fyzické disky, svazky, odolnost, přidělení, nastavení údržby, zásady a typ spouštění se zachovávají. Nepožaduje se žádná operace s fondem, diskem, svazkem, přidělením, připojením, opravou, optimalizací ani nastavením. Výsledky zprostředkovatele neověřují dostupnost fondu, redundanci, integritu dat, obnovitelnost záloh ani V/V operace aplikací.

### Služba úložiště

Kontroluje ohraničené aktuální úlohy modulu Storage, přesnou historii selhání služby a aktuální připravenost Služby úložiště. Neúplné důkazy mají přednost. Stav úloh a historická selhání nemohou opravňovat k obnovení služby ani ke změnám úložiště. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující oznámení úložiště a aktivita úložiště řízená zásadami mohou pokračovat. Svazky, oddíly, souborové systémy, body připojení, zařízení, ovladače, nastavení Storage Sense, zásady údržby, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá žádnou operaci s úlohou, svazkem, oddílem, bodem připojení, souborem, zařízením, ovladačem ani nastavením úložiště. Ověření pokrývá pouze stabilní stav služby, nikoli výsledek úloh ani operace se zařízením, svazkem nebo zásadami úložiště.

### Služba Optimalizace jednotek

Kontroluje dostupnost služby Optimalizace jednotek. Volitelné obnovení spustí pouze přesně tuto povolenou a zastavenou službu po čerstvých kontrolách závislostí. Existující optimalizace naplánovaná systémem Windows může pokračovat podle aktuálních zásad údržby. Svazky, souborové systémy, data, plány optimalizace, zásady údržby, oprávnění služby a typ spouštění se zachovávají. KillerBench nevybírá svazek a žádnou jednotku neanalyzuje, nespouští na ní trim, nedefragmentuje, neoptimalizuje ani neplánuje. Ověření pokrývá pouze stabilní stav služby, nikoli analýzu, optimalizaci ani stav zařízení.
