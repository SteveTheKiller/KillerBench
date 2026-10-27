# Větší soukromí ve Windows

Tento postup použijte ke kontrole a zpřísnění soukromí na notebooku. KillerBench přečte aktuální nastavení, ukáže, která z nich již vlastní zásady nebo správa zařízení, a změní jen nastavení, která vyberete, jedno po druhém a s přesným vrácením zpět.

## Kontrola aktuálního nastavení

1. Otevřete **Soukromí**.
2. Zaškrtněte oblasti, které chcete zkontrolovat. Žádná oblast není na začátku zaškrtnutá.
3. Stiskněte **Spustit diagnostiku**. Zpráva zobrazí aktuální hodnotu pro tento počítač a účet, pod kterým KillerBench běží, a označí nastavení vlastněná zásadami.

## Změna nastavení

1. V poli **Nastavení soukromí** zvolte nastavení, například kameru, mikrofon, polohu, diagnostická data, reklamu, historii schránky, cloudové vyhledávání obsahu nebo zvýraznění vyhledávání.
2. Zvolte novou hodnotu a zaškrtněte **Použít, když vyberu Spustit diagnostiku**.
3. Stiskněte **Spustit diagnostiku**. KillerBench zkontroluje vlastníka zásad, zaznamená aktuální hodnotu, provede jedinou změnu a znovu ji přečte.

Nastavení vlastněná zásadami skupiny nebo správou zařízení zůstávají beze změny. Změňte je tam, kde jsou spravována.

## Vrácení změny

Každá ověřená změna má svůj deník. Odpovídající vrácení zpět obnoví přesnou předchozí hodnotu, opět po kontrole zásad.

## Pročištění Windows

Pro návrhy, spotřebitelský obsah, Recall a funkce AI, nepořádek na hlavním panelu a v nabídce Start, Edge a předinstalovaný software otevřete **Údržba** a zvolte **Pročištění Windows**. Kontrola je pouze pro čtení a nic se nevybírá automaticky.

## Z příkazového řádku

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Viz [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) a [debloat-review](cli.html#cmd-debloat-review). Nepodporovaný klíč vypíše podporované volby.

## Související

- [Sekce Soukromí](privacy.html)
- [Nastavení nového počítače](set-up-new-pc.html)
