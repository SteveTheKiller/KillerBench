# Varnost

Razdelek Varnost pregleda Microsoft Defender, požarni zid, potrdila in storitve prijave. Prikaže tudi TPM in varnostno stanje platforme, od katerih sta odvisna BitLocker in Windows Hello.

Navodila po korakih: [Šifriranje pogona z BitLockerjem](encrypt-with-bitlocker.html) in [Več zasebnosti v sistemu Windows](make-windows-private.html).

## Orodja v tem razdelku

Preglejte zaščito, požarni zid, potrdila in storitve prijave.

### Microsoft Defender

Pregleda stanje zaščite Defender in podpisov. Samodejno popravilo osveži podpise le, če Defender sporoča aktivno zaščito in zastarele podpise. Uporabi nastavljene vire podpisov. Brez sprememb zaščite, izključitev ali pravilnikov; brez pregleda zlonamerne programske opreme.

### Varnostno središče Windows

Pregleda omejene registracije protivirusnih programov s surovim stanjem ponudnikov, zgodovino napak točno določene storitve in trenutno pripravljenost Windows Security Center, ne da bi razlagal zdravje zaščite. Prazen popis ni ocena zdravja zaščite; število registracij, surovo stanje ali pretekle napake ne morejo odobriti obnovitve ali varnostne spremembe. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječe spremljanje varnostnega stanja in obvestila se lahko nadaljujejo. Nastavitve protivirusnih programov, registracije ponudnikov, nastavitve zaščite, izključitve, obvestila, pravilniki in vrsta zagona se ohranijo. Ne zahteva pregleda, posodobitve ali spremembe zaščite. Preverjanje potrjuje le stabilno stanje storitve, ne pa prisotnosti protivirusnega programa, stanja zaščite, svežine podpisov, dostave obvestil, lastništva registracij ali veljavnih pravilnikov.

### Storitve požarnega zidu

Prebere nastavitve omogočenosti profilov Domain, Private in Public iz ActiveStore, nato preveri storitvi Base Filtering Engine in Windows Defender Firewall. Izbirna obnovitev po svežem preverjanju odvisnosti zažene le omogočene, ustavljene storitve. Obstoječi pravilniki filtriranja lahko začnejo veljati in prekinejo omrežni ali oddaljeni dostop. Vrste zagona, dovoljenja storitev, pravila požarnega zidu, profili in nastavitve IPsec se ohranijo. Brez ponastavitev, sprememb pravil ali ustavljanja storitev. Preverjanje potrjuje le delovanje storitev; dejanska zaščita in povezljivost ostaneta nepreverjeni.

### Potek veljavnosti potrdil

Pregleda datume potrdil in zgradi lokalne verige zaupanja samo iz predpomnilnika za osebni shrambi računalnika ter trenutnega uporabnika, nato poda eno omejeno operativno ugotovitev. Preklic, predstavitev na strežniku, vezave, vpis, lastništvo pravilnikov, oddaljeno zaupanje in uporabnost zasebnih ključev ostanejo nepreverjeni. Ne spremeni potrdil, nastavitev zaupanja, vpisa, vezav, zasebnih ključev, aplikacij, storitev ali pravilnikov.

### Upravitelj poverilnic

Preveri razpoložljivost storitve Credential Manager. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječe aplikacije lahko nadaljujejo zahteve za shranjene poverilnice. Vsebina trezorjev, gesla, ključi za dostop, potrdila, žetoni, registracije poverilnic, dovoljenja, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. Ne zahteva vsebine poverilnic ali dejanj z njimi. Preverjanje potrjuje le stabilno stanje storitve, ne pa pridobivanja poverilnic, preverjanja pristnosti ali dostopa aplikacij.

### Upravitelj spletnih računov

Pregleda zgodovino posrednika računov z očiščeno vsebino dogodkov in razpoložljivost točno določene storitve Web Account Manager. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječe aplikacije lahko nadaljujejo zahteve za račune in žetone skladno s trenutnim pravilnikom. Pretekli dogodki ne morejo odobriti obnovitve ali potrditi trenutnega stanja računov. Računi, poverilnice, žetoni, piškotki, registracije aplikacij, dovoljenja, stanje prijave, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. KillerBench ne izbere dejanj z računi, poverilnicami, žetoni, piškotki, dovoljenji ali prijavo. Preverjanje potrjuje le stabilno stanje storitve, ne pa preverjanja pristnosti, izdajanja žetonov ali dostopa aplikacij.

### Pomočnik za prijavo v Microsoftov račun

Pregleda zgodovino posrednika računov z očiščeno vsebino dogodkov in razpoložljivost točno določene storitve Microsoft Account Sign-in Assistant. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječe zahteve za prijavo v Microsoftov račun se lahko nadaljujejo skladno s trenutnim pravilnikom. Pretekli dogodki ne morejo odobriti obnovitve ali potrditi trenutnega stanja računov. Računi, poverilnice, žetoni, registracija naprave, podatki aplikacij, dovoljenja, stanje prijave, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. KillerBench ne izbere dejanj z računi, poverilnicami, žetoni, registracijami, dovoljenji ali prijavo. Preverjanje potrjuje le stabilno stanje storitve, ne pa preverjanja pristnosti, povezljivosti računov ali dostopa aplikacij.
