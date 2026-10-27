# Začetna priprava

Priprava računalnika je namenjena novemu računalniku. Pregleda izbrane aplikacije in nastavitve ter ime računalnika. Sama ne prenese ali namesti ničesar.

Navodila po korakih: [Nastavitev novega računalnika](set-up-new-pc.html).

## Preverjanje namestitvenega programa pred zagonom

Ukazna vrstica lahko pred zagonom preveri namestitveni program ponudnika:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

KillerBench zaupa namestitvenemu programu le, če ima veljaven podpis Authenticode s točnim imenom organizacije ponudnika. Namestitvenega programa nikoli ne zažene.

## Orodja v tem razdelku

Pripravite nov računalnik: aplikacije, ime in nastavitve.

### Začetna priprava

Pregleda izrecno izbrane aplikacije za pripravo računalnika in skupine nastavitev. Poveže trenutne registracije za odstranjevanje programov, pakete aplikacij in Office Click-to-Run brez prenosa ali izvajanja namestitvenih programov. Izbire nastavitev opišejo posledice in usmerijo v ločeno varovane postopke, kjer so ti na voljo. Ne izvede prenosa, namestitve ali dejanj z računi, posodobitvami, čiščenjem, pravilniki, registrom, storitvami, profili ali ponovnim zagonom.

### Ime računalnika

Predlagano ime računalnika primerja s trenutno identiteto in vlogo v domeni. Brez preimenovanja, zbiranja poverilnic ali ponovnega zagona. Razpoložljivost imena, dovoljenja in odvisnosti aplikacij ostanejo nepreverjeni.
