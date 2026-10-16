# BitLocker

BitLocker undersøker stasjonskryptering, beskyttere og gjenopprettingsinformasjon, og hjelper deg deretter med å sikkerhetskopiere, beskytte eller endre en stasjon. Den fungerer bare på denne datamaskinen.

Trinnvise veiledninger: [Krypter en stasjon med BitLocker](encrypt-with-bitlocker.html).

## Bruk BitLocker-verktøyet

Velg stasjonsbokstav og en **Operasjon**, og trykk **Kjør diagnose**.

- **Inspiser** leser krypteringstilstand, beskyttelsesstatus og beskytteridentiteter. Det er standardvalget og endrer ingenting.
- **Sikkerhetskopier gjenoppretting** skriver det valgte gjenopprettingspassordet til en ny fil utenfor den beskyttede stasjonen og verifiserer det. Trykk først **Finn gjenopprettingsbeskyttere**, velg beskytteren og bekreft sikkerhetskopien.
- **Krypter** sikkerhetskopierer det valgte gjenopprettingspassordet, verifiserer sikkerhetskopien og starter deretter krypteringen én gang med metoden du velger. Windows-stasjonen trenger en TPM-beskytter og et gjenopprettingspassord. En datastasjon trenger bare et gjenopprettingspassord.
- **Gjenoppta beskyttelse** slår beskyttelsen på igjen for en fullstendig kryptert stasjon der den var satt på pause.

Hver av disse trenger sin egen bekreftelsesboks, og KillerBench kontrollerer stasjonen på nytt rett før den handler.

## Flere BitLocker-handlinger

Åpne **Flere BitLocker-handlinger** for endringer som ligger utenfor **Kjør diagnose**. Velg en handling, trykk **Forhåndsvis** for å lese nøyaktig hva den krever, bekreft og trykk **Kjør handling**.

- **Slå på automatisk opplåsing** gjør at en kryptert datastasjon låses opp når Windows starter. Windows-stasjonen må være fullstendig kryptert med beskyttelse slått på.
- **Legg til en TPM-beskytter** eller **Legg til et gjenopprettingspassord** oppretter én ny beskytter. KillerBench viser eller logger aldri et nytt gjenopprettingspassord, så ta sikkerhetskopi av det med en gang.
- **Sikkerhetskopier gjenopprettingspassordet til Active Directory** eller **Microsoft Entra ID** sender beskytteren du valgte ovenfor. KillerBench melder at Windows godtok forespørselen. Den kan ikke lese katalogkopien, så bekreft den der.
- **Dekrypter denne stasjonen** starter dekryptering, for eksempel før ny kryptering med en påkrevd metode. Den krever en ekstra bekreftelse fordi stasjonen er ubeskyttet etterpå, og den avvises for Windows-stasjonen så lenge andre stasjoner låses opp automatisk fra den.

## TPM og FIPS

Hvis TPM finnes, men ikke er klar, kan seksjonen Sikkerhet forberede den uten å tømme den. KillerBench tømmer aldri en TPM, fordi det ødelegger nøkler som BitLocker og Windows Hello har. Bryteren for FIPS-algoritmepolicyen er tilgjengelig som en eksplisitt, reversibel reparasjon for organisasjoner som trenger FIPS-godkjente BitLocker-PIN-koder.

## Verktøy i denne seksjonen

Undersøk kryptering, beskyttere og gjenopprettingsinformasjon, og sikkerhetskopier dem eller beskytt stasjonen.

### BitLocker

Forhåndsviser BitLocker-alternativer og undersøker det valgte volumet. Denne arbeidsflyten utfører ingen kryptering eller sikkerhetskopiering av gjenoppretting.
