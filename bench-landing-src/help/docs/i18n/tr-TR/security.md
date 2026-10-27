# Güvenlik

Güvenlik; Microsoft Defender'ı, güvenlik duvarını, sertifikaları ve oturum açma hizmetlerini inceler. Ayrıca BitLocker ve Windows Hello'nun bağlı olduğu TPM ve platform güvenliği durumunu da gösterir.

Adım adım kılavuzlar: [BitLocker ile sürücü şifreleme](encrypt-with-bitlocker.html) ve [Windows'u daha gizli hâle getirme](make-windows-private.html).

## Bu bölümdeki araçlar

Korumayı, güvenlik duvarını, sertifikaları ve oturum açma hizmetlerini inceleyin.

### Microsoft Defender

Defender korumasını ve imza durumunu inceler. Otomatik onarım, imzaları yalnızca Defender etkin koruma ve güncel olmayan imzalar bildirdiğinde yeniler. Yapılandırılmış imza kaynaklarını kullanır. Koruma, dışlama veya ilke değişikliği yoktur; kötü amaçlı yazılım taraması yoktur.

### Windows Güvenlik Merkezi

Ham sağlayıcı durumuyla birlikte sınırlı virüsten koruma kayıtlarını, tam hizmet hatası geçmişini ve geçerli Windows Güvenlik Merkezi hazırlığını, koruma sağlığını yorumlamadan gözden geçirir. Boş bir envanter bir koruma sağlığı kararı değildir ve kayıt sayısı, ham durum veya geçmiş hatalar kurtarmaya ya da bir güvenlik değişikliğine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut güvenlik durumu izleme ve bildirimler sürebilir. Virüsten koruma yapılandırması, sağlayıcı kayıtları, koruma ayarları, dışlamalar, bildirimler, ilke ve başlangıç türü korunur. Hiçbir tarama, güncelleştirme veya koruma değişikliği istenmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; virüsten korumanın varlığını, koruma durumunu, imzaların güncelliğini, bildirim teslimini, kayıt sahipliğini veya geçerli ilkeyi kapsamaz.

### Güvenlik duvarı hizmetleri

Etki Alanı, Özel ve Ortak profil etkinleştirme ayarlarını ActiveStore'dan okur, ardından Temel Filtreleme Altyapısı ve Windows Defender Güvenlik Duvarı hizmetlerini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş hizmetleri başlatır. Mevcut filtreleme ilkesi geçerlik kazanabilir ve ağ ya da uzaktan erişimi kesintiye uğratabilir. Başlangıç türleri, hizmet izinleri, güvenlik duvarı kuralları, profiller ve IPsec ayarları korunur. Sıfırlama, kural değişikliği veya hizmet durdurma yoktur. Doğrulama yalnızca hizmetlerin çalışma durumunu belirler; geçerli koruma ve bağlantı doğrulanmamış olarak kalır.

### Sertifika süre sonu

Sertifika tarihlerini inceler ve makine ile geçerli kullanıcının kişisel depoları için yalnızca önbellekten yerel güven zincirleri oluşturur, ardından tek bir sınırlı işletim sonucu döndürür. İptal, sunucu tarafından sunum, bağlamalar, kayıt, ilke sahipliği, uzak güven ve özel anahtarın kullanılabilirliği doğrulanmamış olarak kalır. Hiçbir sertifika, güven ayarı, kayıt, bağlama, özel anahtar, uygulama, hizmet veya ilke değiştirilmez.

### Kimlik Bilgisi Yöneticisi

Kimlik Bilgisi Yöneticisi hizmetinin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut uygulamalar depolanan kimlik bilgileri için isteklerini sürdürebilir. Kasa içeriği, parolalar, geçiş anahtarları, sertifikalar, belirteçler, kimlik bilgisi kayıtları, izinler, ilke, hizmet izinleri ve başlangıç türü korunur. Hiçbir kimlik bilgisi içeriği veya işlemi istenmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; kimlik bilgisi alımını, kimlik doğrulamayı veya uygulama erişimini kapsamaz.

### Web Hesabı Yöneticisi

Yükü temizlenmiş hesap aracısı geçmişini ve tam Web Hesabı Yöneticisi kullanılabilirliğini gözden geçirir. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut uygulamalar geçerli ilke altında hesap ve belirteç isteklerini sürdürebilir. Geçmiş olaylar kurtarmaya yetki veremez veya geçerli hesap durumunu belirleyemez. Hesaplar, kimlik bilgileri, belirteçler, tanımlama bilgileri, uygulama kayıtları, izinler, oturum açma durumu, ilke, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir hesap, kimlik bilgisi, belirteç, tanımlama bilgisi, izin veya oturum açma işlemi seçmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; kimlik doğrulamayı, belirteç verilmesini veya uygulama erişimini kapsamaz.

### Microsoft Hesabı Oturum Açma Yardımcısı

Yükü temizlenmiş hesap aracısı geçmişini ve tam Microsoft Hesabı Oturum Açma Yardımcısı kullanılabilirliğini gözden geçirir. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut Microsoft hesabı oturum açma istekleri geçerli ilke altında sürebilir. Geçmiş olaylar kurtarmaya yetki veremez veya geçerli hesap durumunu belirleyemez. Hesaplar, kimlik bilgileri, belirteçler, aygıt kaydı, uygulama verileri, izinler, oturum açma durumu, ilke, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir hesap, kimlik bilgisi, belirteç, kayıt, izin veya oturum açma işlemi seçmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; kimlik doğrulamayı, hesap bağlantısını veya uygulama erişimini kapsamaz.
