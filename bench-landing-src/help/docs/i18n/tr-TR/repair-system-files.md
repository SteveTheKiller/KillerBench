# Sistem dosyalarını onarma

Windows bileşenleri bozulduğunda bunu kullanın: özellikler yüklenemez, güncelleştirmeler bileşen hataları bildirir veya sistem araçları çöker. KillerBench, DISM ve SFC'yi yalnızca canlı bir denetim bozulmayı doğruladıktan sonra çalıştırır ve ardından sonucu doğrular.

## Başlamadan önce

- KillerBench'i yönetici olarak çalıştırın.
- Önce bekleyen yeniden başlatmaları giderin.
- Makine yönetiliyorsa, bir ilkenin DISM'i belirli bir onarım kaynağına yönlendirip yönlendirmediğini denetleyin. KillerBench bu ilkeyi bildirir ve değiştirmez.

## Bozulmayı denetleme

1. **Onarım** bölümünü açın ve **Sistem dosyaları** öğesini seçin.
2. **Onaylanan sorunları otomatik onar** seçeneğini kapalı bırakın ve **Tanılama çalıştır** düğmesine basın.
3. Raporu okuyun. Bakım paketlerinin, isteğe bağlı özelliklerin ve yeteneklerin durumlarını envantere alır, ön koşulları denetler ve korumalı sistem dosyalarını tarar. Temiz bir sonuç, hiçbir onarım sunulmadığı anlamına gelir.

## Doğrulanan bozulmayı onarma

1. **Onaylanan sorunları otomatik onar** seçeneğini açın ve **Onarımı çalıştır** düğmesine basın. KillerBench, DISM'i veya SFC'yi yalnızca canlı denetim bozulmayı doğruladığında seçer.
2. Doğrulamayı bekleyin. Rapor, yalnızca takip denetimi onarılmış bir durumu geri okuduğunda **Doğrulandı** gösterir.
3. Rapor deponun kurtarılamaz olduğunu veya onarım kaynağının kullanılamadığını söylüyorsa orada durun. Onarımı yinelemek sonucu değiştirmez.

Kesintiye uğrayan bir DISM veya SFC çalıştırması, bakım komutu yinelenmeden bir sonraki önyüklemede mutabakata bağlanır.

## Bileşen deposunu temizleme

**Güncelleştirmeler** bölümünde **Bileşen deposu temizliği** öğesini seçin. Önce Windows'a temizliğin önerilip önerilmediğini sorar. İsteğe bağlı bakım, yerini başkalarına bırakmış bileşenleri kaldırır ve yeniden analiz eder. ResetBase'i hiçbir zaman kullanmaz ve WinSxS içeriğini hiçbir zaman elle silmez.

## Komut satırından

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Bkz. [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) ve [servicing-logs](cli.html#cmd-servicing-logs). `repair` varsayılan olarak önizleme yapar ve yalnızca `--execute` ile işlem gerçekleştirir.

## İlgili

- [Windows Update'i düzeltme](fix-windows-update.html)
- [Onarım bölümü](repair.html#system-files)
- [Değişiklikler nasıl güvenli kalır](safety.html)
