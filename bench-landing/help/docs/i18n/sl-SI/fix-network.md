# Odpravite težave z omrežjem in internetom

Uporabite, ko računalnik nima interneta, ne more doseči strežnika ali imena oziroma nenehno izgublja povezavo. KillerBench po vrsti preveri vmesnik, naslov, prehod, DNS in požarni zid ter obnovi zakup ali zažene storitev samo, ko sveži dokazi potrdijo, da je to potrebno.

## Preverite povezavo

1. Odprite **Omrežje** in izberite **Omrežje**.
2. Za določen preizkus izpolnite **Poizvedba po imenu gostitelja (izbirno)** ali **Cilj TCP (izbirno)** in **Vrata TCP**. Za pregled samo lokalnih nastavitev jih pustite prazna. Podatki aplikacij se ne pošiljajo.
3. Če je omrežje počasno, nastavite **Časovna omejitev posamezne poizvedbe ali povezave (sekunde)**.
4. Pustite **Samodejno popravi potrjene težave** izklopljeno in pritisnite **Zaženi diagnostiko**.
5. Preberite poročilo. Loči manjkajoči naslov, manjkajoči ali nedosegljivi prehod, težave z nastavitvami DNS, nastavitve posrednika, neskladja profilov požarnega zidu in neuspešno poizvedbo ali povezavo ter za vsako navede naslednji korak.

Odgovor prehoda ne dokazuje dostopa do interneta, neuspešna poizvedba prek nastavljenega razreševalnika pa ne pomeni, da spletno mesto ne deluje.

## Popravite ugotovitve poročila

1. Znova zaženite **Omrežje** z vklopljeno možnostjo **Samodejno popravi potrjene težave**. Lahko zažene omogočene storitve DNS Client, DHCP Client, odjemalca SMB, Network Location Awareness, Network List in IP Helper ter obnovi zakup priključenih fizičnih vmesnikov DHCP brez uporabnega naslova IPv4.
2. Med obnovitvijo pričakujte kratko prekinitev povezave. Statični naslovi in navidezni vmesniki se nikoli ne spremenijo.
3. Za eno napačno predpomnjeno ime uporabite čiščenje predpomnilnika DNS za točno tega gostitelja namesto čiščenja vsega.

Poti, vnosi Hosts, strežniki DNS, pravila požarnega zidu, ponudniki Winsock in omrežni profili se vedno ohranijo.

## Brezžično omrežje, prijava v žično omrežje in posredniki

- **Brezžično omrežje** preveri vmesnik in WLAN AutoConfig ter lahko zažene storitev, če je ustavljena.
- **Preverjanje pristnosti v žičnem omrežju (802.1X)** naredi enako za Wired AutoConfig v omrežjih, ki zahtevajo prijavo na vratih.
- **Samodejni posrednik WinHTTP** pokaže shranjene nastavitve posrednika za računalnik in trenutni račun. KillerBench med diagnostiko nikoli ne pridobi naslova URL PAC ali spremeni nastavitev posrednika.

## Iz ukazne vrstice

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Glejte [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) in [dns-cache](cli.html#cmd-dns-cache).

## Povezano

- [Razdelek Omrežje](network.html)
- [Odpravite težave s tiskanjem](fix-printing.html)
- [Kako spremembe ostanejo varne](safety.html)
