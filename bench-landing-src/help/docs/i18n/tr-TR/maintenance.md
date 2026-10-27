# Bakım

Bakım bir makineyi düzenli tutar: dosya temizleme, eski kullanıcı profilleri, zamanlanmış görevler ve Windows'u sadeleştirme. Her araç bir şey silinmeden önce neleri kaldıracağını önizler.

Adım adım kılavuzlar: [Disk alanı boşaltma](free-disk-space.html) ve [Windows'u daha gizli hâle getirme](make-windows-private.html).

## Bu bölümdeki araçlar

Dosyaları, profilleri ve zamanlanmış görevleri temizleyin ve Windows'u sadeleştirin.

### Temizleme

Ana paneldeki yaş ve dosya sınırı ayarlarını kullanarak seçili geçici dosya, önbellek ve saklanan tanılama kanıtı hedeflerini tahmin eder. Windows Temp ve Windows SystemTemp bağımsız hedeflerdir. Teslim İyileştirme ayrı bir önbellek seçimidir ve kaldırılan içeriğin yeniden indirilmesi gerekebilir. İsteğe bağlı kalıcı kaldırma ayrı temizleme onay kutusunu kullanır, saklanan adayları seçili sınır içinde yeniden denetler ve her sonucu kaydeder. Silme işlemi Geri Dönüşüm Kutusu'nu atlar. Hedefler, yaş ve dosya sınırları cleanup CLI komutuyla da yapılandırılabilir.

### Profiller

Seçili yaş eşiğini, tam ProfileList kayıtlarını ve isteğe bağlı hizmet ve zamanlanmış görev hesabı bağımlılıklarını kullanarak Windows profillerini gözden geçirir, ardından tek bir sınırlı inceleme sonucu döndürür. Yüklü profiller, sistem profilleri ve geçerli hesabın profili inceleme kapsamı dışındadır. Adaylar yalnızca incelemeye yöneliktir. Hiçbir profil, klasör, kayıt defteri anahtarı, hesap, hizmet, görev, kimlik bilgisi, ilke veya dosya değiştirilmez ve hiçbir kaldırma seçilmez. Uygulama bağımlılıkları ve yedeklerin kurtarılabilirliği doğrulanmamış olarak kalır.

### Zamanlanmış görevler

Görünür zamanlanmış görevleri, durumlarını ve bildirilen güvenlik sorumlularını envantere alır. Hiçbir görev başlatılmaz, kaydedilmez veya değiştirilmez. Eylemler, tetikleyiciler ve yürütme başarısı doğrulanmaz.

### Windows'u sadeleştir

Recall ve yapay zekâ, telemetri, arama önerileri, tüketici içeriği, görev çubuğu ve Başlat, bildirimler, Edge, güç, hazırda bekletme, Office dilleri ve önceden yüklenmiş yazılımlar gibi açıkça seçilen Windows temizleme alanlarını çalışan hesap veya tüm profiller için gözden geçirir. İnceleme salt okunurdur. Yüksek performanslı güç, uyku ve ekran zamanlayıcıları ile hazırda bekletme, o alan tek seçim olduğunda, güncel kanıt ve açık onayla ayrı ayrı değiştirilebilir.
