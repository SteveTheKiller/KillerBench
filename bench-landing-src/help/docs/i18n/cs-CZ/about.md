# O KillerBench

KillerBench je pracovní nástroj pro diagnostiku, opravy a údržbu Windows od Steve the Killer, součást [rodiny Killer Tools](https://killertools.net). Verze 1.0.0 zatím není vydána. Jeho desktopová aplikace, Engine a CLI sdílejí diagnostické důkazy a podporované postupy oprav.

## Dostupnost a rozsah

Veřejné stažení KillerBench ani balíček Engine zatím nejsou k dispozici. Stránka [Stažení a nastavení](setup.html) popisuje aktuální cíl Windows x64 a požadavky na Desktop Runtime. Nativní ověřování oprav stále probíhá; existence nástroje není důkazem, že byla otestována každá chyba, přerušení nebo konfigurace Windows.

## Zdrojový kód a oznámení o distribuci

KillerBench je licencován pod **GPLv3**. Jeho distribuční licencí je [GNU General Public License verze 3](../license.txt). Licence popisuje oprávnění a podmínky pro kopírování, úpravy a distribuci díla, na které se vztahuje.

Veřejné [úložiště KillerBench](https://github.com/SteveTheKiller/KillerBench) v současné době poskytuje informace o projektu. Úplná implementace v něm veřejně dostupná není. Tyto příručky pro vývojáře popisují autorizovaný zdrojový kód; neposkytují veřejné stažení zdrojového kódu.

Před distribucí binárního souboru musí jeho vydavatel poskytnout odpovídající zdrojový kód způsobem povoleným [oddílem 6 GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html#section6), včetně zdrojového kódu a sestavovacích materiálů, které licence vyžaduje. Pokyny k poskytnutí zdrojového kódu a přiložená oznámení třetích stran pro první veřejný balíček stále čekají. GPLv3 neuděluje oprávnění k další distribuci písem třetích stran; aktuální oprávnění k písmům je ještě třeba ověřit. Před další distribucí zveřejněného balíčku si přečtěte jeho licenci, pokyny ke zdrojovému kódu a oznámení.

## Soukromí a externí služby

Diagnostika a analýza protokolů probíhají místně bez služby AI a bez účtu KillerBench. Deníky a uchované důkazy používají složky `%LOCALAPPDATA%\KillerBench` místního účtu. Vybrané akce mohou kontaktovat Windows Update, služby pro stahování softwaru, adresáře nebo jiné pojmenované cíle. Kontrola aktualizací při spuštění kontaktuje GitHub a aktualizace znalostí i stránka instalačního programu MCP kontaktují své nakonfigurované služby. Klient AI, který používá MCP, může obdržet vrácený referenční výsledek.

Tento web ukládá volbu motivu a barvy zvýraznění do místního úložiště prohlížeče. Jeho příručky na vašem počítači žádnou diagnostiku nespouštějí. Surové protokoly, importované soubory a exportované důkazy mohou obsahovat názvy počítačů, účty, cesty nebo citlivý zdrojový text. Před sdílením je zkontrolujte; do veřejné zprávy nevkládejte klíče pro obnovení, přihlašovací údaje ani data zákazníků.

## Podpora

Informace o projektu jsou k dispozici ve veřejném úložišti. Pro podporu použijte kontaktní údaje zveřejněné na [webu autora](https://thekiller.net); uveďte verzi aplikace, sestavení Windows, přesný nástroj, očekávaný výsledek a pozorovaný výsledek. Před odesláním důkazy zkontrolujte a začerněte a pro informace o zákaznících použijte schválený soukromý kanál. U nejistého výsledku opravy si deníky ponechte.

## Související příručky

- [Jak změny zůstávají bezpečné](safety.html)
- [Zprávy a důkazy](reports.html)
- [Vývojářská příručka Engine](../engine/)
- [KillerMCP](../mcp.html)
