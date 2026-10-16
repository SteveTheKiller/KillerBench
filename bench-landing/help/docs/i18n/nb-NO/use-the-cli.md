# Kjør KillerBench fra skript

Bruk kommandolinjen når du vil ha KillerBench-bevis i et skript, en RMM-jobb eller en ekstern konsoll. `killerbench-cli` kjører samme Engine som skrivebordsappen og skriver ut JSON.

## Kom i gang

1. Åpne PowerShell der `killerbench-cli.exe` ligger, eller legg mappen til i banen din.
2. Kjør `killerbench-cli help` for å vise alle kommandoene med sikkerhetsgrensen deres.
3. Kjør `killerbench-cli diagnostic-scopes` for å se de 11 anbefalte diagnosegruppene.

## Kjør en skanning og lagre en rapport

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` er skrivebeskyttet med mindre du legger til `--auto-repair`. `--report` skriver den samme temabaserte rapporten som appen eksporterer. Bruk `--scope` for å kontrollere bare noen områder, og `common-check` for samlet JSON uten arbeidsflytjournal.

## Les avslutningskoder

| Kode | Betydning |
| --- | --- |
| 0 | Forhåndsvisningen eller det verifiserte resultatet lyktes. |
| 1 | En oppstarts-, inndata- eller journalfeil stoppet kommandoen. |
| 2 | Argumentene ble ikke gjenkjent. |
| 3 | Blokkert, mislykket eller ikke verifisert. |
| 4 | En arbeidsflyt trenger en omstart før den kan verifisere. |
| 130 | Du stoppet den. |

Avslutningskode 0 på en skrivebeskyttet kommando betyr at den kjørte, ikke at datamaskinen er sunn. Les JSON-dataene for funn.

## Gjør en endring

Alle kommandoer som kan endre maskinen, viser en forhåndsvisning som standard. Les forhåndsvisningen, og kjør deretter samme kommando med `--execute`. Hvis en forespørsel blir avbrutt, kjører du den tilhørende `-recover`-kommandoen; den melder det faktiske utfallet og gjentar aldri endringen.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Åpne gamle resultater på nytt

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Bruk den fra en KI-assistent

[KillerMCP](../mcp.html) kan kalle den installerte CLI-en for å slå opp feilkoder fra Enhetsbehandling og Windows.

## Relatert

- [Referanse for kommandolinjen](cli.html)
- [Utviklerveiledning for Engine](../engine/index.html)
- [Feilsøking](troubleshooting.html)
