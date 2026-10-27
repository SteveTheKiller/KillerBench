# Slik holdes endringer trygge

KillerBench behandler diagnose og endring som to ulike oppgaver. Diagnose leser bare. Den fulle systemskanningen kjører aldri reparasjoner. Velg en bestemt reparasjon eller handling, se over alternativer og forutsetninger, og bekreft den foreslåtte endringen.

## Før en endring

1. **Ferske bevis.** Verktøyet leser gjeldende tilstand på nytt. En lagret rapport eller en gammel hendelse gir aldri grunnlag for en endring.
2. **Nøyaktig kvalifisering.** Endringen tilbys bare når bevisene samsvarer med det nøyaktige problemet den løser. Alt som er ufullstendig, blokkerer den.
3. **Din bekreftelse.** Se over den valgte handlingen i bekreftelsesdialogen. Noen handlinger, som dekryptering av en stasjon, krever en ekstra bekreftelse. Dialogen følger det aktive apptemaet; utseendet endrer ikke kravene til handlingen.
4. **Forutsetninger.** Administratorrettigheter, omstartstilstand og eierskap via policy eller enhetsadministrasjon kontrolleres. Innstillinger som eies av en policy, blir ikke rørt.
5. **En ventende oppføring.** KillerBench skriver ned hva den skal gjøre før den ber Windows om å gjøre det.
6. **En siste kontroll.** Alt kontrolleres på nytt rett før forespørselen, så en endring i mellomtiden stopper den.

## Etter en endring

- KillerBench leser resultatet tilbake og melder **Verifisert** bare når Windows viser forventet tilstand.
- Hvis Windows melder at det lyktes, men tilstanden ikke endret seg, sier resultatet det, og ingenting gjentas.
- Hver endring lagres i en journal under `%LOCALAPPDATA%\KillerBench`.

## Hvis noe blir avbrutt

Støttede journalførte handlinger beholder en ventende oppføring når en forespørsel kan ha nådd Windows og verifiseringen ikke er fullført. Bruk handlingens dokumenterte avstemmingsvei, eller den tilhørende `-recover`-kommandoen, for å undersøke oppføringen. Avstemming er ikke en automatisk tilbakerulling. Ta vare på journalen når utfallet er usikkert, og ikke gjenta endringen manuelt. Oppførsel ved krasj, avbrudd og senere oppstart trenger fortsatt handlingsspesifikk native validering.

## Hva KillerBench ikke gjør

- Tømmer en TPM, sletter beskyttere den ikke har laget, eller viser gjenopprettingspassord.
- Endrer en innstilling som styres av gruppepolicy eller enhetsadministrasjon.
- Kjører en reparasjon bare fordi en historisk hendelse eller en importert rapport nevner et problem.

Diagnostikk og rapportanalyse kjører lokalt. En handling du velger, kan kontakte et eksternt mål, for eksempel programvarenedlastinger eller sikkerhetskopiering av gjenopprettingsnøkkelen til Active Directory eller Entra ID. Se over målet før du bekrefter. Lagrede opptak og rå bevis kan inneholde maskinidentitet og kildestier, så se over dem før du deler.

## Utviklingsstatus

Reparasjonsdekningen utvides fortsatt. En vellykket bygging eller simulert kontroll beviser ikke hvordan en reparasjon oppfører seg på en ekte maskin.
