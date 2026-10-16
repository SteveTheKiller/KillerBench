# KillerBench'in teknolojisi

Kaputun altına bir bakış: KillerBench bir Windows bilgisayarı nasıl okur, kanıtları nasıl dürüst tutar ve bir değişikliğe ne zaman izin verileceğine nasıl karar verir.

## Teknoloji yığını

KillerBench yerel bir Windows uygulamasıdır. Masaüstü uygulaması, komut satırı ve bunların altındaki Engine aynı tanılama kanıtlarını ve onarım iş akışlarını paylaşır.

| Bileşen | Ayrıntı |
| --- | --- |
| Arayüz | .NET 10 üzerinde WPF (`net10.0-windows`), Windows x64, özel pencere çerçevesi |
| Engine | WPF'ye veya masaüstü denetimlerine başvuru içermeyen `Engine.Core`, `Engine` ve `Engine.Windows` |
| Komut satırı | `killerbench-cli`, aynı Engine üzerinde ayrı bir ana bilgisayar |
| Windows sağlayıcıları | .NET, Windows API'leri, PowerShell ve Windows komut satırı araçları; her biri sabit süre, kayıt ve boyut sınırları içinde çalışır |
| Raporlar | Ham çıktı, Okunabilir rapor, HTML, metin ve JSON |
| Başvuru verileri | Hata kodları, donanım tanımlayıcıları, satıcılar ve destek başvuruları için sürümlü bilgi paketleri |
| Paketleme | Yükleyici: çerçeveye bağımlı, .NET Desktop Runtime 10 for Windows x64 gerektirir. Taşınabilir: kendi içinde, çalışma zamanı yüklemesi gerektirmez |
| Görünüm | 13 aile teması ve 19 dil |

## Yükleme, klasörler ve veriler

KillerBench iki biçimde sunulur. Yükleyici standart indirmedir ve kullanıcı başına veya tüm kullanıcılar için yüklemeyi destekler. Taşınabilir sürüm kendi .NET çalışma zamanını taşır ve yükleme yapmadan çalışır.

KillerBench, onu başlatan hesabın haklarıyla çalışır. Kendi yetkisini kendisi yükseltmez. BitLocker koruyucuları gibi korumalı kanıtlar veya bir onarım yönetici erişimi gerektirdiğinde onu **Yönetici olarak çalıştır** ile başlatın. Yetki yükseltme hiçbir zaman bir onarımı seçmez veya onaylamaz.

### Yükleyicinin yaptıkları

Yükleyici uygulamayı doğrular ve `%LOCALAPPDATA%\Programs\KillerBench\` konumuna ya da tüm kullanıcılar için bir yükseltme isteminden sonra `%ProgramFiles%\KillerBench\` konumuna çıkarır. Bir Başlat menüsü kısayolu, isteğe bağlı bir masaüstü kısayolu ve normal ve sessiz kaldırma seçenekleriyle bir Program Ekle veya Kaldır girdisi ekler. Taşınabilir sürüm, geçerli çalıştırma için içeriğini `%LOCALAPPDATA%\KillerBench\Portable\` altında doğrular ve çıkarır, sonraki başlangıçta terk edilmiş kopyaları temizler.

### Nerede ne bulunur

| Konum | İçerik |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Tema, vurgu rengi, arayüz ölçeği ve dil |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Her iş akışı çalıştırması için bir JSON günlüğü: kararlar, sınırlı komut çıktısı ve onarım sonuçları |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | DISM ve SFC gibi bakım onarımları için yürütme günlükleri |
| `%LOCALAPPDATA%\KillerBench\` altındaki özellik klasörleri | Tek tek korumalı işlemler için bekleyen kayıtlar ve sonuçlar; örneğin `BitLockerBackup`, `Time`, `Firewall` ve `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | Hakkında bölümünde seçilen KillerMCP onarım düzeyi |
| `HKCU` veya `HKLM\Software\KillerBench` | Eşleşen yükleme kapsamı için yüklü yol |
| Seçtiğiniz bir `.kbench` dosyası | Saklanan kanıtlarıyla kaydedilmiş bir yakalama |

Hiçbir şey karşıya yüklenmez. Bir günlüğü yalnızca belirsiz bir sonuca ait kanıta artık ihtiyacınız kalmadığında silin.

## Önce tanılama

KillerBench tanılamayı ve değişikliği ayrı işler olarak ele alır. Tanılama yalnızca okur. Bir değişiklik yalnızca belirli bir onarım veya işlem seçtiğinizde, seçeneklerini ve ön koşullarını gözden geçirdiğinizde ve onayladığınızda gerçekleşir.

Tam sistem taraması, odaklı bir iş akışında otomatik onarım açık olsa bile yalnızca tanılama amaçlıdır. Kaydedilmiş yakalamalar, içe aktarılan günlükler ve geçmiş olaylar hiçbir zaman bir onarıma yetki vermez.

### Sınırlı toplama

Her yerel komut ve Windows sağlayıcısı sabit süre, kayıt ve boyut sınırları içinde çalışır. Bir sağlayıcı çıkış durumunu, zaman aşımını, kesilmeyi, standart çıktıyı ve hata çıktısını kaydeder. Başarısız olan veya kesilen bir sağlayıcı hiçbir zaman temiz, boş bir yanıt olarak okunamaz.

### En az saklama

Toplayıcılar bir bulgunun ihtiyaç duyduğunu saklar. Birçok sonuç adları, kategorileri, sayıları, durumları ve zaman damgalarını saklarken kimlik bilgilerini, komut satırlarını, tam yolları, hesap kimliklerini, ağ uç noktalarını ve ilgisiz olay yükünü dışarıda bırakır.

## İzleyebileceğiniz kanıtlar

Her bulgu onu üreten kanıta bağlıdır ve rapor her zaman kanıtlananı yalnızca gözlemlenenden ayırır.

| Durum | Anlamı |
| --- | --- |
| İncelenecek | Kanıt, dikkat gerektiren bir sorunu destekliyor. |
| Bilgi | Kanıt toplandı ve bağlam olarak gösteriliyor. |
| Tamamlanmayan veya Bilinmiyor | Bir denetim tamamlanamadı veya kanıt kullanılamadı. Hiçbir zaman sağlıklı sayılmaz. |
| Geçmiş | Geçmişteki bir olay. Geçmişi açıklar ancak güncel bir arızayı kanıtlamaz. |
| Doğrulandı | Bir onarım veya değişiklik, sonuç geri okunarak onaylandı. |

### Bilinmiyor da bir sonuçtur

Bilinmiyor; erişimin reddedildiği, bir kaynağın eksik olduğu, bir sağlayıcının başarısız olduğu, çıktının bozuk olduğu, bir sınırın aşıldığı veya kanıtların çeliştiği anlamına gelir. KillerBench bu sınırlamayı gizlemek yerine onu ve bir sonraki yararlı adımı gösterir.

### Geçmiş geçmişte kalır

Olay kayıtları ve içe aktarılan günlükler geçmişte bir zamanda kaydedileni anlatır. Bir hata koduna veya bir sonraki tanılama kapsamına işaret edebilirler. Arızanın hâlâ etkin olduğunu kanıtlamazlar.

## Korumalı onarımlar

Onarımlar; uygunluk, yürütme, doğrulama ve kurtarma kurallarına sahip türlenmiş işlemlerdir. Bir günlük satırından seçilmiş komut dizeleri değildir. Desteklenen her onarım aynı sırayı izler:

1. Eksiksiz güncel kanıt toplanır.
2. Desteklenen tek bir tarif eşleştirilir.
3. Gerekli seçenekler ve sonuçlar gözden geçirilir.
4. Yönetimsel ve ilke ön koşulları onaylanır.
5. Belirleyici kanıt, değişiklikten hemen önce yeniden toplanır.
6. Kalıcı bir hazırlık kaydı yazılır.
7. Tek bir sınırlı yerel işlem çalıştırılır.
8. Güncel doğrulama kanıtı toplanır.
9. Doğrulandı, engellendi, durduruldu veya belirsiz sonucu kaydedilir.

Belirleyici kanıt işlem çalışmadan önce değişirse KillerBench işlemi gerçekleştirmeden durur.

### Açık seçimler

Bir seçeneğe bağlı onarımlar sizden birim, yazıcı kuyruğu, kurtarma anahtarı hedefi, güç değeri, paket sürümü veya onarım kaynağı gibi tam bir seçim ister. KillerBench bu değerleri hiçbir zaman kendisi uydurmaz.

### Yönetilen bilgisayarlar

Grup İlkesi ve aygıt yönetimi yerel bir değişikliği etkisiz veya uygunsuz hâle getirebilir. Desteklenen onarımlar bir ayarın sahibinin kim olduğunu inceler ve bu sahiplik yönetiliyorsa veya bilinmiyorsa durur. Bir ilkenin sahip olduğu ayarlara dokunulmaz.

### Otomatik onarım

Otomatik onarım yalnızca uygun tariflerin ilerlemesine izin verir. Kanıtı atlamaz, eksik seçenekleri seçmez, ilkeyi yok saymaz, belirsiz bir işlemi yeniden denemez, Windows'u yeniden başlatmaz veya incelenmesi gereken bir bulguyu onarıma dönüştürmez.

## Günlükler ve kesintiler

KillerBench, Windows'tan bir değişiklik yapmasını istemeden önce ne yapmak üzere olduğunu yazar. Her değişiklik `%LOCALAPPDATA%\KillerBench` altındaki bir günlüğe kaydedilir.

Değişiklikten sonra KillerBench sonucu geri okur ve yalnızca Windows beklenen durumu gösterdiğinde **Doğrulandı** bildirir. Windows başarı bildirir ancak durum değişmezse, sonuç bunu belirtir ve hiçbir şey yinelenmez.

Bir işlem, Windows isteği aldıktan sonra ancak doğrulama bitmeden durabilir. Bekleyen kayıt kalır ve daha sonraki bir mutabakat, ne olduğunu anlamak için güncel durumu okur. Mutabakat otomatik bir geri alma değildir ve özgün değişikliği hiçbir zaman yinelemez. Sonuç belirsizse günlüğü saklayın.

## Sabitler ve sınırlar

| Özellik | Değer |
| --- | --- |
| Tanılama kapsamı | Tam sistem taraması 49 tanılama alanını çalıştırır ve 96 korumalı onarım tarifini denetler |
| Metin kaynağı okuma | Kaynak başına 1.000.000 karakter, eşleşen 2.000 satır gösterilir |
| Oturum kaynakları | Kenar çubuğunda 12; bir yakalama en fazla 24 kaynak ve 64 MB tutar |
| Olay sorguları | Son 7 günden kanal başına en yeni 200 olay |
| Windows Update taraması | Kaynak başına 15 saniye, toplamda iki dakika |
| Günlük incelemesi | 10.000 ayrıştırılmış girdi, 256 farklı kod, kod başına 320 karakterlik 8 alıntı |
| Sıkıştırılmış CBS arşivi | 256 MiB arşiv, 512 MiB günlük üyesi, 30 saniyelik çıkarma |
| Salt okunur CHKDSK | On dakika; bir onarım 65 dakikayla sınırlıdır |
| DNS ve TCP denetimleri | Varsayılan olarak on saniye, en fazla bir dakika |
| Hizmet envanteri | En fazla 1.024 yüklü hizmet |
| Bilgi paketleri | Aynı anda en fazla 32 açık |
| Büyük dosya incelemesi | En az 100 MB'lık dosyalar |
| Bakım etkinliği izleyicisi | 1 ile 60 saniye arası aralık, 1 ile 120 arası anlık görüntü |
| Zamanlanmış görev taslakları | 1 ile 4.320 dakika arası yürütme sınırı |
| Özel saat kaynakları | En fazla sekiz NTP eşi |
| Etkinlik dışa aktarma | Saklanan en fazla 150 girdi |
| Diller ve temalar | 19 dil, vurgu çeşitleriyle 13 tema |

## Yapmayacakları

- TPM'yi temizlemez, kendi oluşturmadığı koruyucuları silmez ve kurtarma parolalarını göstermez.
- Grup ilkesinin veya aygıt yönetiminin denetlediği bir ayarı değiştirmez.
- Yalnızca geçmiş bir olay veya içe aktarılan bir rapor bir sorundan bahsediyor diye onarım çalıştırmaz.
- Eksik, kısmi veya başarısız kanıtı sağlıklı bir karara dönüştürmez.

## Raporlar

Her çalıştırma okuyabileceğiniz, filtreleyebileceğiniz ve dışa aktarabileceğiniz bir rapor üretir. Rapor şunları birbirinden ayırır:

1. Neyin incelendiği
2. Hangi sorunun bulunduğu
3. Bulguyu hangi kanıtın desteklediği
4. Hangi işlemin önerildiği veya gerçekleştirildiği
5. Sonucun nasıl doğrulandığı
6. Neyin bilinmediği veya yeniden başlatma gerektirdiği

**Ham çıktı**, teknik kanıtları özgün satır numaralarıyla saklar; böylece her bulguyu kaynağına kadar izleyebilirsiniz. **Okunabilir rapor** aynı çalıştırmayı sade bir dille özetler ve bir sonraki güvenli adımı en başa koyar. HTML raporları Killer Tools tema sistemini kullanır ve uygulama olmadan da okunabilir kalır. Metin taşınabilirliği öne çıkarır, JSON ise başka bir program için yapılandırılmış alanları saklar.

## Kaydedilmiş günlükler ve yakalamalar

KillerBench, makinede hiçbir şey çalıştırmadan kanıtları inceleyebilir. Bir metin günlüğünü pencereye bırakın veya yolunu yapıştırıp **Günlük aç** seçeneğini seçin. CBS, CSI ve Setup satırları zaman damgalarıyla ayrıştırılır ve bulgular özgün satırın tam kendisine bağlanır. EVTX ve olay XML dosyaları da aynı şekilde içe aktarılabilir.

**Windows Update taraması**; CBS, DISM, Panther ve Setup geri alma günlüklerinin, güncelleştirme ve bakım olay kanallarının, güncelleştirme hizmeti durumlarının, boş alanın, çalışma süresinin, bekleyen yeniden başlatma göstergelerinin ve seçili güncelleştirme ilkesinin isteğe bağlı, salt okunur bir toplamasıdır. **Yakalama kaydet**, saklanan kanıtları bir `.kbench` dosyasına yazar. **Yakalama aç**, özgün dosyalar olmadan onu yeniden açar ve yüklü kurallarla tekrar değerlendirir; böylece aynı kurallar aynı bulguları verir.

## Tanıtım modu

Uydurma bir iş istasyonunda **Tanılama çalıştır** özelliğini denemek için `KillerBench.exe --demo` ile başlatın. Her iş akışı yapay kanıt ve zamanlanmış ilerleme üretir. Hiçbir Windows sağlayıcısı çağrılmaz, hiçbir onarım komutu çalışmaz ve hiçbir günlük yazılmaz. **Tanılama çalıştır** dışındaki ayrı araç düğmeleri canlı kalır; bu nedenle onları tanıtım işlemi olarak kullanmayın.

`KillerBench.exe --demo-screenshot`, sabit ve kurgusal bir makine için yalnızca görüntülenebilen bir Tam sistem taraması raporu açar. Çalışma alanındaki tıklamalar, kısayollar ve dosya bırakma engellenir.

## Bilgi paketleri

Büyük ve sık güncellenen başvuru verileri tanılama mantığı yerine bilgi paketlerinde bulunur: Windows ve Setup hata kodları, satıcı ve aygıt tanımlayıcıları, PCI, USB, ACPI ve UEFI başvuruları, ağ OUI satıcı verileri, sürücü ve destek konumları ve seçilmiş destek bağlantıları.

Her paket biçimini, şema sürümünü, kaynağını, kaynak revizyonunu, oluşturulma zamanını ve içerik karmasını belirtir; KillerBench bunları kullanmadan önce doğrular. Bilinmeyen veya geçersiz bir paket tahmine başvurmak yerine kullanılamaz durumda kalır. Bir güncelleştirme geçici bir konuma indirilir, doğrulanır, ardından etkin paketin yerini tek adımda alır; böylece başarısız bir güncelleştirme önceki paketi yerinde bırakır.

Bilgi verileri bir tanımlayıcıyı açıklayabilir veya bir sonraki bakılacak yeri önerebilir. Geçmiş bir kodu güncel bir arızaya dönüştüremez, otomatik olarak sürücü seçemez veya bir onarıma yetki veremez.

## Güncelleştirmeler ve imzalama

Başlangıçta KillerBench, dört saniyelik bir sınırla GitHub'dan en son kararlı sürümü ister ve yalnızca o sürüm daha yeniyse bir güncelleştirme sunar. Denetimi Hakkında bölümündeki **Başlangıçta güncellemeleri denetle** ile veya güncelleştirme isteminden kapatabilirsiniz. İndirme, çalışan kopyayla eşleşen pakettir (yükleyici veya taşınabilir) ve herhangi bir şey yüklenmeden önce SHA-256 değeri sürümün `SHA256SUMS.txt` dosyasıyla eşleşmelidir. Uygulama işlemi tamamlamak için kendini kapatıp yeniden açar. Arka plan hizmeti yoktur.

Hakkında kartı sürümü, kod imzalama yayıncısını ve parmak izini ve çalışan yürütülebilir dosyanın SHA-256 değerini gösterir; böylece elinizdeki kopyayı denetleyebilirsiniz.

## KillerMCP

KillerBench bir yapay zekâ aracısı içermez. [KillerMCP](../mcp.html), bir aracıyı aynı Engine'e bağlayabilir. Hakkında bölümündeki **KillerMCP Seçenekleri**, o aracının neler yapabileceğine karar verir:

| Ayar | Etki |
| --- | --- |
| Kapalı (varsayılan) | Aracı bir onarım isteyemez. |
| KillerMCP'nin güvenli onarımlar yapmasına izin ver | Aracı, güvenli olarak sınıflandırılan onarımları isteyebilir. |
| Kesintiye yol açan onarımlara da izin ver | Kesintiye yol açan onarımlar kullanılabilir hâle gelir ve her biri yine onay gerektirir. |

Düzeyi yalnızca bu bilgisayarın başındaki bir kişi yükseltebilir. Belirsiz bir onarım sonucundan sonra KillerMCP bir kilit yazar ve biri Hakkında bölümünden kaldırana kadar aracı onarımları engelli kalır.

## Yerelleştirme

Arayüz 19 dilde kullanılabilir: İngilizce, Bengalce, Çekçe, Almanca, İspanyolca, Fransızca, Macarca, İtalyanca, Japonca, Kazakça, Norveççe, Lehçe, Portekizce (Brezilya), Rusça, Türkçe, Ukraynaca, Vietnamca ve Basitleştirilmiş ve Geleneksel Çince. Dil değişikliği; bölüm ve iş akışı adları, karşılama metni ve açık bir okunabilir rapor dahil hemen uygulanır. Eksik metinler İngilizce gösterilir.

Kanıtlar, Engine bulguları, günlükler ve Ham çıktı, bir Windows günlüğü gibi İngilizce kalır; böylece bir rapor her makinede aynı okunur ve tam metin için aranabilir.

## Komut satırı ve çıkış kodları

`killerbench-cli`, masaüstü uygulaması olmadan aynı Engine'i çalıştırır. İlerleme standart hataya, JSON standart çıktıya gider. Çoğu komut varsayılan olarak önizleme yapar ve bir değişiklik `--execute` gerektirir. Eşleşen `-recover` komutları, kesintiye uğrayan bir işlemi yinelemeden gözden geçirir. Bkz. [KillerBench'i betiklerden çalıştırma](use-the-cli.html) ve [komut başvurusu](cli.html).

| Çıkış kodu | İş akışı komutları | Tarama ve açma |
| --- | --- | --- |
| 0 | Denetlenen kapsamlarda çözülmemiş sorun yok | Eksiksiz kapsam |
| 1 | Başlatma veya günlük hatası | Başarısızlık |
| 2 | Geçersiz bağımsız değişkenler | Geçersiz bağımsız değişkenler |
| 3 | Dikkat gerekiyor | Kısmi toplama |
| 4 | Doğrulamadan önce yeniden başlatma gerekiyor | |
| 130 | Durduruldu | İptal edildi veya genel zaman aşımı |

Hiçbir çıkış kodu bir bilgisayarın sağlıklı olduğunu onaylamaz.

## Mimari

Engine, arayüz içermeyen üç projeye bölünmüştür. `Engine.Core` temel öğeleri ve bildirimsel tarifleri sağlar, `Engine` paylaşılan iş akışlarına ve kanıtlara sahiptir, `Engine.Windows` ise yerel sağlayıcıları kaydeder. Masaüstü uygulaması, CLI ve testler ayrı ana bilgisayarlardır.

| Katman | Sorumluluk |
| --- | --- |
| Yetenek toplayıcıları | Tek bir tam kapsam için sınırlı Windows durumunu okur |
| Kanıt ayrıştırıcıları | Biçimi, kimliği, eksiksizliği ve sınırları doğrular |
| Tanılama yorumu | Kanıtı açık iş akışı durumlarına dönüştürür |
| İş akışı düzenleme | Denetimleri, ilerlemeyi, tarifleri ve günlükleri birleştirir |
| Onarım düzenleyicileri | Uygunluğu yeniden denetler, hazırlar, değiştirir, doğrular ve mutabakat sağlar |
| Raporlar | Sorunları, işlemleri, sonuçları ve sınırlamaları sunar |
| Bilgi paketleri | Mantığı değiştirmeden sürümlü başvuru verilerini çözümler |

Toplayıcılar; kimlik belirsiz olduğunda, çıktı bozuk olduğunda, bir sınır aşıldığında veya gerekli yetki incelenemediğinde belirsizlikte engeller. Engine bugün Windows'u hedefler ve platformlar arası değildir. [Engine geliştirici kılavuzu](../engine/index.html) API'yi, CLI'yi ve Engine'in başka bir .NET ana bilgisayarına yerleştirilmesini anlatır.

## Gizlilik ve ağ

Tanılama ve günlük analizi, yapay zekâ hizmeti veya KillerBench hesabı olmadan yerel olarak çalışır. Günlükler ve saklanan kanıtlar yerel hesabın `%LOCALAPPDATA%\KillerBench` klasörlerinde kalır.

Seçtiğiniz işlemler; Windows Update, yazılım indirme hizmetleri veya kurtarma anahtarının Active Directory'ye ya da Entra ID'ye yedeklenmesi gibi hedefleriyle iletişim kurabilir. Başlangıçtaki güncelleştirme denetimi GitHub ile, bilgi güncelleştirmeleri ve KillerMCP yükleyicisi ise yapılandırılmış hizmetleriyle iletişim kurar. [KillerMCP](../mcp.html) kullanan bir yapay zekâ istemcisi istediği başvuru sonucunu alabilir.

Ham günlükler, içe aktarılan dosyalar ve dışa aktarılan kanıtlar bilgisayar adları, hesaplar, yollar veya hassas kaynak metin içerebilir. Paylaşmadan önce gözden geçirin ve herkese açık bir rapora hiçbir zaman kurtarma anahtarları, kimlik bilgileri veya müşteri verileri eklemeyin.

## Geliştirme durumu

KillerBench geliştirme aşamasındadır ve henüz herkese açık bir indirme yoktur. Onarım kapsamı hâlâ genişlemektedir ve yerel onarım doğrulaması hâlâ sürmektedir. Başarılı bir derleme veya benzetimli bir denetim yerel onarım davranışını kanıtlamaz. Her yerel değişikliği Windows sürümleri ve hata durumları genelinde doğrulamak için tek kullanımlık Windows sanal makineleri kullanılır. Güvenlik modelinin tamamı için [Değişiklikler nasıl güvenli kalır](safety.html) sayfasına bakın.

## Sözlük

Bu sayfada kullanılan terimlerin alfabetik sırayla sade tanımları.

| Terim | Anlamı |
| --- | --- |
| Bekleyen kayıt | Bir değişikliğin Windows'a gönderilmiş olabileceğini işaretleyen kayıt. Mutabakat sonucu okuyana kadar yinelemeyi engeller. |
| Belirsizlikte engelleme | Kanıt belirsiz, eksik veya çok büyük olduğunda sonuç Bilinmiyor olur ve hiçbir değişikliğe izin verilmez. |
| Bilgi paketi | Hata kodları veya donanım tanımlayıcıları gibi başvuru verilerini içeren sürümlü ve doğrulanmış bir dosya. |
| BITS | Arka Plan Akıllı Aktarım Hizmeti; güncelleştirmeleri ve diğer dosyaları arka planda indiren Windows hizmeti. |
| CBS | Component-Based Servicing; güncelleştirmeleri ve özellikleri yükleyen Windows alt sistemi. Günlüğü `CBS.log` dosyasıdır. |
| DISM | Deployment Image Servicing and Management; bileşen deposunu denetleyen ve onaran Windows aracı. |
| Doğrulandı | Sonuç Windows'tan geri okundu ve değişikliğin yapması gerekenle eşleşti. |
| Günlük | Bir değişiklikten önce yazılan ve sonrasında güncelleştirilen yerel bir JSON kaydı; böylece kesintiye uğrayan bir işlem daha sonra gözden geçirilebilir. |
| İlke sahibi | Bir ayarı denetleyen Grup İlkesi veya aygıt yönetimi. KillerBench sahibi olan ayarlara dokunmaz. |
| İş akışı | Bir aracın tek bir çalıştırması: topladığı denetimler, kullanabileceği tarifler ve ürettiği rapor. |
| Kanıt | KillerBench'in Windows'tan gerçekten okuduğu ve bir bulgunun geriye doğru izlenebilmesi için kaynağıyla birlikte saklanan bilgi. |
| Mutabakat | Bir kesintiden sonra ne olduğunu öğrenmek için güncel durumun, değişiklik yinelenmeden okunması. |
| Panther | Windows Kurulumunun yükseltme günlüklerini yazdığı klasör. |
| SFC | System File Checker; korumalı sistem dosyalarını doğrulayan ve geri yükleyen Windows aracı. |
| Tarif | Kendi uygunluk, yürütme, doğrulama ve kurtarma kurallarına sahip türlenmiş bir onarım. |
| TPM | Trusted Platform Module; BitLocker'ın sürücü anahtarını korumak için kullandığı güvenlik yongası. |
| Yakalama | Saklanan kanıtları tutan ve özgün bilgisayar olmadan yeniden gözden geçirilebilen, kaydedilmiş bir `.kbench` dosyası. |
| Yetki yükseltme | Bir Windows isteminden sonra yönetici haklarıyla çalışma. KillerBench kendi yetkisini hiçbir zaman kendisi yükseltmez. |
