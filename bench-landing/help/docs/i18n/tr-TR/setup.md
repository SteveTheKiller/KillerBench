# İndirme ve kurulum

KillerBench 0.1.0 henüz yayımlanmadı. Herkese açık bir KillerBench paketi veya indirmesi henüz yok. Bu sayfa, resmî olmayan bir kopya indirmeden hazırlanabilmeniz için geçerli paket gereksinimlerini açıklar.

## Windows ve çalışma zamanı

- Geçerli yayımlama hedefi **Windows x64**'tür. Geçerli yerel doğrulama temeli Windows 11 x64'tür. Yerel ARM64, x86 ve Windows Server dağıtımları sağlanmaz.
- KillerBench iki biçimde paketlenir. Yükleyici **çerçeveye bağımlıdır** ve **.NET Desktop Runtime 10 for Windows x64** gerektirir. Yalın .NET Runtime, ASP.NET Core Runtime ve .NET Framework WPF sağlamaz. Taşınabilir sürüm kendi çalışma zamanını içerir ve ayrı bir yükleme gerektirmez. `killerbench-cli.exe` komut satırı aracı .NET 10 Runtime for Windows x64 gerektirir. SDK sıradan kullanım için değil, geliştirme için gereklidir.
- [.NET 10 on Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows) tarafından desteklenen bir Windows sürümü kullanın. Çalışma zamanı desteği, KillerBench onarımlarının desteklenen her sürümde veya derlemede doğrulandığı anlamına gelmez. Windows 10 kapsamı henüz beklemededir ve desteklenen sürümleri Microsoft'un geçerli yaşam döngüsü gereksinimleriyle sınırlıdır.
- Desktop Runtime'ı yalnızca [Microsoft'un .NET 10 indirme sayfasından](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) **.NET Desktop Runtime**, **Windows**, **x64** seçerek edinin. Kuruluşunuzun yazılım yükleme kurallarına uyun.

`dotnet` kullanılabiliyorsa, `dotnet --list-runtimes` komutu x64 yüklemesi için `Microsoft.WindowsDesktop.App 10.0.x` girdisini listelemelidir. Yalnızca başka bir mimari için yüklenmiş bir çalışma zamanı x64 yükleyici paketini karşılamaz.

## Bir paket yayımlandığında

1. Bu web sitesinden bağlantı verilen resmî indirmeyi kullanın. Henüz etkin bir KillerBench indirme bağlantısı yoktur.
2. O paketin gereksinimlerini, [GPLv3 lisansını](../license.txt), Karşılık Gelen Kaynak talimatlarını, dahil edilen üçüncü taraf bildirimlerini, imza ve sağlama toplamı bilgilerini okuyun. Bir dosya adı tek başına özgünlüğü kanıtlamaz.
3. Yükleyiciyi geçerli hesap için ya da bir yükseltme isteminden sonra tüm kullanıcılar için çalıştırın. Yükleyici bir Başlat menüsü kısayolu, isteğe bağlı bir masaüstü kısayolu ve Program Ekle veya Kaldır girdisi ekler. Taşınabilir sürüm yükleme gerektirmeden çalışır.
4. KillerBench'i normal hesabınızla başlatın ve [İlk tanılamanız](first-run.html) sayfasını izleyin. Tanılama erişim hataları görünür kalır; yetki yükseltme ayrı ve bilinçli bir seçimdir.

Doğrulanmamış bir kopyayı çalıştırmak için Windows güvenlik uyarılarını atlamayın. Makineyi değiştiren herhangi bir işlemi seçmeden önce güncel bir yedeğiniz olsun.

Başlangıçta KillerBench GitHub'da daha yeni bir kararlı sürüm olup olmadığını denetler ve yüklemeden önce sorar. İndirme, yayımlanan SHA-256 sağlama toplamıyla eşleşmelidir. Denetimi Hakkında bölümündeki **Başlangıçta güncellemeleri denetle** ile kapatabilirsiniz.

## Geçerli sınırlar

Geçerli derlemede on iki bölüm, salt okunur tanılama, raporlar ve tek tek seçilen onarım yüzeyleri vardır. Yerel onarım doğrulaması hâlâ sürmektedir. Çalışan bir arayüz, başarılı bir derleme veya bir test düzeni sonucu, gerçek bir bilgisayardaki onarım davranışını kanıtlamaz. Bkz. [Değişiklikler nasıl güvenli kalır](safety.html#development-status).

Açılış sayfasındaki örnek konsol yapay çıktıdır. Son uygulama ekran görüntüleri ve ilk herkese açık paket hâlâ beklemededir.

## Kaynak, lisans ve destek

KillerBench GPLv3 ile lisanslanmıştır. İlk herkese açık paket için kaynak teslim talimatları ve üçüncü taraf yazı tipi izinleri hâlâ beklemededir. Kaynak kullanılabilirliği, dağıtım bildirimleri, kanıt gizliliği ve destek bilgileri için [KillerBench hakkında](about.html) sayfasını okuyun. [Engine kılavuzu](../engine/) yetkili bir kaynak kopyasına sahip geliştiriciler içindir; herkese açık bir Engine paketinin bulunduğu anlamına gelmez.
