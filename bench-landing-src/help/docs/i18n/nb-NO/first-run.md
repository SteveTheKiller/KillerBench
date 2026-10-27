# Din første diagnose

Begynn med [Nedlasting og oppsett](setup.html), og kjør deretter en Full systemskanning. Den samler diagnosebevis og lagrer en lokal journal; den kjører ingen reparasjoner. Et funn beskriver det kontrollerte omfanget, ikke alle mulige feil på datamaskinen.

## Kjør skanningen

1. Åpne KillerBench. Seksjonen **Diagnostikk** åpnes med **Full systemskanning** valgt.
2. La **Reparer bekreftede problemer automatisk** være slått av første gang.
3. Trykk **Kjør diagnose**. Fremdriften vises i **Rå utdata** til høyre mens hver kontroll kjører.
4. Når den er ferdig, bytter utdatapanelet til **Lesbar rapport**.

Start uten utvidede rettigheter. Noen Windows-bevis, som BitLocker-beskyttere og beskyttede tjenester, krever administratorrettigheter. Under en standardkonto melder disse kontrollene at beviset ikke er tilgjengelig. Hvis du trenger det, fullfører du kjøringen, tar vare på rapporten og åpner KillerBench på nytt med **Kjør som administrator**. Utvidede rettigheter velger eller godkjenner ingen reparasjon.

## Les rapporten

Rapporten begynner med antall for **Må gjennomgås**, **Verifiserte reparasjoner**, **Ufullstendige** og **Kontrollert**, og deretter et anbefalt neste trinn.

- **Hva som trenger oppmerksomhet** viser problemene bevisene støtter, de viktigste først.
- **Innsamlet informasjon** viser hva som ble lest, inkludert bevis som var ufullstendige eller utilgjengelige.
- Historiske hendelser merkes som historikk. En feil fra forrige måned behandles ikke som en aktuell feil.

Manglende bevis regnes aldri som sunt. Hvis en kontroll ikke kunne kjøre, sier rapporten det.

## Velg kontroller selv

Åpne **Velg kontroller (avansert)** for å velge enkeltkontroller i stedet for full skanning. Hver seksjon i appen har også egne verktøy, beskrevet på sidene under **Seksjoner** i denne veiledningen.

## Reparer det du fant

Når rapporten anbefaler en reparasjon, oppgir den hvilken seksjon og hvilket verktøy du skal åpne. Les [Slik holdes endringer trygge](safety.html) før din første reparasjon. Den native valideringen pågår fortsatt; en vellykket diagnose betyr ikke at hver tilbudt reparasjon er testet på din Windows-versjon. Ta sikkerhetskopier og se over det nøyaktige målet og konsekvensene før enhver endring.
