# Løs nettverks- og internettproblemer

Bruk denne veiledningen når en datamaskin ikke har internett, ikke når en server eller et navn, eller stadig mister tilkoblingen. KillerBench kontrollerer adapter, adresse, gateway, DNS og brannmur i rekkefølge, og fornyer bare en lease eller starter en tjeneste når ferske bevis viser at det trengs.

## Kontroller tilkoblingen

1. Åpne **Nettverk** og velg **Nettverk**.
2. For å teste noe bestemt fyller du ut **Vertsnavnoppslag (valgfritt)**, eller **TCP-mål (valgfritt)** og **TCP-port**. La dem stå tomme for å kontrollere bare den lokale konfigurasjonen. Ingen appdata sendes.
3. Angi **Tidsavbrudd per oppslag eller tilkobling (sekunder)** hvis nettverket er tregt.
4. La **Reparer bekreftede problemer automatisk** være av, og trykk **Kjør diagnose**.
5. Les rapporten. Den skiller mellom manglende adresse, manglende eller utilgjengelig gateway, DNS-konfigurasjonsproblemer, proxyinnstillinger, avvik i brannmurprofilen og et mislykket oppslag eller en mislykket tilkobling, og gir neste trinn for hvert tilfelle.

Svar fra gatewayen beviser ikke internettilgang, og et mislykket oppslag gjennom den konfigurerte resolveren betyr ikke at nettstedet er nede.

## Reparer det rapporten fant

1. Kjør **Nettverk** på nytt med **Reparer bekreftede problemer automatisk** slått på. Den kan starte de aktiverte tjenestene DNS-klient, DHCP-klient, SMB-klient, Nettverksplasseringsbevissthet, Nettverkslistetjeneste og IP-hjelper, og fornye tilkoblede fysiske DHCP-adaptere som mangler en brukbar IPv4-adresse.
2. Regn med at tilkoblingen faller ut et øyeblikk under en fornyelse. Statiske adresser og virtuelle adaptere endres aldri.
3. For ett enkelt feil navn i hurtigbufferen bruker du tømming av DNS-hurtigbufferen for nøyaktig den verten i stedet for å tømme alt.

Ruter, Hosts-oppføringer, DNS-servere, brannmurregler, Winsock-leverandører og nettverksprofiler blir alltid bevart.

## Trådløst, kablet pålogging og proxyer

- **Trådløst nettverk** kontrollerer adapteren og WLAN AutoConfig, og kan starte tjenesten hvis den er stoppet.
- **Kablet godkjenning (802.1X)** gjør det samme for Wired AutoConfig på nettverk som krever pålogging ved porten.
- **WinHTTP Auto-Proxy** viser lagrede proxyinnstillinger for maskinen og gjeldende konto. KillerBench henter aldri en PAC-URL og endrer aldri proxyinnstillinger under diagnose.

## Fra kommandolinjen

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Se [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) og [dns-cache](cli.html#cmd-dns-cache).

## Relatert

- [Seksjonen Nettverk](network.html)
- [Løs utskriftsproblemer](fix-printing.html)
- [Slik holdes endringer trygge](safety.html)
