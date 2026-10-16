# Onarım

Onarım, bir şeyi düzeltebilen araçları bir araya getirir. Her biri önce tanılama yapar. Bir onarım yalnızca güncel kanıt, düzelttiği sorunun tam kendisini gösterdiğinde sunulur ve yalnızca **Onaylanan sorunları otomatik onar** seçeneğini açtıktan veya tek bir onarımı kendiniz onayladıktan sonra çalışır. Her onarım günlüğe yazılır ve sonrasında doğrulanır.

Adım adım kılavuzlar: [Sistem dosyalarını onarma](repair-system-files.html) ve [Windows Update'i düzeltme](fix-windows-update.html).

## Otomatik ve el ile onarım

**Onaylanan sorunları otomatik onar**, bir tanılama aracının taramadan sonra gerekli olduğunu kanıtladığı onarımları çalıştırmasına olanak tanır. Varsayılan olarak kapalıdır.

Tek bir bakım onarımı için **Gelişmiş: günlükler ve el ile onarımlar** öğesini açın ve **Onarımlar** seçeneğini seçin. **Bileşen deposu (DISM)** veya **Sistem dosyaları (SFC)** seçeneğini seçin, önizlemeyi okuyun, **Bu onarımı bu PC'de çalıştır** seçeneğini işaretleyin ve **Onarımı çalıştır** düğmesine basın. Herhangi bir değişiklikten önce ve sonra ne olduğu için [Değişiklikler nasıl güvenli kalır](safety.html) sayfasına bakın.

## Bu bölümdeki araçlar

Bir tanılama bir sorunu belirledikten sonra bunları kullanın. Onarımlar doğrulanmış kanıt ve açık onay gerektirir.

### Temel Windows hizmetleri

Güç, Tak ve Kullan, Kullanıcı Yöneticisi, Grup İlkesi İstemcisi, Ağ Deposu Arabirimi Hizmeti, RPC Uç Nokta Eşleyicisi, Uzak Yordam Çağrısı (RPC), DCOM Sunucu İşlem Başlatıcısı, Arka Plan Görevleri Altyapı Hizmeti ve CoreMessaging hizmetlerini birlikte denetler. İsteğe bağlı kurtarma, yalnızca güncel kanıtın başlangıç süresi ve eksiksiz bağımlılık denetimlerinden sonra durdurulmuş olduğunu doğruladığı tam, etkin otomatik hizmetleri başlatır. İsteğe bağlı başlayan hizmetler boştayken değiştirilmeden kalır. Güç planları, aygıtlar, sürücüler, hesaplar, kimlik bilgileri, profiller, ilke, bağdaştırıcılar, adresler, yollar, DNS, güvenlik duvarı yapılandırması, kayıtlar, uç noktalar, görev kayıtları, uygulama verileri, hizmet izinleri ve başlangıç türleri korunur. Hiçbir ilke yenilemesi, aygıt taraması, hesap işlemi, ağ değişikliği, güç ayarı değişikliği, kayıt değişikliği veya görev başlatma istenmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; özelliğin uçtan uca davranışını kapsamaz.

### Sistem dosyaları

Sınırlı Windows bakım paketi, isteğe bağlı özellik ve yetenek durumlarını envantere alır, ön koşulları denetler, korumalı sistem dosyalarını tarar ve SFC'yi yalnızca canlı denetim bozulmayı doğruladığında otomatik olarak seçer. Bekleyen paket, özellik veya yetenek durumları incelenmeyi gerektirir ancak yükleme, kaldırma, temizleme, geri alma, etkinleştirme veya devre dışı bırakmaya yetki vermez.

### WMI deposu

WMI hizmetinin kullanılabilirliğini, ardından depo tutarlılığını denetler. Otomatik onarım uygun, durdurulmuş bir hizmeti başlatabilir ve doğrulanan bozulmayı kurtarabilir, ardından her sonucu doğrular. Devre dışı hizmet ayarları korunur. Okunabilir kayıtlar birleştirilir; okunamayan içerik sağlayıcının yeniden yüklenmesini gerektirebilir. Yönetim istemcileri kesintiye uğrayabilir. Sıfırlama, silme veya otomatik geri alma yoktur.

### Windows sistem bildirimleri

Son Hizmet Denetimi Yöneticisi geçmişini okur, ardından COM+ Olay Sistemi ile Sistem Olay Bildirimi Hizmeti'ni birlikte denetler. İsteğe bağlı kurtarma, SENS'i başlatmadan önce COM+ Olay Sistemi hazırlığını doğrular. Mevcut oturum açma, ağ, güç ve COM+ bildirimleri geçerli yapılandırma altında sürebilir. COM+ katalogları, abonelikler, bildirim kayıtları, tüketiciler, hesaplar, izinler, kurtarma ayarları, ilke ve başlangıç türleri korunur. Hiçbir abonelik, kayıt, tüketici, hesap, izin veya ilke değiştirilmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; uçtan uca bildirim teslimini kapsamaz.

### Tanılama İlkesi Hizmeti

Sınırlı Tanılama İlkesi işletim geçmişini, tam hizmet hatası geçmişini ve geçerli hizmet hazırlığını gözden geçirir. Eksik kanıt önceliklidir. Geçmiş kayıtlar hizmet kurtarmaya veya bir sorun giderme işlemine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut tanılama senaryoları sürebilir. İlke, sorun giderme verileri ve başlangıç türü korunur. Hiçbir sorun giderici veya önerilen düzeltme çalıştırılmaz. Doğrulama yalnızca kararlı hizmet durumunu kapsar; arıza algılamayı, tanılamanın tamamlanmasını veya onarım sonucunu kapsamaz.

### Windows Hata Bildirimi

Kilitlenme dökümü yapılandırmasını, sınırlı döküm dosyası meta verilerini, son sistem çökme raporlarını, tam Windows Hata Bildirimi hizmet hatası geçmişini ve geçerli hizmet hazırlığını gözden geçirir. Geçmiş raporlar güncel bir arızayı veya kök nedeni belirlemez ve kurtarmaya yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Kuyruktaki mevcut raporlar geçerli ilke altında sürebilir. Rapor arşivleri, dökümler, onay, karşıya yükleme ilkesi ve başlangıç türü korunur. Hiçbir karşıya yükleme veya silme istenmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; rapor oluşturmayı, karşıya yüklemeyi, teslimi, sorun çözümünü veya çökme tanılamasını kapsamaz.

### Windows Olay Toplayıcısı

Sınırlı Windows Olay Toplayıcısı abonelik envanterini, tam hizmet hatası geçmişini ve geçerli hizmet hazırlığını gözden geçirir. Boş bir envanter kasıtlı olabilir ve abonelik sayısı ya da geçmiş hatalar kurtarmaya yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut abonelikler uzak olay toplamayı sürdürebilir. Abonelikler, iletilen olaylar, kimlik bilgileri, kanal ayarları, ilke ve başlangıç türü korunur. Doğrulama yalnızca kararlı hizmet durumunu kapsar; kaynağa ulaşılabilirliği, kimlik doğrulamayı, uzak teslimi, olayların güncelliğini veya abonelik sağlığını kapsamaz.

### Windows Olay Günlüğü

Dört tanılama kanalını etkinlik durumu, saklama modu, geçerli boyut, yapılandırılmış sınır, kayıt sayısı ve doluluk açısından inceler, ardından Windows Olay Günlüğü hizmetinin kullanılabilirliğini denetler. Otomatik onarım bağımlılıkları denetledikten sonra durdurulmuş, etkin bir hizmeti başlatabilir ve çalışma durumunu doğrulayabilir. Ayrıca onaylanan odaklı bir işlem, bildirilen diğer tüm ayarları koruyarak devre dışı bırakılmış ve yönetilmeyen tek bir tam tanılama kanalını etkinleştirebilir. Günlükler, kanal izinleri, saklama, boyut sınırları, başlangıç türleri ve yönetilen ayarlar korunur. Başarılı olay teslimi ve arşiv kapasitesi doğrulanmamış olarak kalır.

### Windows Saat hizmeti

Windows Saat yapılandırmasını, hizmetini ve eşitlemesini denetler. Otomatik onarım, geçerli durum tam olarak durdurulmuş hizmet hatasını bildirdiğinde ve güncel ilke, mod ve bağımlılık denetimleri geçtiğinde W32Time'ı başlatabilir, ardından yeniden eşitleme isteyip güncel durumu doğrular. Bu, saati mevcut yapılandırılmış kaynakları kullanarak ayarlayabilir. Başlangıç türü, kaynak yapılandırması ve yönetilen ayarlar korunur.

### Görev Zamanlayıcı hizmeti

Sınırlı zamanlanmış görev durumunu, çalışma sonuçlarını ve kaçırılan çalıştırma sayılarını envantere alır, ardından geçerli hizmet kullanılabilirliğini denetlemeden önce bunları tam Görev Zamanlayıcı hizmet hatası geçmişiyle birleştirir. Görev bulguları ve geçmiş hizmet hataları bir hizmet başlatmaya yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut etkin görevler, kaçırılan tetikleyiciler dahil, yapılandırılmış hesapları altında başlayabilir. Hiçbir görev tanımı, güvenlik sorumlusu, kimlik bilgisi başvurusu, ilke veya başlangıç türü değiştirilmez ve hiçbir görev doğrudan başlatılmaz veya silinmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; eylem başarısını, kimlik bilgisi geçerliliğini veya gelecekteki görev sonuçlarını kapsamaz.

### Kullanıcı Profili Hizmeti

Kullanıcı Profili Hizmeti'nin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut oturum açma ve profil istekleri sürebilir. Profil klasörleri, kayıt defteri kovanları, izinler, profil listesi kayıtları, kimlik bilgileri, kullanıcı verileri, ilke, hizmet izinleri ve başlangıç türü korunur. Hiçbir profil işlemi istenmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; oturum açmayı, profil yüklemeyi veya kullanıcı verilerine erişimi kapsamaz.

### Yedekleme hizmetleri

Birim Gölge Kopyası, Microsoft Yazılım Gölge Kopyası Sağlayıcısı, Windows Yedekleme ve Blok Düzeyinde Yedekleme Altyapısı hizmetlerini denetler, ardından VSS yazıcı durumunu, kayıtlı sağlayıcı türlerini, geçerli anlık görüntüleri ve gölge depolama ayırmasını okur. Durdurulmuş, isteğe bağlı başlayan hizmetler boştayken normal olabilir. İsteğe bağlı kurtarma, bağımlılık denetimlerinden sonra yalnızca durdurulmuş otomatik hizmetleri başlatır, ardından çalışma durumunu doğrular. Mevcut zamanlanmış veya kesintiye uğramış yedekleme işleri sürebilir. Devre dışı ve isteğe bağlı başlangıç ayarları, anlık görüntüler, gölge depolama, sağlayıcı kayıtları, yedekleme planları, kataloglar, zamanlamalar, hedefler, kimlik bilgileri ve saklama korunur. Hiçbir yedekleme seçilmez veya yeniden yapılandırılmaz, hiçbir hizmet yeniden başlatılmaz ve hiçbir anlık görüntü veya katalog silinmez. Sağlayıcı kaydı, yazıcı durumu, depolama ayırması ve kararlı hizmet durumu, yedeklemenin tamamlandığını, hedefe erişimi veya geri yükleme başarısını doğrulamaz.
