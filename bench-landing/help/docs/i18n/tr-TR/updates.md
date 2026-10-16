# Güncelleştirmeler

Güncelleştirmeler; Windows Update'i, arkasındaki hizmetleri ve bileşen deposunu kapsar. Nelerin yüklü olduğunu, nelerin beklediğini ve bir güncelleştirmenin neden başarısız olduğunu görebilir, yüklemek için herhangi birini seçmeden önce kullanılabilir güncelleştirmeleri gözden geçirebilirsiniz.

Adım adım kılavuzlar: [Windows Update'i düzeltme](fix-windows-update.html) ve [Sistem dosyalarını onarma](repair-system-files.html).

## Bu bölümdeki araçlar

Bakım etkinliğini izleyin, kullanılabilir güncelleştirmeleri gözden geçirin ve Windows Update zincirini onarın.

### Bakım etkinliği izleyicisi

Sınırlı Windows Update ve Setup bakım etkinliğini bir veya daha fazla anlık görüntü boyunca gözlemler. Seçenekler yenileme zamanlamasını ve güncelleştirme olaylarının, güncelleştirme geçmişinin, tam Windows Update zamanlanmış görevinin, tam hizmet durumlarının, yeniden başlatma göstergelerinin, geçerli hesabın arka plan aktarımlarının, Teslim İyileştirme etkinliğinin ve gizlilik açısından sınırlı Windows Installer etkinliğinin dahil edilip edilmeyeceğini denetler. İşlem kimlikleri, komut satırları, kullanıcılar ve yollar hariç tutulur. Etkinlik ilerlemeyi, tamamlanmayı, takılmayı veya bir arızayı kanıtlamaz. Hiçbir güncelleştirme, kurulum, aktarım, görev, hizmet, işlem, ilke, yeniden başlatma veya onarım işlemi istenmez.

### Kullanılabilir güncelleştirmeler

Yapılandırılmış kaynağı, Windows Update'i veya zaten kayıtlı bir Microsoft Update hizmetini kullanarak seçili yazılım ve sürücü güncelleştirmelerini bulur. Kaynak hazırlığı bulmadan önce denetlenir. Yükleme, lisans kabulü, kaynak kaydı veya ilke değişikliği yoktur. Bu, bakım onarımından ayrıdır.

### Windows Update

Sınırlı Windows bakım paketi, isteğe bağlı özellik ve yetenek durumlarını envantere alır, yeniden başlatma göstergelerini, boş alanı ve güncelleştirme hizmetlerini denetler, Teslim İyileştirme'yi başlatmadan gözlemler, ardından bileşen deposunu ve korumalı sistem dosyalarını tarar. Bekleyen paket, özellik veya yetenek durumları incelenmeyi gerektirir ancak bir bileşen işlemine yetki vermez. Otomatik onarım etkin Windows Update, BITS, Şifreleme Hizmetleri, Windows Modül Yükleyicisi ve Update Orchestrator hizmetlerini başlatabilir. Update Orchestrator'ın başlatılması, geçerli ilke altında mevcut zamanlanmış güncelleştirme etkinliğini sürdürebilir. Paketler, isteğe bağlı özellikler, yetenekler, Teslim İyileştirme önbelleği, eşler, bant genişliği, ilke, kaynak kaydı, zamanlamalar ve başlangıç ayarları korunur. DISM veya SFC canlı bozulma kanıtı gerektirir. Bu iş akışı hiçbir güncelleştirmeyi seçmez veya onaylamaz.

### Windows Update Hizmeti

Tam Windows Update Hizmeti durumunu, kayıtlı hizmet DLL'sini, sınırlı geçerli güncelleştirme etkinliğini ve tam hizmet hatası geçmişini denetler. DLL gözlemleri, güncelleştirme etkinliği ve geçmiş hatalar bir hizmet başlatmaya veya güncelleştirme işlemine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık ve yürütülebilir dosya kaydı denetimlerinden sonra yalnızca durdurulmuş bir otomatik hizmeti başlatır. Durdurulmuş, isteğe bağlı başlayan bir hizmet yalnızca bilgi amaçlı kalır. Mevcut güncelleştirme etkinliği geçerli ilke altında sürebilir. Güncelleştirme ilkesi, kaynak kaydı, zamanlamalar, yeniden başlatma ayarları, paketler, BITS işleri, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir güncelleştirmeyi seçmez, onaylamaz veya yüklemez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; kaynağa ulaşılabilirliği, uygulanabilirliği, indirmeyi, yüklemeyi, tamamlanmayı veya yeniden başlatma hazırlığını kapsamaz.

### Teslim İyileştirme

Tam Teslim İyileştirme hizmeti durumunu denetler. Durdurulmuş, isteğe bağlı başlayan bir hizmet yalnızca bilgi amaçlı kalır. Yalnızca tam Duraklatıldı durumu kurtarma sunabilir. İsteğe bağlı kurtarma durum denetimini yineler, sınırlı tek bir devam isteği gönderir ve tutarlı iki Çalışıyor gözlemi gerektirir. Başlangıç türü, önbellek içeriği, eşler, bant genişliği, güncelleştirme kaynağı ve yönetilen ilke korunur. Aktarım etkinliği, önbellek sağlığı, güncelleştirme ilerlemesi ve tamamlanma doğrulanmamış olarak kalır.

### BITS hizmeti

Tam Arka Plan Akıllı Aktarım Hizmeti durumunu, geçerli hesabın aktarım işlerini ve sınırlı tam hizmet hatası geçmişini denetler. Geçmiş hatalar ve aktarım anlık görüntüleri bir hizmet başlatmaya yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut uygulamalar kuyruğa alınmış arka plan aktarımlarını sürdürebilir. İşler, öncelikler, sahipler, hedefler, ara sunucu ayarları, ilke ve başlangıç türü korunur. Doğrulama yalnızca kararlı hizmet durumunu kapsar; aktarım ilerlemesini, uygulama sahipliğini veya tamamlanmayı kapsamaz.

### Arka plan aktarımları (BITS)

Seçili hesap kapsamındaki BITS aktarım işlerini okur ve tam iş kimliklerini gözden geçirir. Kuyruk adları uygulama sahipliğini belirlemez. Hiçbir aktarım sürdürülmez, tamamlanmaz, iptal edilmez, silinmez veya yeniden atanmaz.

### Şifreleme Hizmetleri

Tam Şifreleme Hizmetleri durumunu, kayıtlı hizmet DLL'sini ve sınırlı tam hizmet hatası geçmişini denetler. Geçmiş hatalar ve DLL gözlemleri bir hizmet başlatmaya veya bileşen değişimine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut katalog, sertifika ve güncelleştirme işlemleri sürebilir. Sertifikalar, anahtarlar, kataloglar, güven ayarları, ilke ve başlangıç türü korunur. Doğrulama yalnızca kararlı hizmet durumunu kapsar; sertifika, imza, katalog veya güven doğrulamasını kapsamaz.

### Windows Modül Yükleyicisi

Tam Windows Modül Yükleyicisi durumunu, sınırlı bakım paketi geçişlerini ve tam hizmet hatası geçmişini denetler. Geçmiş hatalar ve paket durumları bir hizmet başlatmaya veya paket işlemine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut bakım etkinliği sürebilir. Paketler, bileşen deposu içeriği, kaynaklar, ilke ve başlangıç türü korunur. Doğrulama yalnızca kararlı hizmet durumunu kapsar; bileşen deposu sağlığını, paket uygulanabilirliğini, bakımın tamamlanmasını veya yeniden başlatma hazırlığını kapsamaz.

### Windows Update Medic Hizmeti

Tam Windows Update Medic Hizmeti durumunu, kayıtlı hizmet DLL'sini, sınırlı geçerli güncelleştirme etkinliğini ve tam hizmet hatası geçmişini denetler. DLL gözlemleri, güncelleştirme etkinliği ve geçmiş hatalar bir hizmet başlatmaya veya bileşen işlemine yetki veremez. Durdurulmuş, isteğe bağlı başlayan bir hizmet normal olabilir. Tam olarak bu araç seçildiğinde, isteğe bağlı kurtarma güncel bağımlılık denetimlerinden sonra yalnızca durdurulmuş etkin hizmeti başlatabilir. Windows düzeltmesi geçerli ilke altında sürebilir ve güncelleştirme bileşenlerini onarabilir ya da güncelleştirme etkinliğini tetikleyebilir. Güncelleştirme ilkesi, kaynak kaydı, zamanlamalar, paketler, BITS işleri, yeniden başlatma ayarları, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir güncelleştirmeyi seçmez ve hiçbir güncelleştirme ayarını değiştirmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; düzeltmeyi, bileşen sağlığını, uygulanabilirliği, indirmeyi, yüklemeyi veya yeniden başlatma hazırlığını kapsamaz.

### Update Orchestrator Hizmeti

Tam Update Orchestrator Hizmeti durumunu, tam yerleşik Windows Update görevini, sınırlı geçerli etkinliği ve tam hizmet hatası geçmişini denetler. Görev sonuçları, güncelleştirme etkinliği ve geçmiş hatalar bir hizmet başlatmaya veya güncelleştirme işlemine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık ve yürütülebilir dosya kaydı denetimlerinden sonra yalnızca durdurulmuş bir otomatik hizmeti başlatır. Durdurulmuş, isteğe bağlı başlayan bir hizmet yalnızca bilgi amaçlı kalır. Mevcut zamanlanmış güncelleştirme etkinliği geçerli ilke altında sürebilir ve önceden onaylanmış güncelleştirmeleri indirebilir veya yükleyebilir. Güncelleştirme ilkesi, kaynak kaydı, zamanlamalar, yeniden başlatma ayarları, paketler, BITS işleri, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir güncelleştirmeyi seçmez veya onaylamaz. Doğrulama yalnızca kararlı hizmet durumunu kapsar; uygulanabilirliği, indirmeyi, yüklemeyi, tamamlanmayı veya yeniden başlatma hazırlığını kapsamaz.

### Bileşen deposu temizliği

Windows'un bileşen deposu temizliği önerip önermediğini analiz eder. İsteğe bağlı bakım, yerini başkalarına bırakmış bileşenleri hemen kaldırır, ardından yeniden analiz eder. ResetBase veya elle WinSxS silme yoktur. Otomatik geri alma yoktur; geri kazanılan disk alanı ölçülmez.
