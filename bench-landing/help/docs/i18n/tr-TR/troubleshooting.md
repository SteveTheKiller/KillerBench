# Sorun giderme

## Bir işlem engellendiğini bildiriyor

Engellenen sonuç, istenen işlemin devam edemediği anlamına gelir. İşlemin gönderilmeden önce mi yoksa daha önceki bir adımdan sonra mı durduğunu görmek için iletiyi ve günlüğü okuyun. Kısmen tamamlanmış bir işlemin geri alındığını varsaymayın. En yaygın ön koşul nedenleri aşağıdadır.

- **Yönetici erişimi gerekiyor.** KillerBench'i kapatın ve **Yönetici olarak çalıştır** ile yeniden başlatın.
- **Yeniden başlatma durumu temiz olmalı.** Windows'ta bekleyen bir yeniden başlatma var. Bilgisayarı yeniden başlatın, ardından işlemi yeniden çalıştırın.
- **Yönetilen ilke mevcut.** Ayarın sahibi grup ilkesi veya aygıt yönetimi. Ayarı orada değiştirin.
- **Kanıt değişti.** İlk denetim ile son yeniden denetim arasında bir şey değişti. Yeni durumu okumak için aracı yeniden çalıştırın.

## Önceki bir istek mutabakat gerektiriyor

KillerBench, kesintiye uğrayan bir istekten kalan bekleyen bir kayıt buldu. Aynı işlemi yeniden çalıştırın. Ne olduğunu denetler ve isteği yinelemeden gerçek sonucu bildirir. Komut satırında eşleşen `-recover` komutunu kullanın; örneğin `killerbench-cli bitlocker-auto-unlock-recover`.

## Bir denetim Tamamlanmayan veya Bilinmiyor gösteriyor

Kanıt okunamadı; bunun nedeni çoğu zaman KillerBench'in yönetici olarak çalışmaması veya bir Windows hizmetinin kullanılamamasıdır. KillerBench bilgisayarın sağlıklı olduğunu varsaymak yerine bunu dürüstçe bildirir.

## Dosyalar nerede tutulur

Günlükler, bekleyen kayıtlar ve kaydedilmiş kanıtlar `%LOCALAPPDATA%\KillerBench` altında bulunur. Her korumalı işlemin kendi klasörü vardır. Nelerin değiştiğine dair bir kayda ihtiyacınız olduğunda bunları saklayın.

## Sorun bildirme

KillerBench geliştirme aşamasındadır. Bir sorun bildirirken dışa aktarılan raporu ve etkinlik günlüğünü ekleyin ve paylaşmayacağınız her şeyi kaldırın.
