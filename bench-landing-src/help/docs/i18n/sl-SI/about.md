# O aplikaciji KillerBench

KillerBench je delovno okolje za diagnostiko, popravila in vzdrževanje sistema Windows, ki ga razvija Steve the Killer, in je del [družine Killer Tools](https://killertools.net). Različica 1.0.0 še ni izdana. Namizna aplikacija, Engine in CLI si delijo diagnostične dokaze ter podprte postopke popravil.

## Razpoložljivost in obseg

Javni prenos programa KillerBench ali paketa Engine še ni na voljo. [Prenos in namestitev](setup.html) opisuje trenutno ciljno platformo Windows x64 in zahteve za Desktop Runtime. Preverjanje izvornih popravil še poteka; prisotnost orodja ne dokazuje, da so bile preizkušene vse napake, prekinitve ali konfiguracije sistema Windows.

## Obvestila o izvorni kodi in distribuciji

KillerBench je licenciran pod **GPLv3**. Njegova distribucijska licenca je [GNU General Public License, različica 3](../license.txt). Licenca določa dovoljenja in pogoje za kopiranje, spreminjanje ter distribucijo zajetega dela.

Javni [repozitorij KillerBench](https://github.com/SteveTheKiller/KillerBench) trenutno vsebuje informacije o projektu. Celotna implementacija tam ni javno dostopna. Ti vodniki za razvijalce opisujejo pooblaščeno lokalno kopijo izvorne kode; ne omogočajo javnega prenosa izvorne kode.

Pred distribucijo izvršljivih datotek mora izdajatelj zagotoviti ustrezno izvorno kodo (Corresponding Source) na način, ki ga dovoljuje [6. člen GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html#section6), vključno z izvorno kodo in gradivom za gradnjo, ki ju zahteva licenca. Navodila za pridobitev izvorne kode prvega javnega paketa in priložena obvestila tretjih oseb še niso pripravljena. GPLv3 ne daje dovoljenja za nadaljnjo distribucijo pisav tretjih oseb; trenutna dovoljenja za pisave je še treba preveriti. Pred nadaljnjo distribucijo objavljenega paketa preglejte njegovo licenco, navodila za izvorno kodo in obvestila.

## Zasebnost in zunanje storitve

Diagnostika in analiza dnevnikov potekata lokalno brez storitve umetne inteligence ali računa KillerBench. Dnevniki dejanj in shranjeni dokazi uporabljajo mape `%LOCALAPPDATA%\KillerBench` lokalnega računa. Izbrana dejanja lahko vzpostavijo stik s storitvijo Windows Update, storitvami za prenos programske opreme, imeniki ali drugimi navedenimi cilji. Preverjanje posodobitev ob zagonu vzpostavi stik z GitHubom, posodobitve znanja in stran namestitvenega programa MCP pa s svojimi nastavljenimi storitvami. Odjemalec umetne inteligence, ki uporablja MCP, lahko prejme vrnjeni referenčni rezultat.

To spletno mesto shrani izbiro teme in poudarka v lokalno shrambo brskalnika. Njegovi vodniki ne izvajajo diagnostike v vašem računalniku. Surovi dnevniki, uvožene datoteke in izvoženi dokazi lahko vsebujejo imena računalnikov, račune, poti ali občutljivo izvorno besedilo. Pred deljenjem jih preglejte; v javno poročilo ne vključujte obnovitvenih ključev, poverilnic ali podatkov strank.

## Podpora

Informacije o projektu so na voljo v javnem repozitoriju. Za podporo uporabite kontaktne podatke na [avtorjevem spletnem mestu](https://thekiller.net); navedite različico aplikacije, gradnjo sistema Windows, točno orodje, pričakovani in opaženi rezultat. Pred pošiljanjem preglejte dokaze in odstranite občutljive podatke, za podatke strank pa uporabite odobren zasebni kanal. Pri negotovem izidu popravila ohranite dnevnike dejanj.

## Sorodni vodniki

- [Kako so spremembe varovane](safety.html)
- [Poročila in dokazi](reports.html)
- [Vodnik za razvijalce Engine](../engine/)
- [KillerMCP](../mcp.html)
