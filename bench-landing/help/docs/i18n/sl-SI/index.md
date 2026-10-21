# Pomoč za KillerBench

KillerBench je delovno okolje za diagnostiko in popravila sistema Windows za tehnike. Najprej preveri računalnik, pojasni ugotovitve z dokazi za vsako od njih in spremeni Windows samo, ko izberete določen popravek in ga potrdite.

KillerBench je v razvoju. Javni prenos še ni na voljo. Te strani opisujejo trenutno različico.

## Kaj počne

- Izvaja diagnostiko samo za branje za Windows Update, naprave, shrambo, omrežja, tiskanje, varnost, aplikacije in profile ter nato ustvari pregledno poročilo.
- Ponuja zaščitena popravila, ki se izvedejo samo, ko sveži dokazi potrdijo točno težavo, nato pa preveri rezultat.
- Zajema pripravo in varnostno utrjevanje sistema, kot so BitLocker, nastavitve zasebnosti, odstranjevanje navlake iz sistema Windows in začetna priprava novega računalnika.
- Diagnostiko in analizo poročil izvaja lokalno brez umetne inteligence ali računa KillerBench. Izbrana dejanja, kot so prenosi in varnostno kopiranje obnovitvenega ključa v Entra ID, lahko vzpostavijo povezavo z zunanjimi storitvami.
- Uporablja dejavno temo aplikacije za konzolo, poročila, potrditve ter pogovorna okna Odpri, Shrani in Mapa.

## Kje začeti

- [Prenos in namestitev](setup.html)
- [O aplikaciji KillerBench](about.html)

- [Vaša prva diagnostika](first-run.html) vas vodi skozi celoten pregled sistema.
- [Delovno okolje](workspace.html) pojasnjuje razdelke, podokna in bližnjice.
- [Kako spremembe ostanejo varne](safety.html) pojasnjuje, kaj KillerBench preveri pred vsako spremembo in po njej.
- [Navodila po korakih](fix-windows-update.html) vas vodijo skozi običajna opravila.
- Strani **Razdelki** opisujejo vsako orodje v enakih skupinah kot v aplikaciji.
- [Ukazna vrstica](cli.html) navaja vse ukaze `killerbench-cli`.
- [Odpravljanje težav](troubleshooting.html) opisuje blokirana dejanja, ponovne zagone in prekinjene zahteve.

Razvijalci, ki želijo diagnostiko vgraditi v svoja orodja, lahko preberejo [vodnik po pogonu za razvijalce](../engine/index.html).
