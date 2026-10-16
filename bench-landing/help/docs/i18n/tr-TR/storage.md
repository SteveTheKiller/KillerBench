# Depolama

Diskleri, birimleri, Depolama Alanlarını ve sürücü bakım hizmetlerini inceleyin. Tanılama bir disk işlemi seçmez; desteklenen her makine değiştiren işlem kendi tam hedefini, ön koşullarını ve onayını gerektirir.

Adım adım kılavuzlar: [Sürücüyü denetleme ve onarma](check-a-drive.html) ve [Disk alanı boşaltma](free-disk-space.html).

## Bu bölümdeki araçlar

### Depolama

Sınırlı disk, bölüm ve sürücü harfli birim kimliklerini eşler, her sürücü harfli birimde %10 boş alan baskısını değerlendirir, VSS gölge depolama ayırmasını ve birim başına kota varsayılanlarını inceler, geçerli Storage modülü iş ilerlemesini özetler, seçili NTFS birimini ve çevrimiçi kendi kendini onarma durumunu denetler ve bu PC'deki disk ve denetleyici aygıtlarını inceler. Kapasite eşikleri, kota ayarları ve iş durumları inceleme kanıtıdır; temizleme, yeniden boyutlandırma, kota değişikliği veya iş denetimi seçmez. Sürücü harfi eşlemesi belirli bir ana aittir ve her bağlama yolunu kapsamaz. Aygıt bulguları seçili birimle eşlenmez. Varsayılan Windows birimidir. Hiçbir dosya, anlık görüntü, iş veya sürücü değiştirilmez, hiçbir ayar değiştirilmez, hiçbir birim çıkarılmaz ve hiçbir yeniden başlatma zamanlanmaz.

### Depolama Alanları

İlk düzey olmayan Depolama Alanları havuzlarını ve sanal diskleri sağlık, işletim durumu, kapasite, ayırma ve dayanıklılık dahil envantere alır, ardından tek bir sınırlı işletim sonucu döndürür. Windows, bu salt okunur sorguyu yanıtlarken Depolama Alanları yönetim sağlayıcısını isteğe bağlı olarak başlatabilir. Havuzlar, sanal diskler, fiziksel diskler, birimler, dayanıklılık, ayırma, bakım ayarları, ilke ve başlangıç türü korunur. Hiçbir havuz, disk, birim, ayırma, ekleme, onarım, iyileştirme veya ayar işlemi istenmez. Sağlayıcı sonuçları havuz erişilebilirliğini, yedekliliği, veri bütünlüğünü, yedeklerin kurtarılabilirliğini veya uygulama G/Ç'sini doğrulamaz.

### Depolama Hizmeti

Sınırlı geçerli Storage modülü işlerini, tam hizmet hatası geçmişini ve geçerli Depolama Hizmeti hazırlığını gözden geçirir. Eksik kanıt önceliklidir. İş durumu ve geçmiş hatalar hizmet kurtarmaya veya depolama değişikliklerine yetki veremez. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Mevcut depolama bildirimleri ve ilkeyle yönetilen depolama etkinliği sürebilir. Birimler, bölümler, dosya sistemleri, bağlama noktaları, aygıtlar, sürücüler, Depolama Algısı ayarları, bakım ilkesi, hizmet izinleri ve başlangıç türü korunur. KillerBench hiçbir iş, birim, bölüm, bağlama noktası, dosya, aygıt, sürücü veya depolama ayarı işlemi seçmez. Doğrulama yalnızca kararlı hizmet durumunu kapsar; iş sonucunu veya aygıt, birim ya da depolama ilkesi işlemini kapsamaz.

### Sürücüleri İyileştir hizmeti

Sürücüleri İyileştir hizmetinin kullanılabilirliğini denetler. İsteğe bağlı kurtarma, güncel bağımlılık denetimlerinden sonra yalnızca etkin ve durdurulmuş tam hizmeti başlatır. Windows tarafından zamanlanan mevcut iyileştirme etkinliği geçerli bakım ilkesi altında sürebilir. Birimler, dosya sistemleri, veriler, iyileştirme zamanlamaları, bakım ilkesi, hizmet izinleri ve başlangıç türü korunur. KillerBench bir birim seçmez ve hiçbir sürücüyü analiz etmez, trim yapmaz, birleştirmez, iyileştirmez veya zamanlamaz. Doğrulama yalnızca kararlı hizmet durumunu kapsar; analizi, iyileştirmeyi veya aygıt sağlığını kapsamaz.
