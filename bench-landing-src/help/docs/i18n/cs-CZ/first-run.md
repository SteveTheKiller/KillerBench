# Vaše první diagnostika

Začněte stránkou [Stažení a nastavení](setup.html) a potom spusťte Úplnou kontrolu systému. Ta shromáždí diagnostické důkazy a uloží místní deník; opravy nespouští. Nález popisuje zkontrolovaný rozsah, nikoli každou možnou závadu počítače.

## Spuštění kontroly

1. Otevřete KillerBench. Otevře se sekce **Diagnostika** s vybranou **Úplnou kontrolou systému**.
2. Při prvním spuštění nechte možnost **Automaticky opravit potvrzené problémy** vypnutou.
3. Stiskněte **Spustit diagnostiku**. Během jednotlivých kontrol se průběh zobrazuje v **Surovém výstupu** vpravo.
4. Po dokončení se podokno výstupu přepne na **Čitelnou zprávu**.

Začněte bez zvýšení oprávnění. Některé důkazy Windows, například ochrany BitLocker a chráněné služby, vyžadují oprávnění správce. Pod standardním účtem tyto kontroly ohlásí nedostupné důkazy. Pokud tyto důkazy potřebujete, dokončete aktuální běh, ponechte si zprávu a potom KillerBench záměrně znovu otevřete pomocí **Spustit jako správce**. Zvýšení oprávnění nevybírá ani neschvaluje žádnou opravu.

## Čtení zprávy

Zpráva začíná počty pro **Ke kontrole**, **Ověřené opravy**, **Neúplné** a **Zkontrolováno** a potom doporučí další krok.

- **Co vyžaduje pozornost** uvádí problémy podložené důkazy, od nejdůležitějších.
- **Shromážděné informace** ukazují, co bylo přečteno, včetně důkazů, které byly neúplné nebo nedostupné.
- Historické události jsou označené jako historie. Chyba z minulého měsíce se nepovažuje za aktuální závadu.

Chybějící důkazy se nikdy nepočítají jako zdravý stav. Pokud kontrolu nebylo možné spustit, zpráva to uvede.

## Vlastní výběr kontrol

Otevřete **Vybrat kontroly (pokročilé)** a místo úplné kontroly vyberte jednotlivé kontroly. Každá sekce aplikace má také cílené nástroje, které popisují stránky **Sekce** této příručky.

## Oprava nalezených problémů

Když zpráva doporučí opravu, uvede sekci a nástroj, který máte otevřít. Před první opravou si přečtěte [Jak změny zůstávají bezpečné](safety.html). Nativní ověřování stále probíhá; úspěšná diagnostika neprokazuje, že každá nabízená oprava byla otestována na vašem sestavení Windows. Před každou změnou mějte zálohy a zkontrolujte přesný cíl a důsledky.
