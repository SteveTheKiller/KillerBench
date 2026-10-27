# Údržba

Údržba udržuje počítač v pořádku: úklid souborů, staré uživatelské profily, naplánované úlohy a pročištění Windows. Každý nástroj před smazáním čehokoli zobrazí náhled toho, co by odebral.

Návody krok za krokem: [Uvolnění místa na disku](free-disk-space.html) a [Větší soukromí ve Windows](make-windows-private.html).

## Nástroje v této sekci

Ukliďte soubory, profily a naplánované úlohy a pročistěte Windows.

### Úklid

Odhaduje vybrané cíle dočasných souborů, mezipamětí a uchovaných diagnostických důkazů podle nastavení stáří a limitu souborů v hlavním panelu. Windows Temp a Windows SystemTemp jsou samostatné cíle. Optimalizace doručení je samostatná volba mezipaměti a odebraný obsah může být nutné stáhnout znovu. Volitelné trvalé odebrání používá samostatné zaškrtávací políčko úklidu, znovu kontroluje uchované kandidáty v rámci vybraného limitu a zaznamenává každý výsledek. Mazání obchází Koš. Cíle, stáří a limity souborů lze nastavit také příkazem CLI pro úklid.

### Profily

Kontroluje profily Windows podle vybrané hranice stáří, přesných registrací ProfileList a volitelných závislostí účtů služeb a naplánovaných úloh a potom vrátí jeden ohraničený závěr kontroly. Načtené, systémové profily a profil aktuálního účtu jsou z kontroly vyloučené. Kandidáti zůstávají pouze ke kontrole. Žádný profil, složka, klíč registru, účet, služba, úloha, přihlašovací údaj, zásada ani soubor se nemění a nevybírá se žádné odebrání. Závislosti aplikací a obnovitelnost záloh zůstávají neověřené.

### Naplánované úlohy

Inventarizuje viditelné naplánované úlohy, jejich stavy a hlášené objekty zabezpečení. Žádné úlohy se nespouštějí, neregistrují ani nemění. Akce, aktivační události a úspěšnost spuštění se neověřují.

### Pročištění Windows

Kontroluje výslovně vybrané oblasti úklidu Windows, například Recall a AI, telemetrii, návrhy vyhledávání, spotřebitelský obsah, hlavní panel a nabídku Start, oznámení, Edge, napájení, hibernaci, jazyky Office a předinstalovaný software pro aktuální účet nebo všechny profily. Kontrola je pouze pro čtení. Vysoký výkon napájení, časovače spánku a displeje a hibernaci lze změnit, pokud je daná oblast jedinou volbou, s čerstvými důkazy a výslovným potvrzením.
