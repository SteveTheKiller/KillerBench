# Nastavení nového počítače

Tento postup použijte při přípravě nového nebo čerstvě resetovaného počítače. KillerBench zkontroluje počítač, aplikace, které plánujete nainstalovat, a název, který mu chcete dát, a potom odkáže na chráněné nástroje, které jednotlivé změny provedou.

## Začněte výchozím stavem

1. Spusťte **Úplná kontrola systému** ze sekce **Diagnostika** a zprávu si ponechte jako záznam o výchozím stavu počítače.
2. Nainstalujte čekající aktualizace. Pokud některé selžou, viz [Oprava služby Windows Update](fix-windows-update.html).

## Kontrola aplikací a konfigurace

1. Otevřete **Zprovoznění** a zvolte **Zprovoznění**.
2. Vyberte aplikace a skupiny konfigurace, které plánujete nastavit, a stiskněte **Spustit diagnostiku**. Kontrola je porovná s tím, co je již nainstalováno. KillerBench instalační programy nestahuje ani nespouští.
3. Před spuštěním instalačního programu dodavatele jej ověřte z příkazového řádku. KillerBench mu důvěřuje jen tehdy, když nese platný podpis s přesným názvem organizace daného dodavatele.

## Pojmenování počítače

1. Zvolte **Název počítače**, zadejte název a stisknutím **Spustit diagnostiku** jej zkontrolujte vůči aktuální identitě a roli v doméně.
2. Na počítači v pracovní skupině jej přejmenujete zaškrtnutím **Přejmenovat tento počítač v pracovní skupině po nových kontrolách**. K dokončení je nutný restart.

## Ochrana a úklid

- [Zašifrujte jednotku nástrojem BitLocker](encrypt-with-bitlocker.html) a zálohujte klíč pro obnovení.
- [Zvyšte soukromí ve Windows](make-windows-private.html) a v sekci **Údržba** projděte **Pročištění Windows**.
- Až bude vše připraveno, vytvořte bod obnovení pomocí **Připravit jeden bod obnovení**.

## Z příkazového řádku

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Viz [workflow](cli.html#cmd-workflow) a [restore-point](cli.html#cmd-restore-point).

## Související

- [Sekce Zprovoznění](onboarding.html)
- [Vaše první diagnostika](first-run.html)
