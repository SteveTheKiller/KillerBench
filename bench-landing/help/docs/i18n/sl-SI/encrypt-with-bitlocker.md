# Šifrirajte pogon z BitLockerjem

Uporabite za vklop BitLockerja, varnostno kopiranje obnovitvenega ključa in preverjanje zaščite pogona. KillerBench pred začetkom šifriranja varnostno kopira in preveri obnovitveno geslo ter ga nikoli ne prikaže ali zapiše v dnevnik.

## Pred začetkom

- Zaženite KillerBench kot skrbnik.
- Pogon Windows potrebuje zaščitnik TPM in zaščitnik z obnovitvenim geslom. Podatkovni pogon potrebuje samo obnovitveno geslo.
- Določite cilj varnostne kopije obnovitvenega ključa: datoteko zunaj pogona, ki ga šifrirate, in po želji Active Directory ali Microsoft Entra ID.

## Preverite pogon

1. Odprite **BitLocker** in izberite pogon v **Nosilec**.
2. Pustite opravilo na **Preglej** in pritisnite **Zaženi diagnostiko**. Poročilo prikaže stanje šifriranja, zaščite in identitete zaščitnikov.
3. Če je TPM prisoten, vendar ni pripravljen, ga najprej pripravite v razdelku **Varnost**. KillerBench nikoli ne počisti TPM-ja.

## Dodajte potrebne zaščitnike

1. Odprite **Več dejanj za BitLocker**.
2. Izberite **Dodaj zaščitnik TPM** ali **Dodaj obnovitveno geslo**, pritisnite **Predogled**, označite **Izvedi to spremembo na izbranem pogonu, ko pritisnem Izvedi dejanje** in nato pritisnite **Izvedi dejanje**.
3. Novo obnovitveno geslo takoj varnostno kopirajte. KillerBench ga ne prikaže.

## Varnostno kopirajte obnovitveni ključ in šifrirajte

1. Pritisnite **Poišči zaščitnike za obnovitev** in izberite zaščitnik z obnovitvenim geslom.
2. Vnesite polno pot do datoteke varnostne kopije zunaj pogona, ki ga šifrirate.
3. Izberite **Šifriraj**, izberite način šifriranja in označite **Varnostno kopiraj izbrani obnovitveni ključ in začni šifriranje, ko izberem Zaženi diagnostiko**.
4. Pritisnite **Zaženi diagnostiko**. KillerBench zapiše varnostno kopijo, jo preveri in nato enkrat začne šifriranje.
5. Šifriranje se nadaljuje v ozadju. Pozneje izvedite **Preglej**, da potrdite, da je pogon v celoti šifriran in ima vklopljeno zaščito.

## Varnostno kopirajte v Active Directory ali Entra ID

V **Več dejanj za BitLocker** izberite varnostno kopiranje v imenik za izbrani zaščitnik. KillerBench sporoči, da je Windows sprejel zahtevo, vendar ne more prebrati kopije v imeniku, zato jo potrdite v Active Directory ali skrbniškem središču Entra.

## Podatkovni pogoni in samodejno odklepanje

Po šifriranju podatkovnega pogona možnost **Vklopi samodejno odklepanje** omogoči odklepanje ob zagonu sistema Windows. Pogon Windows mora biti že v celoti šifriran in imeti vklopljeno zaščito.

## Iz ukazne vrstice

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Glejte [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) in [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Vsak ima ustrezen ukaz `-recover` za prekinjeno zahtevo.

## Povezano

- [Razdelek BitLocker](bitlocker.html)
- [Razdelek Varnost](security.html)
- [Kako spremembe ostanejo varne](safety.html)
