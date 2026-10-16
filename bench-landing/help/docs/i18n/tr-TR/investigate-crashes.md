# Çökmeleri ve mavi ekranları inceleme

Windows beklenmedik şekilde yeniden başladığında, bir durdurma hatası gösterdiğinde veya bir uygulama sürekli çöktüğünde bunu kullanın. KillerBench çökme, kapanma, donanım hatası ve güvenilirlik kanıtlarını tek bir raporda toplar. Kaydedilenleri açıklar ve hiçbir zaman kök nedeni tahmin etmez.

## Bilgisayarı denetleme

1. **Tanılama** bölümünü açın ve **Hayati değerler** öğesini seçin.
2. **Tanılama çalıştır** düğmesine basın. Rapor belleği, disk sağlığını, aygıt sorunlarını, Güvenilirlik İzleyicisi geçmişini, donanım hatası kayıtlarını, çökme raporlarını, beklenmeyen kapanmaları ve Windows Bellek Tanılama sonuçlarını birleştirir.
3. Sonucu ve altındaki kanıtları okuyun. Geçmiş çökmeler ne olduğunu açıklar; bir arızanın hâlâ var olduğunu kanıtlamaz.

## Daraltma

- **Donanım hatası geçmişi**, WHEA ve bellek tanılama kayıtlarını aygıt sorunları ve disk sağlığıyla birlikte okur.
- **Güvenilirlik geçmişi**, son 14 günün uygulama çökmelerini, yanıt vermemelerini ve beklenmeyen kapanmalarını gruplar.
- **Uygulama hataları**, çöken programlara odaklanır.
- **Sürücü yükleme geçmişi**, şu anda mevcut olan aygıtlarla eşleştirilmiş başarısız sürücü yüklemelerini gösterir.
- **Aygıt sorunları**, Aygıt Yöneticisi hata kodları olan aygıtları listeler.

## Kilitlenme dökümlerini okuma

KillerBench döküm yapılandırmasını ve döküm dosyası meta verilerini listeler ve özel durum ve modül adları için mini döküm yapısını analiz edebilir. Döküm içerikleri masaüstü uygulamasında hiçbir zaman açılmaz. Bir hata denetimi bilgi paketi yüklüyse, durdurma kodları belgelenmiş anlamlarına çözümlenir.

İncelemeyi bitirene kadar dökümleri saklayın. **Temizleme** bunları kaldırabilir, ancak bu kanıtları atar.

## Komut satırından

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Bkz. [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) ve [device-code](cli.html#cmd-device-code).

## İlgili

- [Tanılama bölümü](diagnostics.html#vitals)
- [Raporlar ve kanıtlar](reports.html)
