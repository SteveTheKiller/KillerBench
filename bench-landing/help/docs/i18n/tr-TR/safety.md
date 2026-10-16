# Değişiklikler nasıl güvenli kalır

KillerBench tanılamayı ve değişikliği ayrı işler olarak ele alır. Tanılama yalnızca okur. Tam sistem taraması hiçbir zaman onarım çalıştırmaz. Belirli bir onarım veya işlem seçin, seçeneklerini ve ön koşullarını gözden geçirin ve önerilen değişikliği onaylayın.

## Değişiklikten önce

1. **Güncel kanıt.** Araç geçerli durumu yeniden okur. Kaydedilmiş bir rapor veya eski bir olay hiçbir zaman bir değişikliğe yetki vermez.
2. **Tam uygunluk.** Değişiklik yalnızca kanıt, düzelttiği sorunla tam olarak eşleştiğinde sunulur. Eksik olan her şey değişikliği engeller.
3. **Onayınız.** Seçilen işlemi onay iletişim kutusunda gözden geçirin. Sürücünün şifresini çözmek gibi bazı işlemler ikinci bir onay gerektirir. İletişim kutusu etkin uygulama temasını izler; görünümü işlemin gereksinimlerini değiştirmez.
4. **Ön koşullar.** Yönetici hakları, yeniden başlatma durumu ve ilke ya da aygıt yönetimi sahipliği denetlenir. Bir ilkenin sahip olduğu ayarlara dokunulmaz.
5. **Bekleyen kayıt.** KillerBench, Windows'tan bir şey yapmasını istemeden önce ne yapmak üzere olduğunu yazar.
6. **Son yeniden denetim.** İstekten hemen önce her şey yeniden denetlenir, böylece arada oluşan bir değişiklik işlemi durdurur.

## Değişiklikten sonra

- KillerBench sonucu geri okur ve yalnızca Windows beklenen durumu gösterdiğinde **Doğrulandı** bildirir.
- Windows başarı döndürür ancak durum değişmezse, sonuç bunu belirtir ve hiçbir şey yinelenmez.
- Her değişiklik `%LOCALAPPDATA%\KillerBench` altındaki bir günlüğe kaydedilir.

## Bir şey kesintiye uğrarsa

Desteklenen günlüklü işlemler, bir istek Windows'a ulaşmış olabilir ve doğrulama tamamlanmamışsa bekleyen kaydı saklar. Bu kaydı incelemek için işlemin belgelenmiş mutabakat yolunu veya eşleşen `-recover` komutunu kullanın. Mutabakat otomatik bir geri alma değildir. Sonuç belirsizse günlüğü saklayın ve değişikliği elle yinelemeyin. Çökme, iptal ve sonraki önyükleme davranışı her işlem için hâlâ yerel doğrulama gerektirir.

## KillerBench'in yapmayacakları

- TPM'yi temizlemez, kendi oluşturmadığı koruyucuları silmez ve kurtarma parolalarını göstermez.
- Grup ilkesinin veya aygıt yönetiminin denetlediği bir ayarı değiştirmez.
- Yalnızca geçmiş bir olay veya içe aktarılan bir rapor bir sorundan bahsediyor diye onarım çalıştırmaz.

Tanılama ve rapor analizi yerel olarak çalışır. Seçtiğiniz bir işlem, yazılım indirmeleri veya kurtarma anahtarının Active Directory'ye ya da Entra ID'ye yedeklenmesi gibi dış bir hedefle iletişim kurabilir. Onaylamadan önce hedefi gözden geçirin. Kaydedilmiş yakalamalar ve ham kanıtlar makine kimliğini ve kaynak yollarını içerebilir; bu nedenle paylaşmadan önce gözden geçirin.

## Geliştirme durumu

Onarım kapsamı hâlâ genişlemektedir. Başarılı bir derleme veya benzetimli bir denetim, yerel onarım davranışını kanıtlamaz. Deponun `VM-VALIDATION.md` dosyası, tek kullanımlık Windows makinesi senaryolarını ve kalan doğrulamaları izler.
