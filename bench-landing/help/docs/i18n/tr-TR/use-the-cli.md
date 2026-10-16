# KillerBench'i betiklerden çalıştırma

KillerBench kanıtlarını bir betikte, bir RMM işinde veya uzak bir konsolda kullanmak istediğinizde komut satırını kullanın. `killerbench-cli` masaüstü uygulamasıyla aynı Engine'i çalıştırır ve JSON yazdırır.

## Başlarken

1. PowerShell'i `killerbench-cli.exe` dosyasının bulunduğu yerde açın veya klasörünü yolunuza ekleyin.
2. Her komutu güvenlik sınırıyla birlikte listelemek için `killerbench-cli help` komutunu çalıştırın.
3. Önerilen 11 tanılama grubunu görmek için `killerbench-cli diagnostic-scopes` komutunu çalıştırın.

## Tarama çalıştırma ve rapor kaydetme

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan`, `--auto-repair` eklemediğiniz sürece salt okunurdur. `--report`, uygulamanın dışa aktardığı temalı raporun aynısını yazar. Yalnızca bazı alanları denetlemek için `--scope`, iş akışı günlüğü olmadan birleşik JSON için `common-check` kullanın.

## Çıkış kodlarını okuma

| Kod | Anlamı |
| --- | --- |
| 0 | Önizleme veya doğrulanan sonuç başarılı oldu. |
| 1 | Bir başlatma, giriş veya günlük hatası komutu durdurdu. |
| 2 | Bağımsız değişkenler tanınmadı. |
| 3 | Engellendi, başarısız oldu veya doğrulanmadı. |
| 4 | Bir iş akışının doğrulama yapabilmesi için yeniden başlatma gerekiyor. |
| 130 | Siz durdurdunuz. |

Salt okunur bir komutta 0 çıkış kodu, bilgisayarın sağlıklı olduğu değil, komutun çalıştığı anlamına gelir. Bulgular için JSON'u okuyun.

## Değişiklik yapma

Makineyi değiştirebilecek her komut varsayılan olarak önizleme yapar. Önizlemeyi okuyun, ardından aynı komutu `--execute` ile çalıştırın. Bir istek kesintiye uğrarsa eşleşen `-recover` komutunu çalıştırın; gerçek sonucu bildirir ve değişikliği hiçbir zaman yinelemez.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Eski sonuçları yeniden açma

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Bir yapay zekâ yardımcısından kullanma

[KillerMCP](../mcp.html), Aygıt Yöneticisi ve Windows hata kodlarını aramak için yüklü CLI'yi çağırabilir.

## İlgili

- [Komut satırı başvurusu](cli.html)
- [Engine geliştirici kılavuzu](../engine/index.html)
- [Sorun giderme](troubleshooting.html)
