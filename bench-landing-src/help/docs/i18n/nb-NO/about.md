# Om KillerBench

KillerBench er en arbeidsbenk for diagnose, reparasjon og vedlikehold av Windows laget av Steve the Killer, en del av [Killer Tools-familien](https://killertools.net). Versjon 1.0.0 er ikke utgitt. Skrivebordsappen, Engine og CLI deler diagnosebevis og støttede reparasjonsflyter.

## Tilgjengelighet og omfang

Ingen offentlig KillerBench-nedlasting eller Engine-pakke er tilgjengelig ennå. [Nedlasting og oppsett](setup.html) beskriver gjeldende Windows x64-mål og kravene til Desktop Runtime. Native validering av reparasjoner pågår fortsatt; at et verktøy finnes, beviser ikke at alle feil, avbrudd eller Windows-konfigurasjoner er testet.

## Merknader om kildekode og distribusjon

KillerBench er lisensiert under **GPLv3**. Distribusjonslisensen er [GNU General Public License versjon 3](../license.txt). Lisensen beskriver tillatelser og vilkår for å kopiere, endre og distribuere det dekkede verket.

Det offentlige [KillerBench-repositoriet](https://github.com/SteveTheKiller/KillerBench) inneholder foreløpig prosjektinformasjon. Den fullstendige implementeringen er ikke offentlig tilgjengelig der. Disse utviklerveiledningene beskriver en autorisert kildekodekopi; de gir ingen offentlig nedlasting av kildekoden.

Før en binærfil distribueres, må utgiveren gjøre tilhørende kildekode tilgjengelig på en måte som er tillatt etter [GPLv3 del 6](https://www.gnu.org/licenses/gpl-3.0.en.html#section6), inkludert kildekoden og byggematerialet lisensen krever. Instruksjonene for levering av kildekode og de medfølgende tredjepartsmerknadene for den første offentlige pakken er fortsatt ventende. GPLv3 gir ikke tillatelse til å videredistribuere tredjepartsskrifter; gjeldende skrifttillatelser må fortsatt verifiseres. Se over lisensen, kildekodeinstruksjonene og merknadene i den publiserte pakken før du videredistribuerer den.

## Personvern og eksterne tjenester

Diagnostikk og logganalyse kjører lokalt uten KI-tjeneste eller KillerBench-konto. Journaler og bevarte bevis bruker mappene `%LOCALAPPDATA%\KillerBench` på den lokale kontoen. Valgte handlinger kan kontakte Windows Update, nedlastingstjenester for programvare, kataloger eller andre navngitte mål. Oppdateringskontrollen ved oppstart kontakter GitHub, og kunnskapsoppdateringer og installasjonssiden for MCP kontakter sine konfigurerte tjenester. En KI-klient som bruker MCP, kan motta referanseresultatet som returneres.

Dette nettstedet lagrer valg av tema og aksentfarge i nettleserens lokale lagring. Veiledningene kjører ingen diagnose på datamaskinen din. Rå logger, importerte filer og eksporterte bevis kan inneholde datamaskinnavn, kontoer, stier eller sensitiv kildetekst. Se over dem før du deler; ikke ta med gjenopprettingsnøkler, påloggingsinformasjon eller kundedata i en offentlig rapport.

## Støtte

Prosjektinformasjon finnes i det offentlige repositoriet. For støtte bruker du kontaktinformasjonen som er publisert på [forfatterens nettsted](https://thekiller.net); oppgi appversjon, Windows-versjon, nøyaktig verktøy, forventet resultat og observert resultat. Se over og anonymiser bevis før du sender dem, og bruk en godkjent privat kanal for kundeinformasjon. Ta vare på journalene når utfallet av en reparasjon er usikkert.

## Relaterte veiledninger

- [Slik holdes endringer trygge](safety.html)
- [Rapporter og bevis](reports.html)
- [Utviklerveiledning for Engine](../engine/)
- [KillerMCP](../mcp.html)
