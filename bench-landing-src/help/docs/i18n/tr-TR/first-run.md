# İlk tanılamanız

[İndirme ve kurulum](setup.html) ile başlayın, ardından bir Tam sistem taraması çalıştırın. Tarama tanılama kanıtı toplar ve yerel bir günlük kaydeder; onarım çalıştırmaz. Bir bulgu, bilgisayardaki olası her arızayı değil, incelenen kapsamı açıklar.

## Taramayı çalıştırma

1. KillerBench'i açın. **Tanılama** bölümü, **Tam sistem taraması** seçili olarak açılır.
2. İlk çalıştırmanızda **Onaylanan sorunları otomatik onar** seçeneğini kapalı bırakın.
3. **Tanılama çalıştır** düğmesine basın. Her denetim çalışırken ilerleme sağdaki **Ham çıktı** görünümünde gösterilir.
4. Tarama bittiğinde çıktı bölmesi **Okunabilir rapor** görünümüne geçer.

Yükseltilmiş yetki olmadan başlayın. BitLocker koruyucuları ve korumalı hizmetler gibi bazı Windows kanıtları yönetici hakları gerektirir. Bu denetimler standart bir hesapta kanıtın kullanılamadığını bildirir. Bu kanıta ihtiyacınız varsa geçerli çalıştırmayı tamamlayın, raporu saklayın ve ardından KillerBench'i bilinçli olarak **Yönetici olarak çalıştır** ile yeniden açın. Yükseltilmiş yetki bir onarımı seçmez veya onaylamaz.

## Raporu okuma

Rapor **İncelenecek**, **Doğrulanan onarımlar**, **Tamamlanmayan** ve **Denetlenen** sayılarıyla başlar, ardından önerilen bir sonraki adımı gösterir.

- **Dikkat gerektirenler**, kanıtların desteklediği sorunları en önemliden başlayarak listeler.
- **Toplanan bilgiler**, eksik veya kullanılamayan kanıtlar dahil nelerin okunduğunu gösterir.
- Geçmiş olaylar geçmiş olarak etiketlenir. Geçen aydan kalma bir hata güncel bir arıza olarak değerlendirilmez.

Eksik kanıt hiçbir zaman sağlıklı sayılmaz. Bir denetim çalışamadıysa rapor bunu belirtir.

## Denetimleri kendiniz seçme

Tam tarama yerine tek tek denetimler seçmek için **Denetimleri seç (gelişmiş)** öğesini açın. Uygulamadaki her bölümde ayrıca bu kılavuzun **Bölümler** sayfalarında anlatılan odaklı araçlar bulunur.

## Bulduklarınızı düzeltme

Rapor bir düzeltme önerdiğinde açılacak bölümü ve aracı belirtir. İlk onarımınızdan önce [Değişiklikler nasıl güvenli kalır](safety.html) sayfasını okuyun. Yerel doğrulama hâlâ sürmektedir; başarılı bir tanılama, sunulan her onarımın sizin Windows derlemenizde test edildiğini kanıtlamaz. Her değişiklikten önce yedeklerinizi saklayın ve tam hedefi ve sonuçlarını gözden geçirin.
