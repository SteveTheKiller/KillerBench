# Teknikken i KillerBench

En titt under panseret på hvordan KillerBench leser en Windows-datamaskin, holder bevisene ærlige og avgjør når en endring er tillatt.

## Teknologistabel

KillerBench er en native Windows-app. Skrivebordsappen, kommandolinjen og Engine under dem deler de samme diagnosebevisene og reparasjonsflytene.

| Komponent | Detalj |
| --- | --- |
| Grensesnitt | WPF på .NET 10 (`net10.0-windows`), Windows x64, egendefinert vindusramme |
| Engine | `Engine.Core`, `Engine` og `Engine.Windows`, uten referanser til WPF eller skrivebordskontroller |
| Kommandolinje | `killerbench-cli`, en egen vert på samme Engine |
| Windows-leverandører | .NET, Windows-API-er, PowerShell og Windows-kommandolinjeverktøy, hver kjørt innenfor faste grenser for tid, oppføringer og størrelse |
| Rapporter | Rå utdata, Lesbar rapport, HTML, tekst og JSON |
| Referansedata | Versjonerte kunnskapspakker for feilkoder, maskinvareidentifikatorer, leverandører og støttereferanser |
| Pakking | Installasjonsprogram: rammeverksavhengig, krever .NET Desktop Runtime 10 for Windows x64. Bærbar: selvstendig, ingen installasjon av kjøretid |
| Utseende | 13 familietemaer og 20 språk |

## Installasjon, mapper og data

KillerBench leveres i to former. Installasjonsprogrammet er standardnedlastingen og støtter installasjon per bruker eller for alle brukere. Den bærbare utgaven har sin egen .NET-kjøretid og kjører uten installasjon.

KillerBench kjører med rettighetene til kontoen som startet den. Den gir seg ikke selv utvidede rettigheter. Start den med **Kjør som administrator** når beskyttede bevis, som BitLocker-beskyttere, eller en reparasjon krever administratortilgang. Utvidede rettigheter velger eller godkjenner aldri en reparasjon.

### Hva installasjonsprogrammet gjør

Installasjonsprogrammet verifiserer og pakker ut appen til `%LOCALAPPDATA%\Programs\KillerBench\`, eller til `%ProgramFiles%\KillerBench\` etter en forespørsel om utvidede rettigheter for alle brukere. Det legger til en snarvei på Start-menyen, en valgfri snarvei på skrivebordet og en oppføring i Legg til eller fjern programmer med vanlig og stille avinstallering. Den bærbare utgaven verifiserer og pakker ut innholdet under `%LOCALAPPDATA%\KillerBench\Portable\` for gjeldende kjøring og rydder bort forlatte kopier ved neste oppstart.

### Hvor ting ligger

| Plassering | Inneholder |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Tema, aksentfarge, skalering av grensesnittet og språk |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Én JSON-journal per kjøring av en arbeidsflyt: beslutninger, avgrensede kommandoutdata og reparasjonsresultater |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Kjøringsjournaler for vedlikeholdsreparasjoner som DISM og SFC |
| Funksjonsmapper under `%LOCALAPPDATA%\KillerBench\` | Ventende vern og utfall for enkeltstående beskyttede handlinger, for eksempel `BitLockerBackup`, `Time`, `Firewall` og `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | KillerMCP-reparasjonsnivået som er valgt under Om |
| `HKCU` eller `HKLM\Software\KillerBench` | Installert bane, for tilsvarende installasjonsomfang |
| En `.kbench`-fil du velger | Et lagret opptak med bevarte bevis |

Ingenting lastes opp. Slett en journal bare når du ikke lenger trenger bevisene for et usikkert utfall.

## Diagnose først

KillerBench behandler diagnose og endring som to ulike oppgaver. Diagnose leser bare. En endring skjer bare når du velger en bestemt reparasjon eller handling, ser over alternativer og forutsetninger og bekrefter den.

Den fulle systemskanningen er bare diagnose, også når en spesifikk arbeidsflyt har automatisk reparasjon slått på. Lagrede opptak, importerte logger og historiske hendelser gir aldri grunnlag for en reparasjon.

### Avgrenset innsamling

Hver native kommando og hver Windows-leverandør kjører innenfor faste grenser for tid, oppføringer og størrelse. En leverandør registrerer avslutningsstatus, tidsavbrudd, avkorting, standard utdata og feilutdata. En leverandør som mislykkes eller blir avkortet, kan aldri leses som et rent, tomt svar.

### Minimal oppbevaring

Innsamlerne beholder det et funn trenger. Mange resultater beholder navn, kategorier, antall, tilstander og tidsstempler, men utelater påloggingsinformasjon, kommandolinjer, fullstendige stier, kontoidentiteter, nettverksendepunkter og irrelevant hendelsesnyttelast.

## Bevis du kan følge

Hvert funn er knyttet til bevisene som ga det, og rapporten skiller alltid mellom det som er bevist, og det som bare er observert.

| Tilstand | Betydning |
| --- | --- |
| Må gjennomgås | Bevisene støtter et problem som fortjener oppmerksomhet. |
| Informasjon | Bevis ble samlet inn og vises som kontekst. |
| Ufullstendige eller Ukjent | En kontroll kunne ikke fullføres, eller beviset var utilgjengelig. Det regnes aldri som sunt. |
| Historisk | En tidligere hendelse. Den forklarer historikken, men beviser ikke en aktuell feil. |
| Verifisert | En reparasjon eller endring ble bekreftet ved å lese resultatet tilbake. |

### Ukjent er et resultat

Ukjent betyr at tilgang ble nektet, en kilde manglet, en leverandør mislyktes, utdataene var feilformet, en grense ble overskredet eller bevisene var uenige. KillerBench viser begrensningen og det neste nyttige trinnet i stedet for å skjule den.

### Historikk forblir historikk

Hendelsesoppføringer og importerte logger beskriver det som ble registrert på et tidligere tidspunkt. De kan peke på en feilkode eller neste diagnoseomfang. De beviser ikke at feilen fortsatt er aktiv.

## Beskyttede reparasjoner

Reparasjoner er typede operasjoner med regler for kvalifisering, kjøring, verifisering og gjenoppretting. De er ikke kommandostrenger plukket ut av en logglinje. Hver støttet reparasjon følger samme rekkefølge:

1. Samle inn fullstendige gjeldende bevis.
2. Finne samsvar med én støttet oppskrift.
3. Se over nødvendige alternativer og konsekvenser.
4. Bekrefte administrative forutsetninger og policyforutsetninger.
5. Samle inn de avgjørende bevisene på nytt rett før endringen.
6. Skrive en varig forberedelsesoppføring.
7. Kjøre én avgrenset native handling.
8. Samle inn ferske verifiseringsbevis.
9. Registrere et verifisert, blokkert, stoppet eller usikkert utfall.

Hvis de avgjørende bevisene endrer seg før handlingen kjøres, stopper KillerBench uten å utføre den.

### Eksplisitte valg

Reparasjoner som avhenger av et alternativ, krever et nøyaktig valg fra deg, for eksempel et volum, en skriverkø, et mål for gjenopprettingsnøkkelen, en strømverdi, en pakkeversjon eller en reparasjonskilde. KillerBench finner aldri på disse verdiene.

### Administrerte datamaskiner

Gruppepolicy og enhetsadministrasjon kan gjøre en lokal endring virkningsløs eller upassende. Støttede reparasjoner undersøker hvem som eier en innstilling, og stopper når eierskapet er administrert eller ukjent. Innstillinger som eies av en policy, blir ikke rørt.

### Automatisk reparasjon

Automatisk reparasjon lar bare kvalifiserte oppskrifter fortsette. Den omgår ikke bevis, velger ikke manglende alternativer, ignorerer ikke policy, prøver ikke en usikker handling på nytt, starter ikke Windows på nytt og gjør ikke et funn som må gjennomgås, om til en reparasjon.

## Journaler og avbrudd

Før KillerBench ber Windows om en endring, skriver den ned hva den skal gjøre. Hver endring lagres i en journal under `%LOCALAPPDATA%\KillerBench`.

Etter endringen leser KillerBench resultatet tilbake og melder **Verifisert** bare når Windows viser forventet tilstand. Hvis Windows melder at det lyktes, men tilstanden ikke endret seg, sier resultatet det, og ingenting gjentas.

En prosess kan stoppe etter at Windows har mottatt en forespørsel, men før verifiseringen er ferdig. Den ventende oppføringen blir liggende, og en senere avstemming leser fersk tilstand for å finne ut hva som skjedde. Avstemming er ikke en automatisk tilbakerulling og gjentar aldri den opprinnelige endringen. Ta vare på journalen når utfallet er usikkert.

## Konstanter og grenser

| Spesifikasjon | Verdi |
| --- | --- |
| Diagnosedekning | Full systemskanning kjører 49 diagnoseområder og kontrollerer 96 beskyttede reparasjonsoppskrifter |
| Lesing av tekstkilder | 1 000 000 tegn per kilde, 2 000 samsvarende linjer vist |
| Øktkilder | 12 i sidefeltet; et opptak rommer opptil 24 kilder og 64 MB |
| Hendelsesspørringer | De nyeste 200 hendelsene per kanal fra de siste 7 dagene |
| Windows Update-skanning | 15 sekunder per kilde, to minutter totalt |
| Logganalyse | 10 000 tolkede oppføringer, 256 ulike koder, 8 utdrag på 320 tegn per kode |
| Komprimert CBS-arkiv | Arkiv på 256 MiB, loggmedlem på 512 MiB, utpakking på 30 sekunder |
| Skrivebeskyttet CHKDSK | Ti minutter; en reparasjon er begrenset til 65 minutter |
| DNS- og TCP-kontroller | Ti sekunder som standard, høyst ett minutt |
| Tjenesteoversikt | Opptil 1 024 installerte tjenester |
| Kunnskapspakker | Opptil 32 åpne samtidig |
| Gjennomgang av store filer | Filer på minst 100 MB |
| Overvåking av vedlikeholdsaktivitet | Intervall på 1 til 60 sekunder, 1 til 120 øyeblikksbilder |
| Utkast til planlagte oppgaver | Kjøregrense på 1 til 4 320 minutter |
| Egendefinerte tidskilder | Opptil åtte NTP-likemenn |
| Eksport av aktivitet | Opptil 150 bevarte oppføringer |
| Språk og temaer | 20 språk, 13 temaer med aksentvarianter |

## Hva den ikke gjør

- Tømmer en TPM, sletter beskyttere den ikke har laget, eller viser gjenopprettingspassord.
- Endrer en innstilling som styres av gruppepolicy eller enhetsadministrasjon.
- Kjører en reparasjon bare fordi en historisk hendelse eller en importert rapport nevner et problem.
- Gjør manglende, delvise eller mislykkede bevis om til en dom om at alt er sunt.

## Rapporter

Hver kjøring gir en rapport du kan lese, filtrere og eksportere. En rapport skiller mellom:

1. Hva som ble undersøkt
2. Hvilket problem som ble funnet
3. Hvilke bevis som støtter funnet
4. Hvilken handling som ble foreslått eller utført
5. Hvordan resultatet ble verifisert
6. Hva som fortsatt er ukjent eller krever omstart

**Rå utdata** beholder de tekniske bevisene med de opprinnelige linjenumrene, slik at du kan spore ethvert funn tilbake til kilden. **Lesbar rapport** oppsummerer samme kjøring i enkelt språk, med det neste trygge trinnet først. HTML-rapporter bruker Killer Tools-temasystemet og er lesbare uten appen. Tekst prioriterer flyttbarhet, og JSON beholder strukturerte felt for et annet program.

## Lagrede logger og opptak

KillerBench kan undersøke bevis uten å kjøre noe på maskinen. Slipp en tekstlogg på vinduet, eller lim inn banen og velg **Åpne logg**. CBS-, CSI- og Setup-linjer tolkes med tidsstempler, og funnene lenker til den nøyaktige opprinnelige linjen. EVTX- og hendelses-XML-filer kan importeres på samme måte.

**Skann Windows Update** er en skrivebeskyttet innsamling ved behov av CBS-, DISM-, Panther- og tilbakerullingslogger fra Setup, hendelseskanaler for oppdatering og vedlikehold, tilstanden til oppdateringstjenester, ledig plass, oppetid, indikatorer for ventende omstart og valgt oppdateringspolicy. **Lagre opptak** skriver de bevarte bevisene til en `.kbench`-fil. **Åpne opptak** åpner den igjen uten de opprinnelige filene og vurderer den på nytt med de installerte reglene, slik at de samme reglene gir de samme funnene.

## Demomodus

Start `KillerBench.exe --demo` for å prøve **Kjør diagnose** på en fiktiv arbeidsstasjon. Hver arbeidsflyt gir syntetiske bevis og tidsstyrt fremdrift. Ingen Windows-leverandør kalles, ingen reparasjonskommando kjøres, og ingen journal skrives. Egne verktøyknapper utenfor **Kjør diagnose** er fortsatt aktive, så ikke bruk dem som demohandlinger.

`KillerBench.exe --demo-screenshot` åpner en rapport fra Full systemskanning for en fast fiktiv maskin, bare til visning. Klikk i arbeidsområdet, hurtigtaster og slipping av filer er blokkert.

## Kunnskapspakker

Store og ofte oppdaterte referansedata ligger i kunnskapspakker i stedet for i diagnoselogikken: feilkoder for Windows og Setup, leverandør- og enhetsidentifikatorer, PCI-, USB-, ACPI- og UEFI-referanser, leverandørdata for nettverks-OUI, plasseringer for drivere og støtte, og utvalgte støttelenker.

Hver pakke angir format, skjemaversjon, kilde, kilderevisjon, genereringstidspunkt og innholdshash, og KillerBench validerer dem før bruk. En ukjent eller ugyldig pakke forblir utilgjengelig i stedet for å falle tilbake på en gjetning. En oppdatering lastes ned til en midlertidig plassering, valideres og erstatter deretter den aktive pakken i ett trinn, så en mislykket oppdatering lar den forrige pakken bli liggende.

Kunnskapsdata kan forklare en identifikator eller foreslå hvor du bør se videre. De kan ikke gjøre en historisk kode om til en aktuell feil, velge en driver automatisk eller gi grunnlag for en reparasjon.

## Oppdateringer og signering

Ved oppstart spør KillerBench GitHub om siste stabile versjon, med en grense på fire sekunder, og tilbyr en oppdatering bare når den versjonen er nyere. Du kan slå av kontrollen med **Se etter oppdateringer ved oppstart** under Om, eller i oppdateringsmeldingen. Nedlastingen er pakken som samsvarer med kopien som kjører, installasjonsprogram eller bærbar, og SHA-256 for den må samsvare med versjonens `SHA256SUMS.txt` før noe installeres. Appen lukker og åpner seg selv på nytt for å fullføre. Det finnes ingen bakgrunnstjeneste.

Om-kortet viser versjonen, utgiveren og avtrykket for kodesigneringen, og SHA-256 for den kjørbare filen som kjører, så du kan kontrollere kopien du har.

## KillerMCP

KillerBench inneholder ingen KI-agent. [KillerMCP](../mcp.html) kan koble en til samme Engine. **KillerMCP-alternativer** under Om avgjør hva agenten kan gjøre:

| Innstilling | Virkning |
| --- | --- |
| Av (standard) | Agenten kan ikke be om en reparasjon. |
| Tillat trygge reparasjoner | Agenten kan be om reparasjoner som er klassifisert som trygge. |
| Tillat også forstyrrende reparasjoner | Forstyrrende reparasjoner blir tilgjengelige, og hver av dem krever fortsatt godkjenning. |

Bare en person ved denne datamaskinen kan heve nivået. Etter et usikkert reparasjonsresultat skriver KillerMCP en lås, og agentreparasjoner forblir blokkert til noen fjerner den under Om.

## Lokalisering

Grensesnittet er tilgjengelig på 20 språk: engelsk, bengali, tsjekkisk, tysk, spansk, fransk, ungarsk, italiensk, japansk, kasakhisk, norsk, polsk, portugisisk (Brasil), russisk, slovensk, tyrkisk, ukrainsk, vietnamesisk og forenklet og tradisjonell kinesisk. Et språkbytte gjelder umiddelbart, også for navnene på seksjoner og arbeidsflyter, velkomstteksten og en åpen lesbar rapport. Tekst som mangler, vises på engelsk.

Bevis, Engine-funn, journaler og Rå utdata forblir på engelsk, som en Windows-logg, så en rapport leses likt på alle maskiner og kan søkes i etter nøyaktig tekst.

## Kommandolinje og avslutningskoder

`killerbench-cli` kjører samme Engine uten skrivebordsappen. Fremdrift går til standard feil og JSON til standard utdata. De fleste kommandoer viser en forhåndsvisning som standard, og en endring krever `--execute`. Tilhørende `-recover`-kommandoer ser over en avbrutt handling uten å gjenta den. Se [Kjør KillerBench fra skript](use-the-cli.html) og [kommandoreferansen](cli.html).

| Avslutningskode | Arbeidsflytkommandoer | Skanning og åpning |
| --- | --- | --- |
| 0 | Ingen uløste problemer i kontrollerte omfang | Fullført dekning |
| 1 | Oppstarts- eller journalfeil | Feil |
| 2 | Ugyldige argumenter | Ugyldige argumenter |
| 3 | Krever oppmerksomhet | Delvis innsamling |
| 4 | Omstart kreves før verifisering | |
| 130 | Stoppet | Avbrutt eller samlet tidsavbrudd |

Ingen avslutningskode sertifiserer at en datamaskin er sunn.

## Arkitektur

Engine er delt i tre prosjekter uten grensesnitt. `Engine.Core` leverer primitiver og deklarative oppskrifter, `Engine` eier delte arbeidsflyter og bevis, og `Engine.Windows` registrerer de native leverandørene. Skrivebordsappen, CLI og testene er egne verter.

| Lag | Ansvar |
| --- | --- |
| Funksjonsinnsamlere | Leser avgrenset Windows-tilstand for ett nøyaktig omfang |
| Bevistolkere | Validerer form, identitet, fullstendighet og grenser |
| Diagnosetolkning | Gjør bevis om til eksplisitte arbeidsflyttilstander |
| Arbeidsflytorkestrering | Kombinerer kontroller, fremdrift, oppskrifter og journaler |
| Reparasjonskoordinatorer | Kontrollerer kvalifisering på nytt, forbereder, endrer, verifiserer og avstemmer |
| Rapporter | Presenterer problemer, handlinger, resultater og begrensninger |
| Kunnskapspakker | Slår opp versjonerte referansedata uten å endre logikken |

Innsamlerne svikter lukket når identiteten er tvetydig, utdataene er feilformet, en grense overskrides eller nødvendig myndighet ikke kan undersøkes. Engine er i dag rettet mot Windows og er ikke plattformuavhengig. [Utviklerveiledning for Engine](../engine/index.html) dekker API-et, CLI og hvordan du bygger Engine inn i en annen .NET-vert.

## Personvern og nettverk

Diagnostikk og logganalyse kjører lokalt, uten KI-tjeneste eller KillerBench-konto. Journaler og bevarte bevis blir liggende i mappene `%LOCALAPPDATA%\KillerBench` på den lokale kontoen.

Handlinger du velger, kan kontakte målet sitt, for eksempel Windows Update, nedlastingstjenester for programvare eller sikkerhetskopiering av gjenopprettingsnøkkelen til Active Directory eller Entra ID. Oppdateringskontrollen ved oppstart kontakter GitHub, og kunnskapsoppdateringer og KillerMCP-installasjonsprogrammet kontakter sine konfigurerte tjenester. En KI-klient som bruker [KillerMCP](../mcp.html), kan motta referanseresultatet den ba om.

Rå logger, importerte filer og eksporterte bevis kan inneholde datamaskinnavn, kontoer, stier eller sensitiv kildetekst. Se over dem før du deler, og ta aldri med gjenopprettingsnøkler, påloggingsinformasjon eller kundedata i en offentlig rapport.

## Utviklingsstatus

KillerBench er under utvikling, og det finnes ingen offentlig nedlasting ennå. Reparasjonsdekningen utvides fortsatt, og native validering av reparasjoner pågår fortsatt. En vellykket bygging eller simulert kontroll beviser ikke hvordan en reparasjon oppfører seg på en ekte maskin. Virtuelle engangsmaskiner med Windows brukes til å validere hver native endring på tvers av Windows-versjoner og feiltilstander. Se [Slik holdes endringer trygge](safety.html) for hele sikkerhetsmodellen.

## Ordliste

Enkle forklaringer av begrepene som brukes på denne siden, i alfabetisk rekkefølge.

| Begrep | Hva det betyr |
| --- | --- |
| Arbeidsflyt | Én kjøring av et verktøy: kontrollene det samler inn, oppskriftene det kan bruke og rapporten det lager. |
| Avstemming | Å lese fersk tilstand etter et avbrudd for å finne ut hva som skjedde, uten å gjenta endringen. |
| Bevis | Det KillerBench faktisk leste fra Windows, lagret med kilden slik at et funn kan spores tilbake. |
| BITS | Background Intelligent Transfer Service, Windows-tjenesten som laster ned oppdateringer og andre filer i bakgrunnen. |
| CBS | Component-Based Servicing, Windows-delsystemet som installerer oppdateringer og funksjoner. Loggen heter `CBS.log`. |
| DISM | Deployment Image Servicing and Management, Windows-verktøyet som kontrollerer og reparerer komponentlageret. |
| Journal | En lokal JSON-oppføring som skrives før en endring og oppdateres etterpå, slik at en avbrutt handling kan gjennomgås senere. |
| Kunnskapspakke | En versjonert, validert fil med referansedata, som feilkoder eller maskinvareidentifikatorer. |
| Lukket svikt | Når bevis er tvetydige, mangler eller er for store, blir resultatet Ukjent, og ingen endring tillates. |
| Opptak | En lagret `.kbench`-fil med bevarte bevis, slik at de kan gjennomgås på nytt uten den opprinnelige datamaskinen. |
| Oppskrift | En typet reparasjon med egne regler for kvalifisering, kjøring, verifisering og gjenoppretting. |
| Panther | Mappen der Windows Setup skriver oppgraderingsloggene sine. |
| Policyeier | Gruppepolicy eller enhetsadministrasjon som styrer en innstilling. KillerBench rører ikke innstillinger som eies slik. |
| SFC | System File Checker, Windows-verktøyet som verifiserer og gjenoppretter beskyttede systemfiler. |
| TPM | Trusted Platform Module, sikkerhetsbrikken BitLocker bruker til å beskytte stasjonsnøkkelen. |
| Utvidede rettigheter | Å kjøre med administratorrettigheter etter en Windows-forespørsel. KillerBench gir seg aldri selv utvidede rettigheter. |
| Ventende vern | Oppføringen som merker at en endring muligens er sendt til Windows. Den blokkerer gjentakelse til avstemmingen har lest utfallet. |
| Verifisert | Resultatet ble lest tilbake fra Windows og samsvarte med det endringen skulle gjøre. |
