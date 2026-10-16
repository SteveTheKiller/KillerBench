# BitLocker

BitLocker sürücü şifrelemesini, koruyucuları ve kurtarma bilgilerini inceler, ardından bir sürücüyü yedeklemenize, korumanıza veya değiştirmenize yardımcı olur. Yalnızca bu bilgisayarda çalışır.

Adım adım kılavuzlar: [BitLocker ile sürücü şifreleme](encrypt-with-bitlocker.html).

## BitLocker aracını kullanma

Sürücü harfini ve bir **İşlem** seçin, ardından **Tanılama çalıştır** düğmesine basın.

- **İncele**, şifreleme durumunu, koruma durumunu ve koruyucu kimliklerini okur. Varsayılandır ve hiçbir şeyi değiştirmez.
- **Kurtarmayı yedekle**, seçili kurtarma parolasını korunan sürücünün dışındaki yeni bir dosyaya yazar ve doğrular. Önce **Kurtarma koruyucularını bul** düğmesine basın, koruyucuyu seçin, ardından yedeklemeyi onaylayın.
- **Şifrele**, seçili kurtarma parolasını yedekler, yedeği doğrular, ardından seçtiğiniz yöntemle şifrelemeyi bir kez başlatır. Windows sürücüsü bir TPM koruyucusu ve bir kurtarma parolası gerektirir. Bir veri sürücüsü yalnızca kurtarma parolası gerektirir.
- **Korumayı sürdür**, korumanın askıya alındığı tamamen şifreli bir sürücüde korumayı yeniden açar.

Bunların her biri kendi onay kutusunu gerektirir ve KillerBench işlem yapmadan hemen önce sürücüyü yeniden denetler.

## Diğer BitLocker işlemleri

**Tanılama çalıştır** dışında kalan değişiklikler için **Diğer BitLocker işlemleri** öğesini açın. Bir işlem seçin, tam olarak neler gerektirdiğini okumak için **Önizleme** düğmesine basın, ardından onaylayıp **İşlemi çalıştır** düğmesine basın.

- **Otomatik kilit açmayı etkinleştir**, şifreli bir veri sürücüsünün Windows başladığında kilidinin açılmasını sağlar. Windows sürücüsünün koruma açıkken tamamen şifreli olması gerekir.
- **TPM koruyucusu ekle** veya **Kurtarma parolası ekle** yeni bir koruyucu oluşturur. KillerBench yeni bir kurtarma parolasını hiçbir zaman göstermez veya günlüğe yazmaz; bu nedenle hemen yedekleyin.
- **Kurtarma parolasını Active Directory'ye yedekle** veya **Microsoft Entra ID**, yukarıda seçtiğiniz koruyucuyu gönderir. KillerBench, Windows'un isteği kabul ettiğini bildirir. Dizindeki kopyayı okuyamaz; bu nedenle onu orada doğrulayın.
- **Bu sürücünün şifresini çöz**, örneğin gerekli bir yöntemle yeniden şifrelemeden önce şifre çözmeyi başlatır. Sürücü sonrasında korumasız kalacağı için ikinci bir onay gerektirir ve başka sürücüler kilitlerini Windows sürücüsünden otomatik olarak açıyorsa Windows sürücüsünü reddeder.

## TPM ve FIPS

TPM mevcut ancak hazır değilse, Güvenlik bölümü onu temizlemeden hazırlayabilir. KillerBench TPM'yi hiçbir zaman temizlemez, çünkü bu BitLocker ve Windows Hello tarafından tutulan anahtarları yok eder. FIPS algoritma ilkesi anahtarı, FIPS onaylı BitLocker PIN'lerine ihtiyaç duyan kuruluşlar için açık ve geri alınabilir bir onarım olarak kullanılabilir.

## Bu bölümdeki araçlar

Şifrelemeyi, koruyucuları ve kurtarma bilgilerini inceleyin, ardından bunları yedekleyin veya sürücüyü koruyun.

### BitLocker

BitLocker seçeneklerini önizler ve seçili birimi inceler. Bu iş akışı şifreleme veya kurtarma yedeklemesi yapmaz.
