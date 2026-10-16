# Oprava služby Windows Update

Tento postup použijte, když aktualizace selhávají, zasekávají se nebo se stále znovu instalují. KillerBench nejprve přečte řetězec aktualizací, vysvětlí selhání i s důkazy a službu spustí nebo úložiště komponent opraví jen tehdy, když čerstvé důkazy prokážou přesně tento problém.

## Než začnete

- Spusťte KillerBench jako správce. Služby aktualizací, úložiště komponent a chráněné systémové soubory vyžadují pro úplné důkazy zvýšená oprávnění.
- Pokud Windows již čeká na restart, nejprve restartujte. Mnoho oprav aktualizací je zablokovaných, dokud není stav restartu čistý.
- Při údržbě nechte počítač připojený k napájení.

## Zjištění, proč aktualizace selhávají

1. Otevřete **Aktualizace** a v bočním panelu nástrojů zvolte **Windows Update**.
2. Při prvním průchodu nechte **Automaticky opravit potvrzené problémy** vypnuté a stiskněte **Spustit diagnostiku**.
3. Přečtěte si **Čitelnou zprávu**. Uvádí zastavené služby aktualizací, čekající restarty, nedostatek volného místa, poškození úložiště komponent a přesné kódy chyb, které Windows zaznamenal, spolu s doporučeným dalším krokem.
4. Pokud zpráva uvádí kód chyby, odpovídající navazující kontroly se spustí automaticky. Kód můžete vyhledat i sami pomocí `killerbench-cli update-code 0x80070002`.

## Sledování aktualizace, která se zdá zaseknutá

V části **Aktualizace** zvolte **Monitor aktivity údržby** a stiskněte **Spustit diagnostiku**. Opakovaně pořizuje snímky aktivity aktualizací, Setup a Installeru, součtů přenosů a indikátorů restartu. Aktivita se zobrazuje pouze jako důkaz; sama o sobě nikdy netvrdí, že došlo k zaseknutí nebo závadě.

## Oprava toho, co zpráva našla

1. Spusťte **Windows Update** znovu se zapnutou možností **Automaticky opravit potvrzené problémy**. Může spustit povolené služby aktualizací, které jsou prokazatelně zastavené, a DISM nebo SFC spustí jen tehdy, když živá kontrola potvrdí poškození.
2. Pro jednu službu místo toho zvolte její cílený nástroj, například **Služba BITS**, **Kryptografické služby**, **Instalační služba modulů Windows** nebo **Služba Update Orchestrator**. Každý z nich nabízí obnovení jen pro danou službu.
3. Když zpráva uvádí, že je třeba restartovat, restartujte dříve, než spustíte cokoli dalšího. KillerBench zaznamenává výsledky vyžadující restart a další práci odkládá, dokud restart neproběhne.

Každá oprava se zapisuje do deníku a znovu čte. Úplný postup popisuje stránka [Jak změny zůstávají bezpečné](safety.html).

## Instalace konkrétní aktualizace

1. V části **Aktualizace** zvolte **Dostupné aktualizace** a stisknutím **Spustit diagnostiku** vyhledejte aktualizace z nakonfigurovaného zdroje.
2. Vyberte požadované aktualizace a stiskněte **Náhled vybraných**. Náhled přesně uvádí, co by se nainstalovalo, a případný požadavek na restart.
3. Instalujte až po přečtení náhledu. KillerBench nikdy nevybírá ani neschvaluje aktualizaci za vás.

## Z příkazového řádku

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Viz [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) a [update-plan](cli.html#cmd-update-plan). Pokud byla instalace přerušena, spusťte [update-recover](cli.html#cmd-update-recover); zkontroluje výsledek a aktualizaci nikdy neopakuje.

## Související

- [Sekce Aktualizace](updates.html)
- [Oprava systémových souborů](repair-system-files.html)
- [Řešení potíží](troubleshooting.html)
