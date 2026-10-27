# Zprovoznění

Zprovoznění připravuje nový počítač. Zkontroluje aplikace a konfiguraci, které vyberete, a pojmenuje počítač. Samo nic nestahuje ani neinstaluje.

Návody krok za krokem: [Nastavení nového počítače](set-up-new-pc.html).

## Kontrola instalačního programu před spuštěním

Příkazový řádek umí ověřit instalační program dodavatele dříve, než jej spustíte:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

KillerBench instalačnímu programu důvěřuje jen tehdy, když nese platný podpis Authenticode s přesným názvem organizace dodavatele. Instalační program nikdy nespouští.

## Nástroje v této sekci

Připravte nový počítač: aplikace, název a konfiguraci.

### Zprovoznění

Kontroluje výslovně vybrané aplikace pro zřízení a skupiny konfigurace. Aktuální registrace odinstalace, balíčků aplikací a Office Click-to-Run se porovnávají bez stahování nebo spouštění instalačních programů. Volby konfigurace popisují důsledky a po implementaci vedou k samostatně chráněným postupům. Neprobíhá žádné stahování, instalace ani akce s účty, aktualizacemi, úklidem, zásadami, registrem, službami, profily nebo restartem.

### Název počítače

Kontroluje navržený název počítače vůči aktuální identitě a roli v doméně. Bez přejmenování, shromažďování přihlašovacích údajů a restartu. Dostupnost názvu, oprávnění a závislosti aplikací zůstávají neověřené.
