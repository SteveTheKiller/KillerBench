# Ağ

Ağ; bağlantıları, DNS'i, yolları, kablosuz ve kablolu oturum açmayı, dosya paylaşımını ve uzaktan erişimi inceler. Başka bir ana bilgisayara ulaşan araçlar ayarladığınız hedefi ve zaman aşımını kullanır ve bir onarımı onaylamadığınız sürece hiçbir şey değiştirilmez.

Adım adım kılavuzlar: [Ağ ve internet sorunlarını giderme](fix-network.html).

## Bu bölümdeki araçlar

Bağlantıları, DNS'i, yolları, kablosuz ağı, paylaşımı ve uzaktan erişimi inceleyin.

### Ağ

Bağdaştırıcıları, adres durumunu ve kaynağını, ağ geçitlerini, DNS yapılandırmasını, geçerli DNS ad alanı ilkesini, TCP ayarlarını, yerel Winsock protokol ve ad alanı sağlayıcısı katalog yapısını, IP yönlendirme tablosunu ve Hosts dosyası eşlemelerini inceler. Otomatik onarım etkin DNS İstemcisi, DHCP İstemcisi, SMB istemcisi, Ağ Konumu Tanıma, Ağ Listesi ve IP Yardımcısı hizmetlerini başlatabilir ve kullanılabilir bir IPv4 adresi olmayan bağlı fiziksel DHCP bağdaştırıcılarını yenileyebilir. Ağ tanımlama hizmetlerinin başlatılması ağ kimliğini yeniden hesaplayabilir ve mevcut farklı bir güvenlik duvarı profilini uygulayabilir. IP Yardımcısı mevcut IPv6 geçiş, bağlantı noktası ara sunucusu ve ağ bildirimi özelliklerini etkinleştirebilir. Hizmet kurtarma çalışma durumunu doğrular; başarılı ad çözümlemeyi, yol seçimini, ağ tanımlamayı veya tünel çalışmasını doğrulamaz. Yollar, Hosts girdileri, DNS sunucuları, profiller, güvenlik duvarı kuralları, Winsock sağlayıcıları, geçiş yapılandırması, başlangıç türleri ve ilkeler korunur. Yenileme, kiralanan adresleri değiştirebilir ve bağlantıları kesintiye uğratabilir. Statik ayarlar ve sanal bağdaştırıcılar korunur. Ağ geçidinin tam ICMP yanıtları internete ulaşılabilirliği doğrulamaz.

### Kablosuz ağ

Ağ aygıtlarını ve WLAN Otomatik Yapılandırmasının kullanılabilirliğini inceler. İsteğe bağlı kurtarma, bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş bir hizmeti başlatır; kaydedilmiş profiller otomatik olarak yeniden bağlanabilir. Profiller, kimlik bilgileri, ilke, radyo ayarları ve başlangıç türleri korunur. Bağdaştırıcı sıfırlama veya sürücü yükleme yoktur. Doğrulama çalışma durumunu kapsar; kablosuz kimlik doğrulamayı veya bağlantıyı kapsamaz.

### Kablolu kimlik doğrulama (802.1X)

802.1X gerektiren ağlar için ağ aygıtlarını ve Kablolu Otomatik Yapılandırmayı inceler. İsteğe bağlı kurtarma, bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş bir hizmeti başlatır. Mevcut kimlik doğrulama profilleri etkinleşebilir ve ağ erişimini değiştirebilir. Sertifikalar, kimlik bilgileri, profiller, ilke ve başlangıç ayarları korunur. Doğrulama yalnızca çalışma durumunu kapsar; kimlik doğrulamayı veya bağlantıyı kapsamaz.

### Ağ Bağlantıları

Sınırlı geçerli ağ profili atamalarını, tam hizmet hatası geçmişini ve geçerli Ağ Bağlantıları hizmeti hazırlığını gözden geçirir. Eksik kanıt önceliklidir. Ortak profiller ve geçmiş hatalar bağlam olarak kalır; kurtarmaya veya bir ağ değişikliğine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut ağ yönetimi arabirimleri sürebilir. Bağdaştırıcılar, adresler, yollar, DNS ayarları, bağlantı profilleri, kimlik bilgileri, güvenlik duvarı yapılandırması, ilke ve başlangıç türü korunur. Hiçbir bağlantı veya ağ ayarı değiştirilmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; bağdaştırıcı çalışmasını, adres atamasını, yönlendirmeyi, ad çözümlemeyi veya bağlantıyı kapsamaz.

### WinHTTP Otomatik Ara Sunucu

Depolanan WinHTTP varsayılanlarını, geçerli hesabın İnternet ayarlarını, tam hizmet hatası geçmişini ve geçerli WinHTTP Otomatik Ara Sunucu hazırlığını gözden geçirir. Eksik kanıt önceliklidir. Ara sunucu yapılandırması ve geçmiş hatalar hizmet kurtarmaya veya bulmaya yetki veremez. Durdurulmuş, isteğe bağlı başlayan bir hizmet normal olabilir. Tam olarak bu araç seçildiğinde, isteğe bağlı kurtarma güncel bağımlılık denetimlerinden sonra yalnızca durdurulmuş etkin hizmeti başlatabilir. Mevcut uygulamalar geçerli ayarlar altında WPAD veya PAC bulmayı sürdürebilir. Ara sunucu tanımları, PAC URL'leri, kimlik bilgileri, ağ ayarları, güvenlik duvarı yapılandırması, ilke, hizmet izinleri ve başlangıç türü korunur. Hiçbir URL getirilmez ve hiçbir ara sunucu veya ağ ayarı değiştirilmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; bulmayı, ulaşılabilirliği, kimlik doğrulamayı, ilke sahipliğini veya uygulama kullanımını kapsamaz.

### Windows Bağlantı Yöneticisi

Sınırlı geçerli ağ profili atamalarını, tam hizmet hatası geçmişini ve geçerli Windows Bağlantı Yöneticisi hazırlığını gözden geçirir. Eksik kanıt önceliklidir. Ortak profiller ve geçmiş hatalar bağlam olarak kalır; kurtarmaya veya bir bağlantı kararına yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Windows, mevcut profiller ve ilke altında otomatik bağlantı kararlarını sürdürebilir. Bağdaştırıcılar, profiller, kimlik bilgileri, ölçümlü bağlantı, maliyet, dolaşım, güvenlik duvarı, VPN ve kimlik doğrulama ayarları ile başlangıç türü korunur. Hiçbir profil, bağdaştırıcı veya ayar değiştirilmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; profil uygulanabilirliğini, bağlantı seçimini, kimlik doğrulamayı, yönlendirmeyi, ad çözümlemeyi veya bağlantıyı kapsamaz.

### Ağ Kurulumu Hizmeti

Sınırlı geçerli bağdaştırıcı protokol bağlamalarını, tam hizmet hatası geçmişini ve geçerli Ağ Kurulumu Hizmeti hazırlığını gözden geçirir. Eksik kanıt önceliklidir. Bağlama durumu ve geçmiş hatalar hizmet kurtarmaya veya bir kurulum değişikliğine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Bekleyen mevcut ağ kurulumu Windows ilkesi altında sürebilir. Bağdaştırıcılar, sürücüler, sürücü paketleri, bağlamalar, adresler, yollar, DNS ayarları, profiller, kimlik bilgileri, güvenlik duvarı yapılandırması, ilke ve başlangıç türü korunur. KillerBench hiçbir sürücü veya yapılandırma değişikliği seçmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; bağdaştırıcı kullanılabilirliğini, sürücü uygunluğunu, kurulumun tamamlanmasını, adres atamasını, yönlendirmeyi, ad çözümlemeyi veya bağlantıyı kapsamaz.

### Dosya paylaşımı (SMB)

Windows SMB sunucusunun kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş bir Sunucu hizmetini başlatır. Mevcut dosya ve yazıcı paylaşımları geçerli izinleri ve güvenlik duvarı ilkesi altında ulaşılabilir hâle gelebilir. Hiçbir paylaşım, izin, SMB ayarı, güvenlik duvarı kuralı, kimlik bilgisi, ilke veya başlangıç türü değiştirilmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; istemci erişimini veya dosya aktarımını kapsamaz.

### Uzaktan Erişim Bağlantı Yöneticisi

Gizli bilgileri toplamadan, sınırlı geçerli hesap VPN profili yapılandırmasını, tam hizmet hatası geçmişini ve geçerli Uzaktan Erişim Bağlantı Yöneticisi hazırlığını gözden geçirir. Boş bir envanter kasıtlı olabilir ve profil varlığı, bağlantı etiketleri veya geçmiş hatalar kurtarmaya ya da bir bağlantı denemesine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut çevirmeli ağ ve VPN istekleri sürebilir. Bağlantı profilleri, kimlik bilgileri, yollar, bağdaştırıcılar, kimlik doğrulama ayarları, ilke ve başlangıç türü korunur. Hiçbir bağlantı başlatılmaz. Doğrulama yalnızca kararlı hizmet durumunu kapsar; profil uygulanabilirliğini, sunucuya ulaşılabilirliği, kimlik doğrulamayı, tünel anlaşmasını, yol yüklemeyi, ad çözümlemeyi veya bağlantıyı kapsamaz.

### Uzak Masaüstü Hizmetleri

Yerel Uzak Masaüstü etkinleştirmesini, Ağ Düzeyinde Kimlik Doğrulamayı ve dinleyici bağlantı noktasını, sınırlı bağlantı ve oturum geçmişini, tam hizmet hatası geçmişini ve geçerli hizmet hazırlığını gözden geçirir. Eksik kanıt önceliklidir. Geçmiş kayıtlar hizmet kurtarmaya veya yapılandırma değişikliklerine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut bir dinleyici geçerli ilke, NLA, güvenlik duvarı kuralları ve izinler altında ulaşılabilir hâle gelebilir. Uzak Masaüstü etkinleştirmesi, dinleyiciler, sertifikalar, bağlantı noktaları, güvenlik duvarı kuralları, NLA, izinler, oturumlar, ilke, hizmet izinleri ve başlangıç türü korunur. Hiçbir dinleyici, güvenlik duvarı kuralı, hesap erişimi, sertifika veya oturum oluşturulmaz ya da değiştirilmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; geçerli ilkeyi, dinleyici bağlamasını, güvenlik duvarı erişimini, sertifika kimliğini, izinleri, kimlik doğrulamayı, ağa ulaşılabilirliği, bağlantı başarısını veya oturum oluşturmayı kapsamaz.

### Windows Uzaktan Yönetimi

Sınırlı WinRM dinleyici yapılandırmasını, işletim geçmişini, tam hizmet hatası geçmişini ve geçerli hizmet hazırlığını gözden geçirir. Eksik kanıt önceliklidir. Devre dışı dinleyiciler veya geçerli hizmet endişeleri incelenmeyi gerektirir; geçmiş kayıtlar ise kurtarmaya veya yapılandırma değişikliklerine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut WS-Management dinleyicileri geçerli kimlik doğrulama, sertifika, güvenlik duvarı, TrustedHosts ve ilke ayarları altında ulaşılabilir hâle gelebilir. Dinleyiciler, adresler, bağlantı noktaları, sertifikalar, kimlik doğrulama, şifreleme, TrustedHosts, güvenlik duvarı kuralları, izinler, kotalar, ilke, hizmet izinleri ve başlangıç türü korunur. Hiçbir dinleyici, güvenlik duvarı kuralı, güven girdisi, sertifika, kimlik bilgisi veya uzak oturum oluşturulmaz ya da değiştirilmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; geçerli ilkeyi, dinleyici bağlamasını, güvenlik duvarı erişimini, sertifika geçerliliğini, kimlik doğrulamayı, izinleri, istemcilerin ulaşabilirliğini veya uzaktan komut yürütmeyi kapsamaz.
