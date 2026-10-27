# Pripravite nov računalnik

Uporabite pri pripravi novega ali sveže ponastavljenega računalnika. KillerBench pregleda računalnik, aplikacije, ki jih nameravate namestiti, in predlagano ime, nato vas usmeri na zaščitena orodja za vsako spremembo.

## Začnite z izhodiščnim stanjem

1. V **Diagnostiki** izvedite **Celoten pregled sistema** in shranite poročilo kot zapis začetnega stanja računalnika.
2. Namestite čakajoče posodobitve. Če katera ne uspe, glejte [Popravite Windows Update](fix-windows-update.html).

## Preglejte aplikacije in nastavitve

1. Odprite **Začetna priprava** in izberite **Začetna priprava**.
2. Izberite aplikacije in skupine nastavitev, ki jih nameravate pripraviti, ter pritisnite **Zaženi diagnostiko**. Pregled jih primerja z že nameščenim. KillerBench ne prenaša ali zaganja namestitvenih programov.
3. Pred zagonom namestitvenega programa ponudnika ga preverite iz ukazne vrstice. KillerBench mu zaupa samo, če ima veljaven podpis s točnim imenom organizacije tega ponudnika.

## Poimenujte računalnik

1. Izberite **Ime računalnika**, vnesite ime in pritisnite **Zaženi diagnostiko**, da ga preverite glede na trenutno identiteto in vlogo v domeni.
2. Na računalniku v delovni skupini označite **Po ponovnem preverjanju preimenuj ta računalnik v delovni skupini**. Za dokončanje je potreben ponovni zagon.

## Zaščitite in uredite ga

- [Šifrirajte pogon z BitLockerjem](encrypt-with-bitlocker.html) in varnostno kopirajte obnovitveni ključ.
- [Izboljšajte zasebnost v sistemu Windows](make-windows-private.html) in v **Vzdrževanju** preglejte **Odstranitev navlake iz sistema Windows**.
- Ko je vse pripravljeno, ustvarite obnovitveno točko z možnostjo **Pripravi eno obnovitveno točko**.

## Iz ukazne vrstice

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Glejte [workflow](cli.html#cmd-workflow) in [restore-point](cli.html#cmd-restore-point).

## Povezano

- [Razdelek Začetna priprava](onboarding.html)
- [Vaša prva diagnostika](first-run.html)
