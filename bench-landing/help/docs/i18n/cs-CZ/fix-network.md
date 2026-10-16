# Řešení problémů se sítí a internetem

Tento postup použijte, když počítač nemá internet, nemůže se spojit se serverem nebo přeložit název, nebo opakovaně ztrácí připojení. KillerBench postupně zkontroluje adaptér, adresu, bránu, DNS a bránu firewall a zapůjčení obnoví nebo službu spustí jen tehdy, když čerstvé důkazy prokážou, že je to potřeba.

## Kontrola připojení

1. Otevřete **Síť** a zvolte **Síť**.
2. Chcete-li otestovat něco konkrétního, vyplňte **Vyhledávání názvu hostitele (volitelné)** nebo **Cíl TCP (volitelné)** a **Port TCP**. Pokud je necháte prázdné, zkontroluje se jen místní konfigurace. Neodesílají se žádná data aplikací.
3. Pokud je síť pomalá, nastavte **Časový limit na vyhledávání nebo připojení (sekundy)**.
4. Nechte **Automaticky opravit potvrzené problémy** vypnuté a stiskněte **Spustit diagnostiku**.
5. Přečtěte si zprávu. Rozlišuje chybějící adresu, chybějící nebo nedosažitelnou bránu, problémy s konfigurací DNS, nastavení proxy, nesoulad profilů brány firewall a neúspěšné vyhledávání nebo připojení a ke každému uvádí další krok.

Odpověď brány neprokazuje přístup k internetu a neúspěšné vyhledávání přes nakonfigurovaný překladač neznamená, že web nefunguje.

## Oprava toho, co zpráva našla

1. Spusťte **Síť** znovu se zapnutou možností **Automaticky opravit potvrzené problémy**. Může spustit povolené služby Klient DNS, Klient DHCP, klient SMB, Rozpoznávání umístění v síti, Seznam sítí a Pomocná služba IP a obnovit připojené fyzické adaptéry DHCP, které nemají použitelnou adresu IPv4.
2. Počítejte s tím, že se připojení během obnovení krátce přeruší. Statické adresy a virtuální adaptéry se nikdy nemění.
3. U jednoho chybného názvu v mezipaměti použijte vyprázdnění mezipaměti DNS pro daného hostitele, místo abyste mazali vše.

Trasy, záznamy Hosts, servery DNS, pravidla brány firewall, zprostředkovatelé Winsock a síťové profily se vždy zachovávají.

## Bezdrátové sítě, kabelové přihlášení a proxy

- **Bezdrátové sítě** kontroluje adaptér a Automatickou konfiguraci WLAN a může službu spustit, pokud je zastavená.
- **Kabelové ověřování (802.1X)** dělá totéž pro Automatickou konfiguraci kabelové sítě v sítích, které vyžadují přihlášení na portu.
- **Automatický proxy server WinHTTP** zobrazuje uložená nastavení proxy pro počítač a aktuální účet. KillerBench během diagnostiky nikdy nestahuje adresu URL souboru PAC ani nemění nastavení proxy.

## Z příkazového řádku

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Viz [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) a [dns-cache](cli.html#cmd-dns-cache).

## Související

- [Sekce Síť](network.html)
- [Řešení problémů s tiskem](fix-printing.html)
- [Jak změny zůstávají bezpečné](safety.html)
