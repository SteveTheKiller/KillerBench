# Šifrování jednotky nástrojem BitLocker

Tento postup použijte k zapnutí nástroje BitLocker, zálohování klíče pro obnovení a potvrzení, že je jednotka chráněná. KillerBench před zahájením šifrování zálohuje a ověří heslo pro obnovení a heslo pro obnovení nikdy nezobrazuje ani nezapisuje do protokolu.

## Než začnete

- Spusťte KillerBench jako správce.
- Jednotka Windows potřebuje ochranu TPM a ochranu heslem pro obnovení. Datové jednotce stačí heslo pro obnovení.
- Rozhodněte, kam se záloha klíče pro obnovení uloží: do souboru mimo šifrovanou jednotku a volitelně do Active Directory nebo Microsoft Entra ID.

## Kontrola jednotky

1. Otevřete **BitLocker** a v poli **Svazek** zvolte jednotku.
2. Ponechte operaci na **Kontrolovat** a stiskněte **Spustit diagnostiku**. Zpráva zobrazí stav šifrování, stav ochrany a identity ochran.
3. Pokud je TPM přítomen, ale není připraven, nejprve jej připravte v sekci **Zabezpečení**. KillerBench nikdy TPM nevymaže.

## Přidání potřebných ochran

1. Otevřete **Další akce nástroje BitLocker**.
2. Zvolte **Přidat ochranu TPM** nebo **Přidat heslo pro obnovení**, stiskněte **Náhled**, zaškrtněte **Provést tuto změnu na vybrané jednotce, když stisknu Spustit akci** a potom stiskněte **Spustit akci**.
3. Nové heslo pro obnovení ihned zálohujte. KillerBench jej nezobrazuje.

## Zálohování klíče pro obnovení a šifrování

1. Stiskněte **Najít ochrany pro obnovení** a vyberte ochranu heslem pro obnovení.
2. Zadejte úplnou cestu k záložnímu souboru mimo šifrovanou jednotku.
3. Zvolte **Šifrovat**, vyberte metodu šifrování a zaškrtněte **Zálohovat vybraný klíč pro obnovení a potom zahájit šifrování, když vyberu Spustit diagnostiku**.
4. Stiskněte **Spustit diagnostiku**. KillerBench zapíše zálohu, ověří ji a potom jednou zahájí šifrování.
5. Šifrování pokračuje na pozadí. Později spusťte **Kontrolovat** a potvrďte, že jednotka hlásí úplné zašifrování se zapnutou ochranou.

## Zálohování do Active Directory nebo Entra ID

V části **Další akce nástroje BitLocker** zvolte zálohu do adresáře pro vybranou ochranu. KillerBench ohlásí, že Windows požadavek přijal, ale kopii v adresáři přečíst nemůže, proto ji ověřte v Active Directory nebo v centru pro správu Entra.

## Datové jednotky a automatické odemykání

Po zašifrování datové jednotky zajistí **Zapnout automatické odemykání**, že se odemkne při spuštění Windows. Jednotka Windows musí být již plně zašifrovaná se zapnutou ochranou.

## Z příkazového řádku

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Viz [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) a [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Každý z nich má pro přerušený požadavek odpovídající příkaz `-recover`.

## Související

- [Sekce BitLocker](bitlocker.html)
- [Sekce Zabezpečení](security.html)
- [Jak změny zůstávají bezpečné](safety.html)
