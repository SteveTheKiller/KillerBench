# Windows Update'i düzeltme

Güncelleştirmeler başarısız olduğunda, takıldığında veya sürekli yeniden yüklendiğinde bunu kullanın. KillerBench önce güncelleştirme zincirini okur, hatayı arkasındaki kanıtlarla açıklar ve bir hizmeti yalnızca güncel kanıt sorunun tam olarak kendisini kanıtladığında başlatır ya da bileşen deposunu onarır.

## Başlamadan önce

- KillerBench'i yönetici olarak çalıştırın. Güncelleştirme hizmetleri, bileşen deposu ve korumalı sistem dosyaları eksiksiz kanıt için yükseltilmiş yetki gerektirir.
- Windows'ta zaten bekleyen bir yeniden başlatma varsa önce yeniden başlatın. Birçok güncelleştirme onarımı, yeniden başlatma durumu temiz olana kadar engellenir.
- Bakım çalışması sırasında bilgisayarı prize takılı tutun.

## Güncelleştirmelerin neden başarısız olduğunu öğrenin

1. **Güncelleştirmeler** bölümünü açın ve araç kenar çubuğunda **Windows Update** öğesini seçin.
2. İlk geçişte **Onaylanan sorunları otomatik onar** seçeneğini kapalı bırakın ve **Tanılama çalıştır** düğmesine basın.
3. **Okunabilir rapor** görünümünü okuyun. Rapor durdurulmuş güncelleştirme hizmetlerini, bekleyen yeniden başlatmaları, düşük boş alanı, bileşen deposu bozulmasını ve Windows'un kaydettiği tam hata kodlarını, önerilen bir sonraki adımla birlikte listeler.
4. Rapor bir hata kodu belirtirse, eşleşen takip denetimleri otomatik olarak çalışır. Bir kodu kendiniz de `killerbench-cli update-code 0x80070002` ile arayabilirsiniz.

## Takılmış gibi görünen bir güncelleştirmeyi izleme

**Güncelleştirmeler** bölümünde **Bakım etkinliği izleyicisi** öğesini seçin ve **Tanılama çalıştır** düğmesine basın. Güncelleştirme, Setup ve Installer etkinliğinin, aktarım toplamlarının ve yeniden başlatma göstergelerinin yinelenen anlık görüntülerini alır. Etkinlik yalnızca kanıt olarak gösterilir; tek başına hiçbir zaman bir takılma veya arıza iddia etmez.

## Raporun bulduklarını onarma

1. **Windows Update** aracını **Onaylanan sorunları otomatik onar** açıkken yeniden çalıştırın. Durduğu doğrulanan etkin güncelleştirme hizmetlerini başlatabilir ve DISM veya SFC'yi yalnızca canlı denetim bozulmayı doğruladığında çalıştırır.
2. Tek bir hizmet için bunun yerine **BITS hizmeti**, **Şifreleme Hizmetleri**, **Windows Modül Yükleyicisi** veya **Update Orchestrator Hizmeti** gibi o hizmete özel aracı seçin. Her biri yalnızca o hizmet için kurtarma sunar.
3. Rapor yeniden başlatmanızı söylediğinde, başka bir şey çalıştırmadan önce yeniden başlatın. KillerBench yeniden başlatma gerektiren sonuçları kaydeder ve sonraki işleri yeniden başlatma yapılana kadar erteler.

Her onarım günlüğe yazılır ve geri okunur. Tüm sıra için [Değişiklikler nasıl güvenli kalır](safety.html) sayfasına bakın.

## Belirli bir güncelleştirmeyi yükleme

1. **Güncelleştirmeler** bölümünde **Kullanılabilir güncelleştirmeler** öğesini seçin ve yapılandırılmış kaynaktan güncelleştirmeleri bulmak için **Tanılama çalıştır** düğmesine basın.
2. İstediğiniz güncelleştirmeleri seçin ve **Seçilenleri önizle** düğmesine basın. Önizleme, tam olarak neyin yükleneceğini ve varsa yeniden başlatma gereksinimini listeler.
3. Yalnızca önizlemeyi okuduktan sonra yükleyin. KillerBench sizin yerinize hiçbir güncelleştirmeyi seçmez veya onaylamaz.

## Komut satırından

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Bkz. [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) ve [update-plan](cli.html#cmd-update-plan). Bir yükleme kesintiye uğradıysa [update-recover](cli.html#cmd-update-recover) komutunu çalıştırın; sonucu denetler ve güncelleştirmeyi hiçbir zaman yeniden denemez.

## İlgili

- [Güncelleştirmeler bölümü](updates.html)
- [Sistem dosyalarını onarma](repair-system-files.html)
- [Sorun giderme](troubleshooting.html)
