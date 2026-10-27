# Kurulum

Kurulum yeni bir bilgisayarı hazırlar. Seçtiğiniz uygulamaları ve yapılandırmayı gözden geçirir ve bilgisayara ad verir. Kendi başına hiçbir şey indirmez veya yüklemez.

Adım adım kılavuzlar: [Yeni bilgisayar kurma](set-up-new-pc.html).

## Bir yükleyiciyi çalıştırmadan önce denetleme

Komut satırı, bir satıcı yükleyicisini siz çalıştırmadan önce doğrulayabilir:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

KillerBench bir yükleyiciye yalnızca satıcının tam kuruluş adıyla geçerli bir Authenticode imzası taşıdığında güvenir. Yükleyiciyi hiçbir zaman çalıştırmaz.

## Bu bölümdeki araçlar

Yeni bir bilgisayarı hazırlayın: uygulamalar, ad ve yapılandırma.

### Kurulum

Açıkça seçilen hazırlama uygulamalarını ve yapılandırma gruplarını gözden geçirir. Geçerli kaldırma, uygulama paketi ve Office Tıkla-Çalıştır kayıtları, yükleyiciler indirilmeden veya çalıştırılmadan ilişkilendirilir. Yapılandırma seçimleri sonuçları açıklar ve uygulandığında ayrıca korunan iş akışlarına yönlendirir. Hiçbir indirme, yükleme, hesap, güncelleştirme, temizleme, ilke, kayıt defteri, hizmet, profil veya yeniden başlatma işlemi gerçekleşmez.

### Bilgisayar adı

Önerilen bilgisayar adını geçerli kimliğe ve etki alanı rolüne göre gözden geçirir. Yeniden adlandırma, kimlik bilgisi toplama veya yeniden başlatma yoktur. Adın kullanılabilirliği, izinler ve uygulama bağımlılıkları doğrulanmamış olarak kalır.
