# Yazdırma sorunlarını giderme

İşler kuyrukta beklediğinde, bir yazıcı çevrimdışı göründüğünde veya varsayılan yazıcı yanlış olduğunda bunu kullanın. KillerBench önce biriktiriciyi, kuyrukları, işleri, sürücüleri ve bağlantı noktalarını okur, ardından tek bir tam kuyruğu veya işi düzeltmenize olanak tanır.

## Yazıcıları denetleme

1. **Aygıtlar** bölümünü açın ve **Yazdırma** öğesini seçin.
2. **Yerel yazıcıları bul** düğmesine basın ve sorun çıkaran kuyruğu seçin. Tüm kuyrukları denetlemek için **Genel yazdırma denetimlerini kullan** seçeneğini seçin.
3. **Onaylanan sorunları otomatik onar** seçeneğini kapalı bırakın ve **Tanılama çalıştır** düğmesine basın.
4. Raporu okuyun. Yazdırma Biriktiricisi durumunu, duraklatılmış kuyrukları, çevrimdışı modu, görünür işleri, sürücü koşullarını, bağlantı noktası kayıtlarını, son PrintService hatalarını ve ağ yazıcıları için yazıcının atanmış adresine yapılan tek bir bağlantı denemesini gösterir.

Başarılı bir bağlantı veya temiz bir durum kodu, yazıcıda kâğıt, toner ya da çalışan bir mekanizma olduğunu kanıtlamaz.

## Kuyruğu onarma

1. **Yazdırma** aracını **Onaylanan sorunları otomatik onar** açıkken yeniden çalıştırın. Etkin biriktiriciyi başlatabilir, bekleyen işleri olan tam bir duraklatılmış kuyruğu sürdürebilir ve çevrimdışı modu temizleyebilir.
2. Çevrimdışı modu temizlemek bekleyen işleri serbest bırakabilir; bu nedenle bu işlerin hâlâ yazdırılması gerektiğinden emin olun.
3. Birinin bilerek duraklattığı ve içinde bekleyen iş olmayan bir kuyruk duraklatılmış olarak kalır.

## Takılmış tek bir işi temizleme

1. **Engellenmiş yazdırma işlerini bul** düğmesine basın.
2. Hata veya Engellendi gösteren işi seçin ve **Yalnızca bu seçili işi iptal et** seçeneğini işaretleyin.
3. **Tanılama çalıştır** düğmesine basın. Yalnızca o iş iptal edilir ve sonuç geri okunur.

## Varsayılan yazıcıyı düzeltme

Varsayılan olması gereken kuyruğu seçin, **Eksik veya çakışan varsayılanı bu kuyrukla onar** seçeneğini işaretleyin, ardından **Tanılama çalıştır** düğmesine basın. Bu yalnızca varsayılan eksik veya çakışıyorsa işlem yapar.

## Komut satırından

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Bkz. [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) ve [default-printer-set](cli.html#cmd-default-printer-set).

## İlgili

- [Aygıtlar bölümü](devices.html#printing)
- [Ağ ve internet sorunlarını giderme](fix-network.html)
