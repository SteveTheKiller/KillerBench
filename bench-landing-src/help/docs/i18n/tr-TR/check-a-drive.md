# Sürücüyü denetleme ve onarma

Bir birim hata bildirdiğinde, Windows bir sürücüyü taramayı istediğinde veya dosyalar açılmadığında bunu kullanın. KillerBench birimi önce salt okunur olarak denetler ve dosya sistemi onarımını yalnızca bir yedeği onayladıktan sonra zamanlar veya çalıştırır.

## Başlamadan önce

- Birimdeki önemli verileri yedekleyin. Dosya sistemi onarımı hasarlı dosyaları değiştirebilir veya kaldırabilir.
- KillerBench'i yönetici olarak çalıştırın.

## Birimi denetleme

1. **Depolama** bölümünü açın ve **Depolama** öğesini seçin.
2. **Denetlenecek birim** içinden sürücüyü seçin ve **Tanılama çalıştır** düğmesine basın.
3. Raporu okuyun. Disk ve birim sağlığını, kirli biti, çevrimiçi NTFS kendi kendini onarmayı, denetleyici ve disk aygıtı hatalarını, güvenilirlik sayaçlarını ve sıfırlamalar, yeniden denemeler ve beklenmeyen çıkarma gibi son depolama olaylarını gösterir.

Temiz bir denetim, hiçbir onarım sunulmadığı anlamına gelir. Disk sıcaklığı, aşınma ve düzeltilmemiş G/Ç sayaçları inceleme kanıtıdır ve tek başlarına onarımı tetiklemez.

## Dosya sistemini onarma

1. **Dosya sistemi onarımını incele** düğmesine basın ve **Nokta onarımı** veya **Çevrimdışı tarama ve onarım** seçeneğini seçin.
2. Bir veri birimi için, onu kullanan her şeyi kapatabiliyorsanız **Sistem dışı birimin çevrimdışı olmasına izin ver** seçeneğini işaretleyin. Windows birimi çevrimdışı olamaz; bu nedenle onarımı bir sonraki yeniden başlatmaya zamanlanır.
3. **Bu birimdeki önemli veriler yedeklendi** seçeneğini işaretleyin.
4. Onarımı onaylayın. KillerBench işlem yapmadan hemen önce tam birimi yeniden denetler ve önce bekleyen bir kayıt yazar.
5. Zamanlanmış bir onarım için uygun olduğunda yeniden başlatın. KillerBench, onarımı yeniden çalıştırmadan, yeniden başlatmanın ardından sonucu doğrular.

## İyileştirme veya retrim

Aynı araç birimi analiz edebilir ve Windows'un önerdiği iyileştirmeyi, **Katı hal depolamayı retrim yap** işlemini veya sabit disk birleştirmesini çalıştırabilir. Her biri, tek bir tam ve sağlıklı birim üzerinde ayrı olarak onaylanan bir işlemdir.

## Komut satırından

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Bkz. [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) ve [volume-optimize](cli.html#cmd-volume-optimize).

## İlgili

- [Depolama bölümü](storage.html)
- [Disk alanı boşaltma](free-disk-space.html)
- [Sorun giderme](troubleshooting.html)
