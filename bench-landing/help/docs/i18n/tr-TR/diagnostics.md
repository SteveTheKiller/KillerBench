# Tanılama

Her ziyaret Tanılama ile başlamalıdır. Her araç makineyi okur ve bir rapor yazar. Buradaki hiçbir şey Windows'u değiştirmez. Varsayılan araç olan tam sistem taraması, geniş bir salt okunur denetim kümesi çalıştırır ve dikkat gerektirenleri sıralar.

Adım adım kılavuzlar: [Çökmeleri ve mavi ekranları inceleme](investigate-crashes.html) ve [Sistem dosyalarını onarma](repair-system-files.html).

## Bu bölümdeki araçlar

Önce makineyi denetleyin, ardından dikkat gerektirenler ve bir sonraki güvenli adım için raporu okuyun.

### Tam sistem taraması

49 tanılama alanını inceler, ardından yüklü her hizmet kurtarma tarifini bakım, DHCP, yazdırma, Defender, saat ve WMI tarifleriyle birlikte değerlendirir. Denetimler birkaç dakika sürebilir. Otomatik onarım isteğe bağlıdır ve her tarifin kendi güncel kanıtını, makine ön koşullarını ve bağımlılık denetimlerini gerektirir. Uygun onarımlar eksik DHCP kiralarını yenileyebilir, kuyruktaki yazdırma işlerini serbest bırakabilir, saati yeniden eşitleyebilir, Defender imzalarını yenileyebilir, uygun hizmetleri geri yükleyebilir ve doğrulanan Windows bozulmasını onarabilir. Durdurulmuş otomatik hizmetler, başlangıç süreleri ve bağımlılıkları doğrulandıktan sonra başlatılabilir. Durdurulmuş, isteğe bağlı başlayan bir hizmet, tek kanıt boşta olması olduğunda değiştirilmeden kalır; dar odaklı iş akışları onu yalnızca geçerli özellik kanıtı başlatmayı desteklediğinde gerektirebilir. Başlangıç türleri, görev tanımları, raporlar, dökümler, abonelikler, kimlik bilgileri, kanal ayarları, güvenlik duvarı ilkesi ve yönetilen ayarlar korunur. Geçmiş, yedekleme ve aktarım anlık görüntüleri salt okunurdur. Döküm içerikleri açılmaz. Kirli bit incelemesi onarım veya yeniden başlatma zamanlamaz. Bu iş akışı güncelleştirme yüklemez, sürücüleri değiştirmez, dosya silmez veya eksiksiz bir donanım testi yapmaz.

### Hayati değerler

Kullanılabilir belleği, sağlayıcının bildirdiği disk sağlığını ve BitLocker koruma durumunu yakalar, ardından geçerli hayati değerleri aygıt sorunları, güvenilirlik geçmişi, donanım hatası kayıtları, çökme kanıtları, beklenmeyen kapanmalar ve Windows Bellek Tanılama sonuçlarıyla ilişkilendirir. Yeniden başlatma göstergeleri, sistem diskinin boş alanı, yükseltilmiş yetki ve güncelleştirme hizmeti yapılandırması görünür kalır. Kurtarma parolaları toplanmaz. Bu iş akışında hiçbir onarım çalışmaz. Birleşik sonuç eksiksiz bir donanım, yazılım veya kararlılık testi değildir.

### Güvenilirlik geçmişi

Son 14 günün Windows Güvenilirlik İzleyicisi kayıtlarını son Uygulama hatalarıyla ve beklenmeyen kapanma olaylarıyla birlikte okur, ardından tek bir sınırlı geçmiş sonucu döndürür. Geçmiş kayıtlar geçerli sağlığı veya kök nedeni belirlemez. Hiçbir onarım seçilmez ve hiçbir uygulama, hizmet veya ayar değiştirilmez.

### Başlangıç performansı

Son 14 günün Windows başlangıç ve kapanma performansı olay meta verilerini yalnızca sağlayıcı kimliğini ve sınırlı sayısal süre alanlarını saklayarak okur, ardından bunları desteklenen bekleyen yeniden başlatma göstergeleriyle ilişkilendirir. Yollar, işlem adları, hesaplar ve diğer olay yükü, kanıt kaydedilmeden önce kaldırılır. Geçmişteki yavaşlama kayıtları geçerli bir darboğazı belirlemez veya başlangıç, hizmet, uygulama, sürücü, görev, güç, yeniden başlatma ya da ilke değişikliklerine yetki vermez.

### Donanım hatası geçmişi

Son 14 günün Windows Hardware Error Architecture ve Bellek Tanılama geçmişini, geçerli Tak ve Kullan aygıt sorunlarını, fiziksel disk sağlığını ve sağlayıcı güvenilirlik sayaçlarını okur, ardından tek bir sınırlı donanım sonucu döndürür. Kayıtların yükleriyle, bellenim tanılamasıyla ve satıcı donanım testleriyle ilişkilendirilmesi gerekir. Hiçbir arızalı bileşen veya kök neden çıkarılmaz, hiçbir onarım seçilmez ve hiçbir donanım, bellenim, sürücü, disk, havuz, birim, dosya, hizmet, güç, yeniden başlatma veya ayar değiştirilmez.

### Sürücü yükleme geçmişi

Windows SetupAPI aygıt yükleme geçmişinin saklanan son bölümünü okur, başarısız bölümleri tam geçerli Tak ve Kullan kimlikleriyle ilişkilendirir ve tek bir sınırlı sonuç döndürür. Geçmiş günlük bölümleri bir aygıtın hâlâ mevcut olduğunu veya bir hatanın güncel olduğunu kanıtlamaz. Hiçbir sürücü yüklenmez, kaldırılmaz, geri alınmaz veya güncelleştirilmez.

### Sistem Geri Yükleme noktaları

En fazla 256 kullanılabilir Sistem Geri Yükleme noktasını listeler, açık oluşturma uygunluğunu gözden geçirir ve tek bir sınırlı hazırlık sonucu döndürür. Envanter, korumanın her birim için etkin olduğunu veya bir geri yüklemenin başarılı olacağını kanıtlamaz. Tanılama sırasında hiçbir geri yükleme noktası oluşturulmaz, silinmez veya uygulanmaz.

### Windows Kurtarma Ortamı

Geçerli Windows Kurtarma Ortamı yapılandırmasını REAgentC ile okur, etkin bir yerel konumu tam geçerli disk ve bölümle ilişkilendirir ve tek bir sınırlı hazırlık sonucu döndürür. İsteğe bağlı otomatik onarım, devre dışı bırakılmış mevcut bir yapılandırmayı etkinleştirebilir, ardından bildirilen durumunu doğrular. Kurtarma görüntüsü indirilmez, taşınmaz veya değiştirilmez, kurtarma önyüklemesi başlatılmaz ve Windows yeniden başlatılmaz. Etkin durum ve bölümün varlığı, kurtarma görüntüsünün bu PC'yi önyükleyebileceğini veya onarabileceğini kanıtlamaz.

### Windows etkinleştirme

Geçerli Windows lisans durumunu tam veya kısmi ürün anahtarları toplamadan okur, son yedi günün temizlenmiş Yazılım Koruması uyarı, hata ve kritik olay meta verilerini saklar, ardından tek bir sınırlı işletim sonucu döndürür. Olay yükü, kanıt kaydedilmeden önce kaldırılır. Geçerli lisanslama Lisanslı bildirdiğinde geçmiş kayıtlar bağlam olarak kalır. Hiçbir etkinleştirme, ürün anahtarı, belirteç, lisans, hesap, hizmet veya ilke işlemi seçilmez. Sağlayıcı durumu ve olay geçmişi abonelik hakkını, etkinleştirme sunucusuna ulaşılabilirliği, belirteç deposu bütünlüğünü, gelecekteki geçerliliği veya kök nedeni kanıtlamaz.

### Windows hizmetleri

Yüklü hizmet adlarını, görünen adları, durumları, başlangıç modlarını ve gizlilik açısından sınırlı kurtarma eylemi türlerini, hesapları, yürütülebilir dosya yollarını, kurtarma komutlarını veya yeniden başlatma iletilerini toplamadan envantere alır. Başlangıç modu tutarlılığını, doğrudan bağımlılık kimliklerini ve döngülerini gözden geçirir, tam bekleyen durumları yineler, desteklenen hizmetleri ve durdurulmuş hizmetlerin bağımlılıklarını inceler, ardından son Hizmet Denetimi Yöneticisi hatalarını yalnızca tam olarak tek bir hizmet adı veya benzersiz bir görünen ad mevcut olduğunda ilişkilendirir. Yükleme sırası grubu kullanımı üyelik genişletilmeden sayılır ve belirsiz kimlikler çözümlenmeden kalır. Durdurulmuş, isteğe bağlı başlayan hizmetler normal olabilir. Hiçbir hizmet başlatılmaz; başlangıç, bağımlılık ve kurtarma ayarları korunur. Belirli bir özelliğin kurtarılması gerektiğinde Onarım bölümündeki ilgili iş akışını kullanın.

### Uygulama hataları

Son Uygulama hata ve kritik olaylarını sınırlı Güvenilirlik İzleyicisi geçmişiyle birlikte okur, ardından tek bir kararlılık geçmişi sonucu döndürür. Geçmiş kayıtlar güncel bir arızayı veya kök nedeni kanıtlamaz ve yalnızca daha dar bir tanılamaya yönlendirir. Hiçbir uygulama durdurulmaz, onarılmaz, kaldırılmaz veya yeniden yapılandırılmaz.

### Görüntü aygıtları

Grafik bağdaştırıcılarını ve monitörleri bildirilen aygıt hataları, donanım kimlikleri ve yüklü sürücü sürümleri açısından inceler. Sürücü yükleme, aygıt sıfırlama veya çözünürlük değişikliği yoktur. Görüntü kalitesi ve fiziksel bağlantılar test edilmez.

### Giriş aygıtları

Klavye, fare ve HID aygıtı hatalarını ve sürücü envanterini inceler. Tuş vuruşları yakalanmaz, aygıtlar sıfırlanmaz ve sürücüler değiştirilmez. Fiziksel giriş çalışması test edilmez.

### Aygıt sorunları

Aygıt sınıfları genelinde sıfır olmayan Tak ve Kullan yapılandırma hata kodlarını bulur, ardından mevcut aygıt kanıtlarını sınırlı SetupAPI sürücü yükleme, aygıt kurulumu ve Tak ve Kullan geçmişiyle ilişkilendirir. Geçmiş kayıtlar tek başına güncel bir arızayı belirlemez. Donanımı, sürücüleri veya yapılandırmayı değiştirmeden tam aygıt kimliğini ve onaylı sürücü kanıtlarını gözden geçirin. Eksik aygıtlar ve fiziksel çalışma doğrulanmaz.
