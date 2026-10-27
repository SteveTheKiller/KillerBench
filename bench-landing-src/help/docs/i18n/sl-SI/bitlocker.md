# BitLocker

BitLocker pregleda šifriranje pogonov, zaščitnike in obnovitvene podatke ter vam pomaga ustvariti varnostno kopijo, zaščititi ali spremeniti pogon. Deluje le v tem računalniku.

Navodila po korakih: [Šifriranje pogona z BitLockerjem](encrypt-with-bitlocker.html).

## Uporaba orodja BitLocker

Izberite črko pogona in **Opravilo**, nato pritisnite **Zaženi diagnostiko**.

- **Preglej** prebere stanje šifriranja, stanje zaščite in identitete zaščitnikov. To je privzeta možnost in ničesar ne spremeni.
- **Varnostno kopiraj podatke za obnovitev** zapiše izbrano obnovitveno geslo v novo datoteko zunaj zaščitenega pogona ter jo preveri. Najprej pritisnite **Poišči zaščitnike za obnovitev**, izberite zaščitnika in nato potrdite varnostno kopiranje.
- **Šifriraj** varnostno kopira izbrano obnovitveno geslo, preveri kopijo in nato enkrat zažene šifriranje z izbrano metodo. Pogon sistema Windows potrebuje zaščitnika TPM in obnovitveno geslo. Podatkovni pogon potrebuje le obnovitveno geslo.
- **Nadaljuj zaščito** znova vklopi zaščito popolnoma šifriranega pogona, na katerem je bila začasno ustavljena.

Vsako od teh dejanj zahteva lastno potrditveno polje, KillerBench pa neposredno pred izvedbo znova preveri pogon.

## Več dejanj za BitLocker

Za spremembe zunaj možnosti **Zaženi diagnostiko** odprite **Več dejanj za BitLocker**. Izberite dejanje, pritisnite **Predogled**, da preberete njegove natančne zahteve, nato potrdite in pritisnite **Izvedi dejanje**.

- **Vklopi samodejno odklepanje** omogoči, da se šifrirani podatkovni pogon odklene ob zagonu sistema Windows. Pogon sistema Windows mora biti popolnoma šifriran z vklopljeno zaščito.
- **Dodaj zaščitnik TPM** ali **Dodaj obnovitveno geslo** ustvari enega novega zaščitnika. KillerBench novega obnovitvenega gesla nikoli ne prikaže ali zabeleži, zato ga takoj varnostno kopirajte.
- **Varnostno kopiraj obnovitveno geslo v Active Directory** ali **Microsoft Entra ID** pošlje zgoraj izbranega zaščitnika. KillerBench sporoči, da je Windows sprejel zahtevo. Kopije v imeniku ne more prebrati, zato jo preverite tam.
- **Dešifriraj ta pogon** zažene dešifriranje, na primer pred ponovnim šifriranjem z zahtevano metodo. Zahteva drugo potrditev, ker pogon nato ni zaščiten, in zavrne pogon sistema Windows, dokler se drugi pogoni samodejno odklepajo z njegovo pomočjo.

## TPM in FIPS

Če je TPM prisoten, vendar ni pripravljen, ga lahko razdelek Varnost pripravi brez čiščenja. KillerBench nikoli ne počisti TPM, ker bi s tem uničil ključe, ki jih uporabljata BitLocker in Windows Hello. Stikalo pravilnika algoritmov FIPS je na voljo kot izrecno, razveljavljivo popravilo za organizacije, ki potrebujejo kode PIN za BitLocker, odobrene po FIPS.

## Orodja v tem razdelku

Preglejte šifriranje, zaščitnike in obnovitvene podatke, nato jih varnostno kopirajte ali zaščitite pogon.

### BitLocker

Prikaže predogled možnosti BitLocker in pregleda izbrani nosilec. Ta postopek ne izvede šifriranja ali varnostnega kopiranja obnovitvenih podatkov.
