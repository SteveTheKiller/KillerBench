# BitLocker ile sürücü şifreleme

BitLocker'ı açmak, kurtarma anahtarını yedeklemek ve sürücünün korunduğunu doğrulamak için bunu kullanın. KillerBench şifrelemeyi başlatmadan önce kurtarma parolasını yedekler ve doğrular; kurtarma parolasını hiçbir zaman göstermez veya günlüğe yazmaz.

## Başlamadan önce

- KillerBench'i yönetici olarak çalıştırın.
- Windows sürücüsü bir TPM koruyucusu ve bir kurtarma parolası koruyucusu gerektirir. Bir veri sürücüsü yalnızca kurtarma parolası gerektirir.
- Kurtarma anahtarı yedeğinin nereye gideceğine karar verin: şifrelediğiniz sürücünün dışındaki bir dosyaya ve isteğe bağlı olarak Active Directory'ye veya Microsoft Entra ID'ye.

## Sürücüyü denetleme

1. **BitLocker** bölümünü açın ve **Birim** içinden sürücüyü seçin.
2. İşlemi **İncele** olarak bırakın ve **Tanılama çalıştır** düğmesine basın. Rapor şifreleme durumunu, koruma durumunu ve koruyucu kimliklerini gösterir.
3. TPM mevcut ancak hazır değilse, önce **Güvenlik** bölümünden hazırlayın. KillerBench hiçbir zaman TPM'yi temizlemez.

## Gereken koruyucuları ekleme

1. **Diğer BitLocker işlemleri** öğesini açın.
2. **TPM koruyucusu ekle** veya **Kurtarma parolası ekle** seçeneğini seçin, **Önizleme** düğmesine basın, **İşlemi çalıştır'a bastığımda bu değişikliği seçili sürücüde yap** seçeneğini işaretleyin, ardından **İşlemi çalıştır** düğmesine basın.
3. Yeni kurtarma parolasını hemen yedekleyin. KillerBench onu göstermez.

## Kurtarma anahtarını yedekleme ve şifreleme

1. **Kurtarma koruyucularını bul** düğmesine basın ve kurtarma parolası koruyucusunu seçin.
2. Şifrelediğiniz sürücünün dışında tam bir yedek dosya yolu girin.
3. **Şifrele** seçeneğini seçin, şifreleme yöntemini belirleyin ve **Tanılama çalıştır'ı seçtiğimde seçili kurtarma anahtarını yedekle, ardından şifrelemeyi başlat** seçeneğini işaretleyin.
4. **Tanılama çalıştır** düğmesine basın. KillerBench yedeği yazar, doğrular, ardından şifrelemeyi bir kez başlatır.
5. Şifreleme arka planda devam eder. Sürücünün koruma açıkken tamamen şifreli olduğunu bildirdiğini doğrulamak için daha sonra **İncele** işlemini çalıştırın.

## Active Directory'ye veya Entra ID'ye yedekleme

**Diğer BitLocker işlemleri** içinde, seçili koruyucu için dizin yedeğini seçin. KillerBench, Windows'un isteği kabul ettiğini bildirir ancak dizindeki kopyayı okuyamaz; bu nedenle onu Active Directory'de veya Entra yönetim merkezinde doğrulayın.

## Veri sürücüleri ve otomatik kilit açma

Bir veri sürücüsü şifrelendikten sonra **Otomatik kilit açmayı etkinleştir**, Windows başladığında sürücünün kilidinin açılmasını sağlar. Windows sürücüsünün zaten koruma açıkken tamamen şifreli olması gerekir.

## Komut satırından

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Bkz. [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) ve [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Her birinin, kesintiye uğrayan bir istek için eşleşen bir `-recover` komutu vardır.

## İlgili

- [BitLocker bölümü](bitlocker.html)
- [Güvenlik bölümü](security.html)
- [Değişiklikler nasıl güvenli kalır](safety.html)
