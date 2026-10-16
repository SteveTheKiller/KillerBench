# Disk alanı boşaltma

Bir sürücünün yeri azaldığında bunu kullanın. KillerBench alanın nereye gittiğini gösterir, her temizleme hedefinin neleri kaldıracağını önizler ve yalnızca kalıcı temizlemeyi açtıktan sonra siler.

## Ne kadar alan kaldığını görme

1. **Depolama** bölümünü açın ve **Depolama** öğesini seçin.
2. **Tanılama çalıştır** düğmesine basın. Rapor, boş alanı %10'un altında olan her sürücü harfli birimi işaretler ve gölge kopya ayırmasını, kota varsayılanlarını ve Depolama Algısı ilkesini gösterir.
3. Kilitlenme dökümleri ve eski güncelleştirme içeriği için **Tanılama** bölümündeki **Hayati değerler** ile **Güncelleştirmeler** bölümündeki **Windows Update** aracını da çalıştırın; güncelleştirme önbelleğinin boyutu orada görünür.

## Temizlemeyi önizleme

1. **Bakım** bölümünü açın ve **Temizleme** öğesini seçin.
2. İstediğiniz hedefleri işaretleyin; örneğin **Geçerli kullanıcı geçici dosyaları**, **Windows geçici dosyaları**, **Geçerli kullanıcı tarayıcı önbellekleri**, **Delivery Optimization önbelleği** veya **Geçerli kullanıcı Geri Dönüşüm Kutusu**. Bir hedefin tam olarak hangi klasörleri kapsadığını okumak için üzerine gelin.
3. **En düşük yaş (gün)** ve **Hedef başına en fazla dosya** değerlerini ayarlayın.
4. **Güncel denetimlerden sonra seçili kalıcı temizlemeyi gerçekleştir** kapalıyken **Tanılama çalıştır** düğmesine basın. Rapor her hedef için adayları ve tahmini alanı listeler. Hiçbir şey silinmez.

## Dosyaları kaldırma

1. **Güncel denetimlerden sonra seçili kalıcı temizlemeyi gerçekleştir** seçeneğini açın ve **Tanılama çalıştır** düğmesine basın.
2. KillerBench her adayı kaldırmadan önce yeniden denetler, yeni, korumalı veya kullanımda olan dosyaları atlar ve her sonucu günlüğe kaydeder.

Kalıcı temizleme Geri Dönüşüm Kutusu'nu atlar ve geri alınamaz. Hata raporlarını ve kilitlenme dökümlerini kaldırmak, çökmeleri incelemek için gerekebilecek kanıtları atar; bu nedenle önce bunları gözden geçirin.

## Önceki Windows yüklemesini kaldırma

Bir özellik yükseltmesinden sonra **Temizleme** içindeki **Önceki Windows yüklemesi**, desteklenen Windows geri alma kaldırma isteğiyle geri alma kopyasını kaldırabilir. Tek başına çalışır, **Önceki yüklemeden ihtiyacım olan tüm dosya ve ayarları yedekledim** onayını gerektirir ve önceki sürüme dönme olanağını kalıcı olarak kaldırır.

## Komut satırından

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Bkz. [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) ve [previous-windows](cli.html#cmd-previous-windows).

## İlgili

- [Bakım bölümü](maintenance.html#cleanup)
- [Depolama bölümü](storage.html)
- [Sürücüyü denetleme ve onarma](check-a-drive.html)
