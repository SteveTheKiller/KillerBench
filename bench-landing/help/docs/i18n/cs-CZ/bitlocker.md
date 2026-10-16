# BitLocker

BitLocker kontroluje šifrování jednotek, ochrany a informace pro obnovení a potom vám pomůže jednotku zálohovat, chránit nebo změnit. Pracuje pouze na tomto počítači.

Návody krok za krokem: [Šifrování jednotky nástrojem BitLocker](encrypt-with-bitlocker.html).

## Používání nástroje BitLocker

Zvolte písmeno jednotky a **Operace** a potom stiskněte **Spustit diagnostiku**.

- **Kontrolovat** čte stav šifrování, stav ochrany a identity ochran. Je to výchozí operace a nic nemění.
- **Zálohovat obnovení** zapíše vybrané heslo pro obnovení do nového souboru mimo chráněnou jednotku a ověří jej. Nejprve stiskněte **Najít ochrany pro obnovení**, zvolte ochranu a potom zálohu potvrďte.
- **Šifrovat** zálohuje vybrané heslo pro obnovení, ověří zálohu a potom jednou zahájí šifrování zvolenou metodou. Jednotka Windows potřebuje ochranu TPM a heslo pro obnovení. Datové jednotce stačí heslo pro obnovení.
- **Obnovit ochranu** znovu zapne ochranu na plně zašifrované jednotce, na které byla pozastavena.

Každá z těchto operací vyžaduje vlastní potvrzovací políčko a KillerBench jednotku znovu zkontroluje těsně před zásahem.

## Další akce nástroje BitLocker

Pro změny mimo **Spustit diagnostiku** otevřete **Další akce nástroje BitLocker**. Zvolte akci, stisknutím **Náhled** si přečtěte, co přesně vyžaduje, a potom ji potvrďte a stiskněte **Spustit akci**.

- **Zapnout automatické odemykání** zajistí, že se zašifrovaná datová jednotka odemkne při spuštění Windows. Jednotka Windows musí být plně zašifrovaná se zapnutou ochranou.
- **Přidat ochranu TPM** nebo **Přidat heslo pro obnovení** vytvoří jednu novou ochranu. KillerBench nové heslo pro obnovení nikdy nezobrazuje ani nezapisuje do protokolu, proto jej ihned zálohujte.
- **Zálohovat heslo pro obnovení do služby Active Directory** nebo **Microsoft Entra ID** odešle ochranu, kterou jste vybrali výše. KillerBench ohlásí, že Windows požadavek přijal. Kopii v adresáři přečíst nemůže, proto ji ověřte tam.
- **Dešifrovat tuto jednotku** zahájí dešifrování, například před opětovným šifrováním požadovanou metodou. Vyžaduje druhé potvrzení, protože jednotka poté není chráněná, a odmítne jednotku Windows, pokud se z ní automaticky odemykají jiné jednotky.

## TPM a FIPS

Pokud je TPM přítomen, ale není připraven, sekce Zabezpečení jej umí připravit bez vymazání. KillerBench TPM nikdy nevymaže, protože by tím zničil klíče, které uchovávají BitLocker a Windows Hello. Přepínač zásad algoritmů FIPS je k dispozici jako výslovná a vratná oprava pro organizace, které potřebují kódy PIN BitLocker schválené podle FIPS.

## Nástroje v této sekci

Kontrolujte šifrování, ochrany a informace pro obnovení a potom je zálohujte nebo jednotku ochraňte.

### BitLocker

Zobrazí náhled možností BitLocker a zkontroluje vybraný svazek. Tento postup neprovádí žádné šifrování ani zálohování pro obnovení.
