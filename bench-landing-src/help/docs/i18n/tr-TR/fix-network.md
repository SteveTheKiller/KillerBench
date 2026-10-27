# Ağ ve internet sorunlarını giderme

Bir bilgisayarın interneti olmadığında, bir sunucuya veya ada ulaşamadığında ya da bağlantısı sürekli koptuğunda bunu kullanın. KillerBench bağdaştırıcıyı, adresi, ağ geçidini, DNS'i ve güvenlik duvarını sırayla denetler ve bir kirayı yalnızca güncel kanıt gerekli olduğunu kanıtladığında yeniler veya bir hizmeti başlatır.

## Bağlantıyı denetleme

1. **Ağ** bölümünü açın ve **Ağ** öğesini seçin.
2. Belirli bir şeyi test etmek için **Ana bilgisayar adı araması (isteğe bağlı)** alanını veya **TCP hedefi (isteğe bağlı)** ile **TCP bağlantı noktası** alanlarını doldurun. Yalnızca yerel yapılandırmayı denetlemek için boş bırakın. Hiçbir uygulama verisi gönderilmez.
3. Ağ yavaşsa **Arama veya bağlantı başına zaman aşımı (saniye)** değerini ayarlayın.
4. **Onaylanan sorunları otomatik onar** seçeneğini kapalı bırakın ve **Tanılama çalıştır** düğmesine basın.
5. Raporu okuyun. Eksik bir adresi, eksik veya ulaşılamayan bir ağ geçidini, DNS yapılandırma sorunlarını, ara sunucu ayarlarını, güvenlik duvarı profili uyuşmazlıklarını ve başarısız bir aramayı ya da bağlantıyı birbirinden ayırır ve her biri için bir sonraki adımı verir.

Ağ geçidinden gelen bir yanıt internet erişimini kanıtlamaz ve yapılandırılmış çözümleyiciniz üzerinden başarısız olan bir arama sitenin çalışmadığı anlamına gelmez.

## Raporun bulduklarını onarma

1. **Ağ** aracını **Onaylanan sorunları otomatik onar** açıkken yeniden çalıştırın. Etkin DNS İstemcisi, DHCP İstemcisi, SMB istemcisi, Ağ Konumu Tanıma, Ağ Listesi ve IP Yardımcısı hizmetlerini başlatabilir ve kullanılabilir bir IPv4 adresi olmayan bağlı fiziksel DHCP bağdaştırıcılarını yenileyebilir.
2. Yenileme sırasında bağlantının kısa süreliğine kesilmesini bekleyin. Statik adresler ve sanal bağdaştırıcılar hiçbir zaman değiştirilmez.
3. Önbellekteki tek bir hatalı ad için her şeyi temizlemek yerine o ana bilgisayara yönelik DNS önbelleği temizlemeyi kullanın.

Yollar, Hosts girdileri, DNS sunucuları, güvenlik duvarı kuralları, Winsock sağlayıcıları ve ağ profilleri her zaman korunur.

## Kablosuz ağ, kablolu oturum açma ve ara sunucular

- **Kablosuz ağ**, bağdaştırıcıyı ve WLAN Otomatik Yapılandırmasını denetler ve durdurulmuşsa hizmeti başlatabilir.
- **Kablolu kimlik doğrulama (802.1X)**, bağlantı noktasında oturum açma gerektiren ağlarda Kablolu Otomatik Yapılandırma için aynısını yapar.
- **WinHTTP Otomatik Ara Sunucu**, makine ve geçerli hesap için depolanan ara sunucu ayarlarını gösterir. KillerBench tanılama sırasında hiçbir zaman bir PAC URL'si getirmez veya ara sunucu ayarlarını değiştirmez.

## Komut satırından

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Bkz. [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) ve [dns-cache](cli.html#cmd-dns-cache).

## İlgili

- [Ağ bölümü](network.html)
- [Yazdırma sorunlarını giderme](fix-printing.html)
- [Değişiklikler nasıl güvenli kalır](safety.html)
