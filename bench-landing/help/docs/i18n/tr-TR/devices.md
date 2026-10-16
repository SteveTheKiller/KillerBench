# Aygıtlar

Aygıtlar; yazıcıları, sesi, kameraları, Bluetooth'u, giriş aygıtlarını ve bağlı donanımın dayandığı Windows hizmetlerini kapsar.

Adım adım kılavuzlar: [Yazdırma sorunlarını giderme](fix-printing.html) ve [Çökmeleri ve mavi ekranları inceleme](investigate-crashes.html).

## Bu bölümdeki araçlar

Yazdırma, ses, kameralar, Bluetooth, giriş aygıtları ve bağlı donanımın arkasındaki hizmetler.

### Yazdırma

Yazdırma Biriktiricisini, yerel kuyruk duraklatmalarını, görünür yazdırma işlerini, çevrimdışı ayarları ve sürücünün bildirdiği koşulları denetler, ardından kuyruk, sürücü, bağlantı noktası, Standart TCP/IP uç noktası, yazdırma işlemcisi ve yazdırma izleyicisi kayıtlarını envantere alır. Son PrintService sorunlarını yalnızca tam olarak tek bir geçerli kuyruk kimliği mevcut olduğunda ilişkilendirir. Atanmış Standart TCP/IP uç noktaları, uygulama verisi, SNMP veya yazdırma işi olmadan tek bir sınırlı bağlantı denemesi alır. Otomatik onarım etkin biriktiriciyi başlatabilir, yalnızca güncel bekleyen iş kanıtı olan tam duraklatılmış kuyrukları sürdürebilir ve çevrimdışı modu temizleyebilir. Boştaki kasıtlı duraklatmalar yalnızca inceleme olarak kalır. Uzak bağlantılar, SNMP topluluk dizeleri, uzantı kayıtları ve iş içerikleri korunur. Bir durum kodu veya başarılı bir bağlantı; yazıcı kimliğini, sarf malzemelerini, mekanizmayı, kuyruğun kabulünü, işleme başarısını veya fiziksel yazdırmayı belirlemez.

### Ses

Varsayılan çoklu ortam giriş ve çıkış kimliklerini ve paylaşılan biçimleri, ses aygıtı ve sürücü durumunu, Windows Ses Uç Noktası Oluşturucusu'nu ve Windows Ses hizmetini denetler. Otomatik onarım durdurulmuş, etkin hizmetleri başlatır ve çalışma durumunu doğrular. Başlangıç ayarları, sürücüler, seçili aygıtlar, özel mod ve ses düzeyleri korunur. Ses çalma, kayıt ve uygulama yönlendirmesi test edilmeden kalır.

### Kamera desteği

Kamera ve görüntüleme aygıtı hatalarını, donanım kimliklerini, yüklü sürücü kanıtlarını ve Windows Kamera Çerçeve Sunucusu kullanılabilirliğini inceler. İsteğe bağlı kurtarma, bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş bir hizmeti başlatır. Zaten kamera erişimi isteyen bir uygulama mevcut izinlerle devam edebilir. Kamera gizlilik ayarları, izinler, aygıt yapılandırması, ilke, sürücüler ve başlangıç türü korunur. Sıfırlama, izin değişikliği veya sürücü yükleme yoktur. Doğrulama yalnızca bildirilen aygıt yapılandırmasını ve hizmetin çalışma durumunu kapsar; görüntü yakalamayı, gizlilik erişimini veya fiziksel çalışmayı kapsamaz.

### Bluetooth desteği

Bluetooth aygıtı hatalarını, donanım kimliklerini, yüklü sürücü kanıtlarını ve Bluetooth Destek Hizmeti kullanılabilirliğini inceler. İsteğe bağlı kurtarma, bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş bir hizmeti başlatır; eşleştirilmiş aygıtlar yeniden bağlanabilir. Eşleştirmeler, radyo ayarları, aygıt yapılandırması, ilke ve başlangıç türü korunur. Sıfırlama, aygıt kaldırma veya sürücü yükleme yoktur. Doğrulama yalnızca bildirilen aygıt yapılandırmasını ve hizmetin çalışma durumunu kapsar; bulmayı, eşleştirmeyi, sesi veya veri aktarımını kapsamaz.

### Akıllı kart desteği

Akıllı kart okuyucu hatalarını, donanım kimliklerini, yüklü sürücü kanıtlarını ve Akıllı Kart hizmeti kullanılabilirliğini inceler. İsteğe bağlı kurtarma, bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş bir hizmeti başlatır. Uygulamalar mevcut ilke altında takılan kartları algılamayı sürdürebilir. Sertifikalar, PIN'ler, kimlik bilgileri, okuyucu ve kart yapılandırması, ilke, sürücüler ve başlangıç türü korunur. Başlatma, kayıt, PIN işlemi, okuyucu sıfırlama veya sürücü yükleme yoktur. Doğrulama yalnızca bildirilen okuyucu yapılandırmasını ve hizmetin çalışma durumunu kapsar; kart algılamayı, kimlik doğrulamayı veya uygulama erişimini kapsamaz.

### Biyometrik destek

Biyometrik aygıt hatalarını, donanım kimliklerini, yüklü sürücü kanıtlarını ve Windows Biyometrik Hizmeti kullanılabilirliğini inceler. İsteğe bağlı kurtarma, bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş bir hizmeti başlatır. Mevcut Windows Hello ve uygulama istekleri geçerli ilke altında sürebilir. Biyometrik şablonlar, Windows Hello verileri, kimlik bilgileri, aygıt yapılandırması, izinler, ilke, sürücüler ve başlangıç türü korunur. Kayıt, şablon silme, algılayıcı sıfırlama, izin değişikliği veya sürücü yükleme yoktur. Doğrulama yalnızca bildirilen algılayıcı yapılandırmasını ve hizmetin çalışma durumunu kapsar; yakalamayı, eşleştirmeyi, oturum açmayı veya uygulama erişimini kapsamaz.

### İnsan Arabirimi Aygıtı Hizmeti

İnsan Arabirimi Aygıtı Hizmeti'nin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut kısayol düğmeleri, medya denetimleri ve diğer HID özellikleri sürebilir. Aygıtlar, sürücüler, düğme eşlemeleri, erişilebilirlik ayarları, giriş ayarları, ilke, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir aygıt, sürücü, eşleme veya giriş ayarı işlemi seçmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; düğme, denetim veya uygulama davranışını kapsamaz.

### Taşınabilir Aygıt Numaralandırıcı Hizmeti

Taşınabilir Aygıt Numaralandırıcı Hizmeti'nin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut uygulamalar bağlı taşınabilir aygıtları algılamayı ve onlara erişmeyi sürdürebilir. Aygıtlar, sürücüler, dosyalar, kitaplıklar, eşitleme ayarları, izinler, ilke, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir aygıt, dosya, kitaplık, eşitleme veya aktarım işlemi seçmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; numaralandırmayı, erişimi, eşitlemeyi veya aktarımı kapsamaz.

### Windows Görüntü Alma (WIA)

Windows Görüntü Alma hizmetinin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut tarayıcı ve durağan görüntü alma istekleri geçerli uygulama izinleri altında sürebilir. Aygıtlar, sürücüler, görüntü verileri, uygulama izinleri, gizlilik ayarları, aktarım ayarları, ilke, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir aygıt, sürücü, görüntü, izin, aktarım veya alma işlemi seçmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; tarayıcı, kamera, aktarım veya uygulama çalışmasını kapsamaz.

### Kabuk Donanımı Algılama

Sınırlı depolama denetleyicisi, disk ve taşınabilir aygıt Tak ve Kullan kanıtlarını ve yüklü sürücü başvurularını inceler, ardından Kabuk Donanımı Algılama hizmetinin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut Otomatik Kullan ve donanım takma bildirimleri geçerli ilke altında sürebilir. Aygıt gözlemleri ve saklanan geçmiş kurtarmaya yetki veremez. Aygıtlar, sürücüler, birimler, bağlama noktaları, Otomatik Kullan ayarları, işleyiciler, ilke, hizmet izinleri ve başlangıç türü korunur. Hiçbir aygıt veya ayar işlemi istenmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; bildirim teslimini, ortam hazırlığını, birim erişimini veya fiziksel aygıt çalışmasını kapsamaz.

### Aygıt Yükleme Hizmeti

Aygıt Yükleme Hizmeti'nin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Windows tarafından istenen mevcut aygıt yükleme işleri geçerli ilke altında sürebilir. Aygıtlar, sürücüler, paketler, yapılandırma, bağlamalar, sınıf ayarları, ilke, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir şey seçmez veya yüklemez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; aygıt yüklemeyi veya çalışmasını kapsamaz.

### Aygıt İlişkilendirme Hizmeti

Son aygıt kurulumu ve Tak ve Kullan geçmişini, ardından Aygıt İlişkilendirme Hizmeti'nin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Windows tarafından istenen mevcut aygıt ilişkilendirme işleri sürebilir. Eşleştirilmiş ve ilişkilendirilmiş aygıtlar, sürücüler, kimlik bilgileri, meta veriler, izinler, ilke, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir aygıt, eşleştirme, kaldırma, sıfırlama veya sürücü işlemi seçmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; bulmayı, eşleştirmeyi, ilişkilendirmeyi veya aygıt çalışmasını kapsamaz.

### Görüntü Geliştirme Hizmeti

Grafik bağdaştırıcısı ve monitör kanıtlarını inceler, ardından Görüntü Geliştirme Hizmeti'nin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut parlaklık, renk ve geliştirme istekleri sürebilir. Ekran yapılandırması, parlaklık, renk profilleri, HDR ayarları, aygıtlar, sürücüler, güç ayarları, ilke, hizmet izinleri ve başlangıç türü korunur. Hiçbir ekran ayarı değiştirilmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; geliştirme davranışını, görüntü kalitesini veya ekranın fiziksel çalışmasını kapsamaz.

### Metin Girişi Yönetimi Hizmeti

Klavye, fare ve HID aygıtı kanıtlarını inceler, ardından Metin Girişi Yönetimi Hizmeti'nin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut dokunmatik klavye, el yazısı, emoji ve gelişmiş metin girişi istekleri sürebilir. Sözlükler, el yazısı verileri, dil ve klavye ayarları, erişilebilirlik ve gizlilik ayarları, aygıtlar, sürücüler, ilke, hizmet izinleri ve başlangıç türü korunur. Hiçbir metin girişi, tuş vuruşu veya el yazısı içeriği toplanmaz. Doğrulama yalnızca kararlı hizmet durumunu kapsar; metin girişini, tanımayı veya aygıt çalışmasını kapsamaz.

### Windows Yazı Tipi Önbellek Hizmeti

Windows Yazı Tipi Önbellek Hizmeti'nin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut uygulamalar geçerli yazı tipi önbelleğini kullanmayı sürdürebilir. Yüklü yazı tipleri, yazı tipi dosyaları, uygulama verileri, önbellek dosyaları, ilke, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir yazı tipini yüklemez veya kaldırmaz ve önbelleği silmez ya da yeniden oluşturmaz. Doğrulama yalnızca kararlı hizmet durumunu kapsar; metin işlemeyi veya uygulama davranışını kapsamaz.

### İşlev Bulma Sağlayıcısı Ana Bilgisayarı

İşlev Bulma Sağlayıcısı Ana Bilgisayarı'nın kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut uygulamalar yüklü sağlayıcılar aracılığıyla ağ aygıtlarını bulmayı sürdürebilir. Ağ profilleri, paylaşımlar, aygıtlar, sağlayıcılar, güvenlik duvarı kuralları, bulma ayarları, kimlik bilgileri, ilke, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir aygıt, paylaşım, sağlayıcı, güvenlik duvarı kuralı veya bulma ayarı işlemi seçmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; sağlayıcı sağlığını, aygıt bulmayı veya bağlantıyı kapsamaz.

### İşlev Bulma Kaynak Yayını

İşlev Bulma Kaynak Yayını'nın kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Bu PC, mevcut ağ bulma ve güvenlik duvarı ayarları aracılığıyla bulunabilir hâle gelebilir. Ağ profilleri, paylaşımlar, yayımlanan kaynaklar, güvenlik duvarı kuralları, bulma ayarları, kimlik bilgileri, ilke, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir paylaşım, kaynak, güvenlik duvarı kuralı veya bulma ayarı işlemi seçmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; yayımlamayı, görünürlüğü, erişimi veya bağlantıyı kapsamaz.
