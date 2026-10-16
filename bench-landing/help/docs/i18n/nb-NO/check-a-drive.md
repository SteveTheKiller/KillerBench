# Kontroller og reparer en stasjon

Bruk denne veiledningen når et volum melder feil, Windows ber om å skanne en stasjon, eller filer ikke vil åpnes. KillerBench kontrollerer volumet skrivebeskyttet først, og planlegger eller kjører en filsystemreparasjon bare etter at du har bekreftet en sikkerhetskopi.

## Før du begynner

- Ta sikkerhetskopi av viktige data på volumet. En filsystemreparasjon kan endre eller fjerne skadde filer.
- Kjør KillerBench som administrator.

## Kontroller volumet

1. Åpne **Lagring** og velg **Lagring**.
2. Velg stasjonen i **Volum som skal kontrolleres** og trykk **Kjør diagnose**.
3. Les rapporten. Den viser disk- og volumtilstand, dirty-biten, NTFS-selvreparasjon på nett, feil på kontroller og diskenheter, pålitelighetstellere og nylige lagringshendelser som tilbakestillinger, nye forsøk og uventet fjerning.

En ren kontroll betyr at ingen reparasjon tilbys. Disktemperatur, slitasje og tellere for ukorrigerte I/O-feil er bevis som skal gjennomgås, og utløser ikke en reparasjon på egen hånd.

## Reparer filsystemet

1. Trykk **Gjennomgå filsystemreparasjon** og velg **Punktreparasjon** eller **Frakoblet skanning og reparasjon**.
2. For et datavolum merker du av **La et volum som ikke er systemvolum, gå frakoblet** hvis du kan lukke alt som bruker det. Windows-volumet kan ikke kobles fra, så reparasjonen av det planlegges i stedet til neste omstart.
3. Merk av **Viktige data på dette volumet er sikkerhetskopiert**.
4. Bekreft reparasjonen. KillerBench kontrollerer det nøyaktige volumet på nytt rett før den handler, og registrerer først en ventende oppføring.
5. For en planlagt reparasjon starter du på nytt når det passer. KillerBench verifiserer resultatet etter omstarten uten å kjøre reparasjonen på nytt.

## Optimaliser eller kjør Retrim

Samme verktøy kan analysere volumet og kjøre optimaliseringen Windows anbefaler, **Kjør Retrim på SSD-lagring** eller en defragmentering av harddisken. Hver av dem er en egen bekreftet handling på ett nøyaktig, sunt volum.

## Fra kommandolinjen

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Se [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) og [volume-optimize](cli.html#cmd-volume-optimize).

## Relatert

- [Seksjonen Lagring](storage.html)
- [Frigjør diskplass](free-disk-space.html)
- [Feilsøking](troubleshooting.html)
