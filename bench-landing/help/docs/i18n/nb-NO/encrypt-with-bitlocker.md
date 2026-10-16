# Krypter en stasjon med BitLocker

Bruk denne veiledningen for å slå på BitLocker, ta sikkerhetskopi av gjenopprettingsnøkkelen og bekrefte at stasjonen er beskyttet. KillerBench sikkerhetskopierer og verifiserer gjenopprettingspassordet før krypteringen starter, og viser eller logger aldri et gjenopprettingspassord.

## Før du begynner

- Kjør KillerBench som administrator.
- Windows-stasjonen trenger en TPM-beskytter og en beskytter med gjenopprettingspassord. En datastasjon trenger bare et gjenopprettingspassord.
- Bestem hvor sikkerhetskopien av gjenopprettingsnøkkelen skal ligge: en fil utenfor stasjonen du krypterer, og eventuelt Active Directory eller Microsoft Entra ID.

## Kontroller stasjonen

1. Åpne **BitLocker** og velg stasjonen i **Volum**.
2. La operasjonen stå på **Inspiser** og trykk **Kjør diagnose**. Rapporten viser krypteringstilstand, beskyttelsesstatus og beskytteridentiteter.
3. Hvis TPM finnes, men ikke er klar, forbereder du den først fra **Sikkerhet**. KillerBench tømmer aldri en TPM.

## Legg til beskytterne du trenger

1. Åpne **Flere BitLocker-handlinger**.
2. Velg **Legg til en TPM-beskytter** eller **Legg til et gjenopprettingspassord**, trykk **Forhåndsvis**, merk av **Gjør denne endringen på den valgte stasjonen når jeg trykker Kjør handling**, og trykk deretter **Kjør handling**.
3. Ta sikkerhetskopi av et nytt gjenopprettingspassord med en gang. KillerBench viser det ikke.

## Sikkerhetskopier gjenopprettingsnøkkelen og krypter

1. Trykk **Finn gjenopprettingsbeskyttere** og velg beskytteren med gjenopprettingspassord.
2. Skriv inn en fullstendig bane for sikkerhetskopifilen utenfor stasjonen du krypterer.
3. Velg **Krypter**, velg krypteringsmetode og merk av **Sikkerhetskopier den valgte gjenopprettingsnøkkelen, og start deretter krypteringen når jeg velger Kjør diagnose**.
4. Trykk **Kjør diagnose**. KillerBench skriver sikkerhetskopien, verifiserer den og starter deretter krypteringen én gang.
5. Krypteringen fortsetter i bakgrunnen. Kjør **Inspiser** senere for å bekrefte at stasjonen melder full kryptering med beskyttelse slått på.

## Sikkerhetskopier til Active Directory eller Entra ID

Under **Flere BitLocker-handlinger** velger du katalogsikkerhetskopien for den valgte beskytteren. KillerBench melder at Windows godtok forespørselen, men kan ikke lese katalogkopien, så bekreft den i Active Directory eller Entra-administrasjonssenteret.

## Datastasjoner og automatisk opplåsing

Etter at en datastasjon er kryptert, gjør **Slå på automatisk opplåsing** at den låses opp når Windows starter. Windows-stasjonen må allerede være fullstendig kryptert med beskyttelse slått på.

## Fra kommandolinjen

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Se [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) og [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Hver av dem har en tilhørende `-recover`-kommando for en avbrutt forespørsel.

## Relatert

- [Seksjonen BitLocker](bitlocker.html)
- [Seksjonen Sikkerhet](security.html)
- [Slik holdes endringer trygge](safety.html)
