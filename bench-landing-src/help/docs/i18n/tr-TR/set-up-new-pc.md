# Yeni bilgisayar kurma

Yeni veya yeni sıfırlanmış bir PC'yi hazırlarken bunu kullanın. KillerBench bilgisayarı, yüklemeyi planladığınız uygulamaları ve vermek istediğiniz adı gözden geçirir, ardından her değişikliği yapan korumalı araçlara yönlendirir.

## Bir temel durumla başlayın

1. **Tanılama** bölümünden **Tam sistem taraması** çalıştırın ve raporu bilgisayarın başlangıç durumunun kaydı olarak saklayın.
2. Bekleyen güncelleştirmeleri yükleyin. Başarısız olan olursa [Windows Update'i düzeltme](fix-windows-update.html) sayfasına bakın.

## Uygulamaları ve yapılandırmayı gözden geçirme

1. **Kurulum** bölümünü açın ve **Kurulum** öğesini seçin.
2. Ayarlamayı planladığınız uygulamaları ve yapılandırma gruplarını seçin ve **Tanılama çalıştır** düğmesine basın. İnceleme bunları zaten yüklü olanlarla karşılaştırır. KillerBench yükleyicileri indirmez veya çalıştırmaz.
3. Bir satıcı yükleyicisini çalıştırmadan önce onu komut satırından doğrulayın. KillerBench bir yükleyiciye yalnızca o satıcının tam kuruluş adıyla geçerli bir imza taşıdığında güvenir.

## Bilgisayara ad verme

1. **Bilgisayar adı** öğesini seçin, adı girin ve geçerli kimlik ve etki alanı rolüne göre gözden geçirmek için **Tanılama çalıştır** düğmesine basın.
2. Bir çalışma grubu bilgisayarında, yeniden adlandırmak için **Güncel denetimlerden sonra bu çalışma grubu PC'sini yeniden adlandır** seçeneğini işaretleyin. Tamamlamak için yeniden başlatma gerekir.

## Koruma ve düzenleme

- [BitLocker ile sürücüyü şifreleyin](encrypt-with-bitlocker.html) ve kurtarma anahtarını yedekleyin.
- [Windows'u daha gizli hâle getirin](make-windows-private.html) ve **Bakım** bölümündeki **Windows'u sadeleştir** aracını gözden geçirin.
- Her şey yerine oturduğunda **Bir geri yükleme noktası hazırla** ile bir geri yükleme noktası oluşturun.

## Komut satırından

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Bkz. [workflow](cli.html#cmd-workflow) ve [restore-point](cli.html#cmd-restore-point).

## İlgili

- [Kurulum bölümü](onboarding.html)
- [İlk tanılamanız](first-run.html)
