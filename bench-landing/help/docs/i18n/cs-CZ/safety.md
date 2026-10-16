# Jak změny zůstávají bezpečné

KillerBench považuje diagnostiku a změnu za dvě odlišné úlohy. Diagnostika pouze čte. Úplná kontrola systému nikdy nespouští opravy. Vyberte konkrétní opravu nebo akci, zkontrolujte její možnosti a předpoklady a navrženou změnu potvrďte.

## Před změnou

1. **Čerstvé důkazy.** Nástroj znovu přečte aktuální stav. Uložená zpráva ani stará událost nikdy neopravňují ke změně.
2. **Přesná způsobilost.** Změna je nabídnuta jen tehdy, když důkazy odpovídají přesně tomu problému, který opravuje. Cokoli neúplného ji zablokuje.
3. **Vaše potvrzení.** Zkontrolujte vybranou akci v jejím potvrzovacím dialogu. Některé akce, například dešifrování jednotky, vyžadují druhé potvrzení. Dialog se řídí aktivním motivem aplikace; jeho vzhled nemění požadavky akce.
4. **Předpoklady.** Kontrolují se oprávnění správce, stav restartu a vlastnictví zásadami nebo správou zařízení. Nastavení, která vlastní zásady, zůstávají beze změny.
5. **Čekající záznam.** KillerBench si zapíše, co se chystá udělat, dříve než o to požádá Windows.
6. **Závěrečná kontrola.** Vše se znovu zkontroluje těsně před požadavkem, takže ho mezitím nastalá změna zastaví.

## Po změně

- KillerBench výsledek znovu přečte a hlásí **Ověřeno** jen tehdy, když Windows ukazuje očekávaný stav.
- Pokud Windows vrátí úspěch, ale stav se nezměnil, výsledek to uvede a nic se neopakuje.
- Každá změna se ukládá do deníku v `%LOCALAPPDATA%\KillerBench`.

## Když se něco přeruší

Podporované akce s deníkem si ponechají čekající záznam, pokud požadavek mohl dorazit do Windows a ověření se nedokončilo. Ke kontrole tohoto záznamu použijte zdokumentovaný postup sladění dané akce nebo odpovídající příkaz `-recover`. Sladění není automatické vrácení změn. Pokud je výsledek nejistý, deník si ponechte a změnu ručně neopakujte. Chování při pádu, zrušení a pozdějším spuštění systému stále vyžaduje nativní ověření pro každou akci.

## Co KillerBench neudělá

- Nevymaže TPM, neodstraní ochrany, které sám nevytvořil, a nezobrazí hesla pro obnovení.
- Nezmění nastavení, které řídí zásady skupiny nebo správa zařízení.
- Nespustí opravu jen proto, že historická událost nebo importovaná zpráva zmiňuje problém.

Diagnostika a analýza zpráv probíhají místně. Akce, kterou vyberete, může kontaktovat externí cíl, včetně stahování softwaru nebo zálohování klíče pro obnovení do Active Directory nebo Entra ID. Před potvrzením cíl zkontrolujte. Uložené záznamy a surové důkazy mohou obsahovat identitu počítače a zdrojové cesty, proto je před sdílením zkontrolujte.

## Stav vývoje

Rozsah oprav se stále rozšiřuje. Úspěšné sestavení nebo simulovaná kontrola neprokazuje nativní chování oprav. Soubor `VM-VALIDATION.md` v úložišti sleduje scénáře na jednorázových počítačích s Windows a zbývající ověřování.
