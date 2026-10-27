# Nedlasting og oppsett

KillerBench 1.0.0 er ikke utgitt. Ingen offentlig KillerBench-pakke eller nedlasting er tilgjengelig ennå. Denne siden beskriver gjeldende pakkekrav, slik at du kan forberede deg uten å laste ned en uoffisiell kopi.

## Windows og kjøretid

- Gjeldende publiseringsmål er **Windows x64**. Windows 11 x64 er gjeldende grunnlag for native validering. Det finnes ingen native utgaver for ARM64, x86 eller Windows Server.
- KillerBench pakkes på to måter. Installasjonsprogrammet er **rammeverksavhengig** og krever **.NET Desktop Runtime 10 for Windows x64**. Den vanlige .NET Runtime, ASP.NET Core Runtime og .NET Framework inneholder ikke WPF. Den bærbare utgaven har sin egen kjøretid og trenger ingen egen installasjon. Kommandolinjeverktøyet `killerbench-cli.exe` trenger .NET 10 Runtime for Windows x64. Et SDK trengs bare til utvikling, ikke til vanlig bruk.
- Bruk en Windows-versjon som støttes av [.NET 10 på Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows). At kjøretiden støttes, betyr ikke at KillerBench-reparasjoner er validert på alle støttede utgaver eller versjoner. Dekning for Windows 10 er fortsatt ventende, og de støttede utgavene begrenses av Microsofts gjeldende livssykluskrav.
- Hent Desktop Runtime bare fra [Microsofts nedlastingsside for .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), og velg **.NET Desktop Runtime**, **Windows**, **x64**. Følg organisasjonens regler for programvareinstallasjon.

Hvis `dotnet` er tilgjengelig, skal `dotnet --list-runtimes` vise `Microsoft.WindowsDesktop.App 10.0.x` for x64-installasjonen. En kjøretid som bare er installert for en annen arkitektur, holder ikke for x64-installasjonspakken.

## Når en pakke blir publisert

1. Bruk den offisielle nedlastingen det lenkes til fra dette nettstedet. Ingen nedlastingslenke for KillerBench er aktiv ennå.
2. Les pakkens krav, [GPLv3-lisensen](../license.txt), instruksjonene for tilhørende kildekode, inkluderte tredjepartsmerknader og informasjon om signatur og kontrollsum. Et filnavn alene beviser ikke ektheten.
3. Kjør installasjonsprogrammet for gjeldende konto, eller for alle brukere etter en forespørsel om utvidede rettigheter. Det legger til en snarvei på Start-menyen, en valgfri snarvei på skrivebordet og en oppføring i Legg til eller fjern programmer. Den bærbare utgaven kjører uten installasjon.
4. Start KillerBench med den vanlige kontoen din, og følg [Din første diagnose](first-run.html). Tilgangsfeil i diagnosen forblir synlige; utvidede rettigheter er et eget, bevisst valg.

Ikke omgå sikkerhetsadvarsler i Windows for å kjøre en uverifisert kopi. Ha en oppdatert sikkerhetskopi før du velger en handling som endrer maskinen.

Ved oppstart spør KillerBench GitHub om en nyere stabil versjon og spør deg før den installeres. Nedlastingen må samsvare med den publiserte SHA-256-kontrollsummen. Slå av kontrollen med **Se etter oppdateringer ved oppstart** under Om.

## Gjeldende begrensninger

Gjeldende versjon har tolv seksjoner, skrivebeskyttet diagnose, rapporter og reparasjonsflater som velges enkeltvis. Native validering av reparasjoner pågår fortsatt. Et fungerende grensesnitt, en vellykket bygging eller et fixture-resultat beviser ikke hvordan en reparasjon oppfører seg på en ekte datamaskin. Se [Slik holdes endringer trygge](safety.html#development-status).

Illustrasjonskonsollen på landingssiden er syntetiske utdata. Den første offentlige pakken er fortsatt ventende.

## Kildekode, lisens og støtte

KillerBench er lisensiert under GPLv3. Instruksjoner for levering av kildekode og tillatelser for tredjepartsskrifter er fortsatt ventende for den første offentlige pakken. Les [Om KillerBench](about.html) for tilgjengelighet av kildekode, distribusjonsmerknader, personvern for bevis og støtteinformasjon. [Engine-veiledningen](../engine/) er for utviklere med en autorisert kildekodekopi; den betyr ikke at en offentlig Engine-pakke er tilgjengelig.
