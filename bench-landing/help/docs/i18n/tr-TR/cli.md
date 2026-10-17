# Komut satırı

`killerbench-cli`, PowerShell'den veya uzak bir konsoldan masaüstü uygulamasıyla aynı Engine'i çalıştırır. Makineyi değiştirebilecek her komut varsayılan olarak önizleme yapar ve işlem yapmadan önce `--execute` gibi açık bir bayrak gerektirir. Komutlar JSON kanıtını standart çıktıya, ilerlemeyi standart hataya yazar.

Adım adım kılavuzlar: [KillerBench'i betiklerden çalıştırma](use-the-cli.html).

Çıkış kodu 0, önizlemenin veya doğrulanan sonucun başarılı olduğu; 1, bir başlatma, giriş veya günlük hatası; 2, bağımsız değişkenlerin tanınmadığı; 3, engellendi, başarısız oldu veya doğrulanmadı; 4, bir iş akışının doğrulama yapabilmesi için yeniden başlatma gerektiği; 130 ise sizin durdurduğunuz anlamına gelir.

Bu listeyi kendi makinenizde yazdırmak için `killerbench-cli` komutunu bağımsız değişken olmadan çalıştırın.

## Güncelleştirmeler ve bakım

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Tam aktarım incelemesi veya iki anlık görüntülü gözlem; yeniden deneme yok.
- `bits-jobs [--all-users]`: BITS işlerinin durumları, sahipleri ve bayt sayıları; aktarım değişikliği yok.
- `bits-retry JOB_GUID [--execute]`: Varsayılan olarak önizleme; tam bir iş için açık yeniden deneme, uygulamanın tamamlandığına dair iddia yok.
- `bits-retry-recover JOB_GUID`: İki salt okunur anlık görüntü; kararlı sonuçları başka bir yeniden deneme yapmadan arşivler.
- `component-repair-source`: Yönetilen DISM onarım kaynağı ilkesi ve çakışmalar; değişiklik yok.
- `delivery-optimization`: Salt okunur Teslim İyileştirme hizmeti durumu; JSON kanıtı.
- `delivery-optimization-activity`: Gizlilik açısından sınırlı geçerli iş sayıları ve bayt toplamları; aktarım kimlikleri ve değişiklik yok.
- `delivery-optimization-policy`: Salt okunur klasik ve Policy CSP ayarları; hassas değerler ve sağlayıcı kimlikleri hariç.
- `feature-service-events`: Saat, Search, WMI, Olay Günlüğü ve ses hizmetlerinin tam hata geçmişini okur.
- `feature-update-policy`: Salt okunur hedef sürüm, erteleme, duraklatma ve koruma ilkesi; JSON kanıtı.
- `pending-file-operations`: Bekleyen dosya işlemlerinin gizlilik açısından sınırlı salt okunur özeti; JSON kanıtı.
- `restart-check`: Salt okunur bekleyen yeniden başlatma kaynakları; JSON kanıtı.
- `servicing-logs`: En yeni CBS, düz metin ve sıkıştırılmış CBS arşivleri, DISM ve Windows Setup günlüklerinin sınırlı analizi; onarım yok.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Salt okunur Bakım etkinliği anlık görüntüleri.
- `servicing-service-events`: Salt okunur tam Windows Update bakım zinciri hata geçmişleri.
- `servicing-service-states`: Altı hizmetli Windows Update bakım zincirinin salt okunur geçerli durumu.
- `setup-code DECIMAL_OR_0xHEX`: Belgelenmiş Windows Setup sonuç kodu başvurusu; makine incelemesi veya onarım yok.
- `update-applicability`: Salt okunur Windows sürümü, derlemesi ve mimarisi kanıtı.
- `update-cache`: Sınırlı salt okunur SoftwareDistribution önbellek toplamları; dosya adları yok.
- `update-code DECIMAL_OR_0xHEX`: Belgelenmiş Windows Update sonuç kodu başvurusu; makine incelemesi veya onarım yok.
- `update-context`: Salt okunur geçerli yükseltme ve masaüstü oturumu kanıtı; hesap tanımlayıcıları yok.
- `update-engine-files`: Salt okunur yerel Windows Update altyapısı varlık, sürüm ve imza kanıtı.
- `update-events`: Son yedi günün salt okunur Windows Update İstemcisi geçmişi; ham kanıt dahil JSON.
- `update-followup DECIMAL_OR_0xHEX`: Belgelenmiş bir Windows Update sonuç koduna göre seçilen güncel salt okunur tanılama.
- `update-history`: Salt okunur en yeni 100 Windows Update işlem sonucu; JSON kanıtı.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: `--select` seçeneğini yineleyin; varsayılan olarak önizleme.
- `update-policy`: Salt okunur yönetilen Windows Update ilkesi; JSON kanıtı.
- `update-policy-surfaces`: Salt okunur klasik ve Policy CSP yapılandırma yüzeyi kanıtı; değerler ve sağlayıcı kimlikleri hariç.
- `update-recover`: Bekleyen bir güncelleştirmeyi denetler; hiçbir zaman yüklemez veya yeniden denemez.
- `update-restart-policy`: Salt okunur uyumluluk son tarihleri, ek süreler, yeniden başlatma bastırma ve bildirim davranışı.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: JSON bulma ve eşleşmeler; yükleme yok.
- `update-service-events`: Salt okunur tam Windows Update hizmeti hata geçmişi.
- `update-service-files`: Salt okunur tam Windows Update hizmet DLL'si kayıt ve imza kanıtı.
- `update-services`: Salt okunur Windows Update hizmet kayıtları; JSON kanıtı.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Güncel aramaya bağlı; varsayılan olarak önizleme; eski depoyu saklar.
- `update-store-reset-recover`: Hazırlanmış bir depo sıfırlamasını yinelemeden mutabakata bağlar.
- `windows-capabilities`: Salt okunur çevrimiçi Windows yetenek durumları; JSON sonucu.
- `windows-update-task`: Salt okunur tam Scheduled Start görev durumu ve çalışma geçmişi; görev yürütme veya güncelleştirme işlemi yok.

## BitLocker ve TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Varsayılan olarak önizleme; şifreli bir veri birimi için otomatik kilit açmayı etkinleştirir.
- `bitlocker-auto-unlock-recover`: Kesintiye uğrayan bir otomatik kilit açma isteğini yinelemeden doğrular.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Varsayılan olarak önizleme; yalnızca yeni kurtarma yedek dosyası.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Kesintiye uğrayan bir yedeklemeyi yeniden yazmadan doğrular.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Varsayılan olarak önizleme; örneğin gerekli bir yöntemle yeniden şifrelemeden önce şifre çözmeyi başlatır.
- `bitlocker-decrypt-recover`: Kesintiye uğrayan bir şifre çözme isteğini yinelemeden doğrular.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Varsayılan olarak önizleme; mevcut TPM ve kurtarma koruyucuları gerektirir.
- `bitlocker-encrypt-recover`: Bekleyen dönüştürme durumunu isteği yinelemeden doğrular.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Varsayılan olarak önizleme; mevcut bir kurtarma parolasını Active Directory'ye veya Microsoft Entra ID'ye yedekler.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Yalnızca seçenek önizlemesi.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Varsayılan olarak önizleme; bir TPM veya kurtarma parolası koruyucusu ekler; parola hiçbir zaman yazdırılmaz.
- `bitlocker-protector-recover`: Kesintiye uğrayan bir koruyucu isteğini yinelemeden doğrular.
- `bitlocker-protectors DRIVE:`: Yalnızca koruyucu kimlikleri ve türleri; kurtarma parolaları yok.
- `bitlocker-resume DRIVE: [--execute]`: Varsayılan olarak önizleme; yalnızca tam, tamamen şifrelenmiş ve askıya alınmış birim.
- `bitlocker-resume-recover`: Bekleyen koruma durumunu isteği yinelemeden doğrular.

## Güvenlik ve gizlilik

- `certificate-chains`: Kişisel sertifikalar için salt okunur, yalnızca yerel önbellekten zincir güveni; iptal denetimi veya indirme yok.
- `defender-overview [--report]`: Salt okunur koruma, tarama zamanlaması ve toplu tehdit özeti; işlem yok.
- `defender-protection [--report]`: Salt okunur koruma tercihleri ve tanınan ilke yüzeyleri; hassas ayrıntılar hariç; değişiklik yok.
- `defender-scan Quick|Full [--execute] [--report]`: Varsayılan olarak önizleme; tek bir açık, korumalı Microsoft Defender taraması.
- `defender-scan-history [--report]`: Salt okunur hızlı ve tam tarama zamanlaması ve gecikme durumu; hassas ayrıntılar hariç; tarama yok.
- `defender-scan-recover [--report]`: Başka bir tarama başlatmadan salt okunur mutabakat.
- `defender-threats [--report]`: Toplu tehdit geçmişi, geçerli etkin bayraklar ve önem kimlikleri; hassas ayrıntılar hariç; işlem yok.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Varsayılan olarak önizleme; yalnızca yönetilmeyen yerel profiller.
- `firewall-enable-recover`: Bekleyen bir değişikliği yinelemeden inceler.
- `firewall-events`: Sınırlı Windows Güvenlik Duvarı uyarı, hata ve kritik işletim geçmişini okur.
- `firewall-logging`: Etki Alanı, Özel ve Ortak trafik günlüğü yapılandırmasını günlükleri açmadan okur.
- `firewall-rules`: Gizlilik açısından sınırlı geçerli kural ve ilke kaynağı sayılarını okur.
- `firewall-service-events`: Tam BFE ve Windows Defender Güvenlik Duvarı Hizmet Denetimi Yöneticisi hata geçmişini okur.
- `privacy-clipboard-history on|off [--execute]`: Varsayılan olarak önizleme; ilke korunarak tam geçerli kullanıcı değeri.
- `privacy-clipboard-history-recover`: Kesintiye uğrayan bir değişikliği yinelemeden yeniden denetler.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Önceki değerin korumalı geri yüklenmesi.
- `privacy-plan --select Camera --select ClipboardSync`: Yalnızca seçimleri gözden geçirir; hiçbir ayar değiştirilmez.
- `privacy-setting KEY on|off [--execute]`: Varsayılan olarak önizleme; ilkeyi koruyan geçerli kullanıcı değişikliği; geçersiz anahtarlar desteklenen seçenekleri listeler.
- `privacy-setting-recover KEY`: Kesintiye uğrayan bir değişikliği yinelemeden yeniden denetler.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Tam değerin korumalı geri yüklenmesi.
- `security-check`: Salt okunur TPM, Güvenli Önyükleme, sanallaştırma korumaları ve UAC ilkesi; JSON kanıtı.

## Hizmetler

- `service-binaries`: Salt okunur yerel hizmet ikili dosyası varlığı ve Authenticode durumu; yollar hariç ve yerel olmayan yollarla iletişim kurulmaz.
- `service-check NAME [NAME ...] | service-check --all`: Salt okunur desteklenen hizmet durumları ve bağımlılık kanıtı.
- `service-dependencies`: Salt okunur doğrudan bağımlılık bütünlüğü incelemesi; hizmet değişikliği yok.
- `service-dlls`: Salt okunur kayıtlı hizmet DLL'si varlığı ve Authenticode durumu; yollar hariç ve yerel olmayan yollarla iletişim kurulmaz.
- `service-events`: Salt okunur hizmet hata geçmişi; ham kanıt dahil JSON.
- `time-events`: JSON kanıtıyla salt okunur son Windows Saat olay geçmişi.
- `service-logons`: Salt okunur hizmet hesap sınıfı ve masaüstü etkileşimi incelemesi; hesap kimlikleri hariç.
- `service-path-quoting`: Salt okunur hizmet yürütülebilir dosya yolu tırnak incelemesi; tam yollar ve bağımsız değişkenler hariç.
- `service-recovery-policy`: Salt okunur yüklü hizmet kurtarma eylemi özeti; komutlar ve yeniden başlatma iletileri hariç.
- `service-startup-config`: Salt okunur başlangıç modu tutarlılığı incelemesi; hizmet değişikliği yok.
- `service-transitions`: Bekleyen hizmet durumlarının salt okunur yinelenen gözlemi; hizmet işlemi yok.
- `service-triggers`: Salt okunur yüklü hizmet tetikleyici özeti; tetikleyici yükleri hariç; değişiklik yok.

## Depolama, dosyalar ve yedekleme

- `backup-events`: VSS ve volsnap geçmişi; JSON kanıtı; değişiklik yok.
- `backup-service-events`: Tam VSS, yazılım gölge kopya sağlayıcısı, Windows Yedekleme ve blok düzeyinde yedekleme altyapısı hata geçmişini okur.
- `backup-summary`: Yerel Windows Yedekleme işlem özeti; yedekleme değişikliği yok.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Varsayılan olarak önizleme; Geri Dönüşüm Kutusu ya hep ya hiçtir ve yaş uygulanmaz.
- `disk-cleanup-task`: Salt okunur tam görev durumu ve çalışma geçmişi; görev yürütme veya temizleme yok.
- `disk-diagnostic`: Tam disk tanılama görevlerini ve olay geçmişini ilişkilendirir; görev veya disk işlemi yok.
- `disk-diagnostic-tasks`: Salt okunur tam toplayıcı ve çözümleyici görev geçmişi; görev veya disk işlemi yok.
- `disk-quotas`: Salt okunur birim başına kota varsayılanları ve durumu; kullanıcı kimlikleri hariç; değişiklik yok.
- `file-history-events`: Gizlilik açısından yansıtılmış Dosya Geçmişi uyarı ve hata sayıları; değişiklik yok.
- `file-history-status`: Geçerli hesabın Dosya Geçmişi koruma durumu; değişiklik yok.
- `search-index-rebuild [--execute]`: Varsayılan olarak önizleme; önceki dizini saklar ve güncel durumu doğrular.
- `search-index-rebuild-recover`: Onarım komutlarını yinelemeden salt okunur mutabakat.
- `storage-check DRIVE_LETTER`: Salt okunur sabit NTFS denetimi; JSON sonuçları ve canlı ilerleme.
- `storage-events`: Salt okunur depolama geçmişi; ham kanıt dahil JSON.
- `storage-jobs`: Salt okunur toplu Storage modülü iş durumu ve ilerlemesi; JSON; değişiklik yok.
- `storage-reliability`: Salt okunur sıcaklık, aşınma ve G/Ç sayaçları; JSON; onarım yok.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Varsayılan olarak önizleme; yürütme yedek onayı gerektirir; sistem birimleri yeniden başlatmaya zamanlanır.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Yalnızca önizleme; onarım veya zamanlama yok.
- `storage-repair-recover`: Tamamlanan veya zamanlanan bir dosya sistemi onarımını yinelemeden doğrular.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Geçerli kanıtı toplar ve bağlar; onarım veya zamanlama yok.
- `storage-sense-policy`: Salt okunur tanınan makine temizleme ilkesi; kullanıcı ayarları ve dosya kimlikleri hariç.
- `storage-sense-task`: Salt okunur tam görev durumu ve çalışma geçmişi; görev yürütme veya temizleme yok.
- `storage-service-events`: Tam Depolama Hizmeti, Sürücüleri İyileştir, VSS ve yazılım gölge kopya sağlayıcısı hata geçmişini okur.
- `volume-capacity DRIVE:`: JSON kanıtıyla salt okunur kullanılabilir alan.
- `volume-dirty DRIVE:`: Salt okunur kirli bit durumu; onarım veya yeniden başlatma yok.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Varsayılan olarak salt okunur analiz; tam ve sağlıklı bir birim ve uyumlu ortam gerekir.
- `volume-optimize-recover`: Bekleyen tamamlanmış bir işlemi yinelemeden doğrular.
- `vss-providers`: Salt okunur VSS sağlayıcı adları, sürümleri ve türleri; tanımlayıcılar hariç; kayıt değişikliği yok.
- `vss-writers`: Bildirilen yazıcı durumları; İngilizce çıktı yorumu; sıfırlama yok.

## Yazdırma

- `print-events`: Son yedi günün salt okunur PrintService Admin geçmişi; JSON kanıtı.
- `print-extension-files`: Salt okunur yazdırma işlemcisi ve izleyici ikili dosyası varlık ve imza özeti; tam yollar hariç.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Varsayılan olarak önizleme; yalnızca tam Hata veya Engellendi durumundaki iş.
- `print-job-cancel-recover [--folder PATH]`: Kesintiye uğrayan bir iptali yinelemeden yeniden denetler.
- `print-job-list`: Geçerli görünür işleri değiştirmeden listeler.
- `print-operational-events`: Salt okunur PrintService Operational kanal durumu ve kullanılabilir geçmiş; JSON kanıtı.
- `print-policy`: Salt okunur Point and Print kısıtlamaları ve sürücü yükleme ilkesi; JSON kanıtı.
- `print-rendering-policy`: Salt okunur işleme ve sürücü yalıtımı ilkesi; JSON kanıtı.
- `print-spooler-events`: Yerel Yazdırma Biriktiricisi kimliğine bağlı salt okunur Hizmet Denetimi Yöneticisi hataları; JSON kanıtı.
- `printer-check --queue NAME [--queue NAME ...]`: Salt okunur seçili yerel kuyruk ayarları.
- `printer-conditions`: Salt okunur yerel kuyruk durum kodları; fiziksel doğrulama veya değişiklik yok.
- `printer-driver-files`: Salt okunur kayıtlı dosya varlığı ve yürütülebilir imza özeti; tam yollar hariç.
- `printer-driver-models`: Salt okunur sürüm 3, sürüm 4 ve paket farkındalığı özeti; JSON kanıtı.
- `printer-drivers`: Salt okunur kuyruk ve sürücü kayıt envanteri ve eşleştirmesi; JSON kanıtı.
- `printer-endpoint-check`: Tam atanmış Standart TCP/IP yazıcı uç noktalarına sınırlı bağlantılar; bulma, SNMP veya yazdırma verisi yok.
- `printer-list`: Yerel kuyrukları ayarları değiştirmeden bulur.
- `printer-ports`: Salt okunur kuyruk/bağlantı noktası kayıt eşleştirmesi; JSON kanıtı.
- `printer-queue-attributes`: Salt okunur yerel kuyruk biriktirme, saklama ve paylaşım öznitelikleri.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Varsayılan olarak önizleme; çevrimdışı modu temizlemek bekleyen işleri serbest bırakabilir.

## Ağ

- `dhcp-check`: Salt okunur eksik veya süresi dolmuş DHCP kirası uygunluğu; JSON kanıtı, yenileme yok.
- `dhcp-events`: Salt okunur IPv4 ve IPv6 DHCP İstemcisi olay geçmişi; JSON sonucu.
- `dhcp-leases`: Salt okunur kira zaman damgası, süre sonu ve sunucu kanıtı; JSON sonucu.
- `dhcp-renew [--execute]`: Varsayılan olarak önizleme; yalnızca tam, eksik veya süresi dolmuş bağlı fiziksel DHCP kiraları.
- `dhcp-renew-recover`: Başka bir kira istemeden salt okunur mutabakat.
- `dns-cache HOST [--execute]`: Varsayılan olarak önizleme; tam ana bilgisayar doğrulamalı korumalı yerel önbellek temizleme.
- `dns-cache-recover`: Kesintiye uğrayan bir önbellek işlemini temizlemeyi yinelemeden yeniden denetler.
- `dns-cache-summary`: Gizlilik açısından sınırlı yerel DNS önbelleği durumu ve kayıt türü sayıları; değişiklik yok.
- `dns-check HOST [--timeout-seconds 1..60]`: Sınırlı sistem ad çözümlemesi; JSON sonucu.
- `dns-client-config`: Salt okunur sonek, kayıt, devretme ve ilke kanıtı; JSON sonucu.
- `dns-client-events`: Salt okunur, gizlilik açısından sınırlı DNS İstemcisi uyarı ve hata geçmişi; JSON sonucu.
- `dns-nrpt-policy`: Salt okunur geçerli DNS ad alanı ilkesi kanıtı; JSON sonucu.
- `ip-addresses`: Salt okunur geçerli IP adresi durumu, kaynağı ve çakışma incelemesi; JSON kanıtı.
- `ip-interface-config`: Salt okunur IP arabirimi yapılandırma kanıtı; JSON sonucu.
- `network-firewall`: Etkin ağ atamalarını eşleşen güvenlik duvarı profili etkinleştirmesiyle ilişkilendirir.
- `network-offload`: Salt okunur genel Windows ağ yük boşaltma yapılandırması; JSON sonucu.
- `network-profiles`: Salt okunur geçerli profil kategorileri ve bağlantı sınıflandırmaları; ağ adları hariç.
- `network-service-events`: Tam DHCP, DNS, İş İstasyonu, NLA, Ağ Listesi ve IP Yardımcısı hata geçmişini okur.
- `proxy-check`: Salt okunur depolanan WinHTTP ve geçerli hesap İnternet ara sunucusu kanıtı.
- `route-priority`: Salt okunur geçerli varsayılan yol ölçümü karşılaştırması; JSON kanıtı.
- `smb-client-config`: Salt okunur SMB imzalama, konuk erişimi ve aktarım yapılandırması.
- `smb-connections`: Salt okunur etkin protokol anlaşması; uzak kimlikler hariç.
- `smb-server-config`: Salt okunur SMB protokolü, imzalama, şifreleme ve çok kanallı yapılandırma.
- `smb-shares`: Salt okunur sınırlı SMB paylaşım envanteri; yollar, izinler, oturumlar ve hesaplar hariç.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Sınırlı bağlantı denetimi; uygulama verisi gönderilmez.
- `tcp-port-pressure`: Salt okunur dinamik TCP aralıkları ve toplu yerel bağlantı noktası kullanımı; uç noktalar ve işlemler hariç.
- `tcp-settings`: Salt okunur Windows TCP şablonu yapılandırma kanıtı; JSON sonucu.
- `vpn-profiles`: Salt okunur geçerli hesap VPN profili yapılandırması; kimlik bilgileri hariç.
- `winrm-listeners`: Salt okunur WinRM dinleyici kayıt özeti; sertifika parmak izleri hariç.
- `winsock-catalog`: Salt okunur yerel Winsock protokol kataloğu yapısı; JSON sonucu.
- `winsock-namespaces`: Salt okunur yerel Winsock ad alanı sağlayıcısı yapısı; JSON sonucu.
- `winsock-provider-files`: Salt okunur protokol sağlayıcısı dosya varlığı ve imzaları; JSON sonucu.
- `winsock-readiness`: Salt okunur yerel Winsock protokolü, ad alanı sağlayıcısı ve sağlayıcı dosyası hazırlığı; JSON sonuçları.

## Uygulamalar

- `app-folder-review FOLDER_NAME [--report]`: Sabit kökte tam klasör incelemesi; silme yok.
- `app-package-review FULL_NAME [--all-users]`: Tam hedef ve bağımlı paketler; kaldırma yok.
- `app-packages [--all-users]`: Yalnızca kayıtlı Appx/MSIX envanteri; kaldırma yok.
- `browser-extensions`: Chrome, Edge, Brave ve Firefox uzantı kimliği incelemesi; kaldırma yok.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Yalnızca inceleme; sistem değişikliği yok.
- `depot-review [application and configuration selections] [--json]`: Yalnızca inceleme; hazırlama yok.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Salt okunur Authenticode ve satıcı imzacısı denetimi; hiçbir şey çalıştırılmaz.
- `foxit-inventory`: Salt okunur makine ürün envanteri; JSON çıktısı.
- `installed-apps [--current-user]`: Kaldırma kayıtları ve yetenek bayrakları; kaldırma yok.
- `installed-apps-compare BEFORE.json AFTER.json`: Kaydedilmiş envanterle kayıt farkları; makine değişikliği yok.
- `installed-hotfixes`: Salt okunur sınırlı yüklü Windows düzeltme envanteri; JSON kanıtı.
- `installer-activity`: Salt okunur Installer işlemi ve işlem işaretçisi kanıtı; JSON kanıtı.
- `installer-check`: Salt okunur Installer ilkesi ve altyapı hazırlığı; JSON kanıtı.
- `installer-engine-files`: Salt okunur gerekli Installer dosyası varlığı ve Authenticode özeti; JSON kanıtı.
- `installer-events`: Son yedi günün salt okunur MSI geçmişi; JSON kanıtı.
- `installer-logging`: Salt okunur Installer günlük kaydı ve hata ayıklama ilkesi; JSON kanıtı.
- `installer-pending-state`: Salt okunur bekleyen dosya işlemi ve Installer etkinliği ilişkilendirmesi; JSON kanıtı.
- `installer-registration-repair [--execute]`: Varsayılan olarak önizleme; yalnızca tam hizmet kaydı hatası; altyapının kaydını hiçbir zaman silmez.
- `installer-registration-repair-recover`: Kesintiye uğrayan bir kaydı komutları yinelemeden yeniden denetler.
- `installer-security-policy`: Salt okunur Installer güvenlik ve yama ilkesi; JSON kanıtı.
- `installer-service-events`: Yerel Windows Installer kimliğine bağlı salt okunur Hizmet Denetimi Yöneticisi hataları; JSON kanıtı.
- `installer-service-registration`: Salt okunur Windows Installer hizmet kaydı özeti; JSON kanıtı.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Güncel tam uygunluk önizlemesi; kaldırma sağlayıcısı yok.
- `mace-package-recover`: Kesintiye uğrayan tek bir uygulama paketi kaldırma kaydını yeniden mutabakata bağlar; kaldırmayı hiçbir zaman yinelemez.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Yalnızca inceleme; kaldırma yok.
- `msi-cache`: Salt okunur kayıtlı Windows Installer önbellek görünürlüğü; JSON kanıtı.
- `msi-code NONNEGATIVE_DECIMAL`: Belgelenmiş Windows Installer dönüş kodu başvurusu; makine incelemesi veya onarım yok.
- `msi-product {PRODUCT-CODE}`: Salt okunur tam MSI kayıt durumu; JSON kanıtı.
- `msi-repair {PRODUCT-CODE} [--execute]`: Varsayılan olarak önizleme; yalnızca tam bozuk veya hatalı yapılandırılmış ürün.
- `msi-repair-recover`: Kesintiye uğrayan tam bir ürün onarımını yinelemeden yeniden denetler.
- `msi-sources`: Salt okunur, gizlilik açısından sınırlı kayıtlı Windows Installer kaynak meta verileri; JSON kanıtı.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Varsayılan olarak önizleme; çalışan hesabın tam önbelleklerini kurtarma adları altında saklar.
- `orca-cache-reset-recover`: Kesintiye uğrayan önbellek saklamayı taşımayı yinelemeden doğrular.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Yalnızca inceleme; Outlook değişikliği yok.
- `startup-apps`: Başlangıç kayıtları ile önyükleme veya oturum açma görevi incelemesi; devre dışı bırakma yok.
- `third-party-update ID SOURCE VERSION [--accept-agreements] [--execute]`: Varsayılan olarak önizleme; yalnızca tek bir tam sürüm.
- `third-party-update-recover`: Bekleyen tam sürümü güncelleştirmeyi yeniden denemeden doğrular.
- `third-party-updates`: Salt okunur Microsoft.WinGet.Client uygulama güncelleştirmesi bulma; uygulama değişikliği yok.

## Sistem

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Tam yerel Bluetooth SIG atama araması; ağ isteği yok.
- `crash-config`: Depolanan Windows döküm modu ve yolları; değişiklik yok.
- `crash-dumps`: Varsayılan Windows döküm dosyası meta verileri; içerik analizi veya değişiklik yok.
- `crash-events`: Son sistem hata raporları; tanılama veya değişiklik yok.
- `crash-review [--knowledge PACK_PATH ...]`: En fazla 32 paket; birleşik çökme, kapanma, sınırlı mini döküm yapısı ve isteğe bağlı tam hata denetimi başvurusu incelemesi; değişiklik yok.
- `default-printer`: Salt okunur varsayılan seçim ve otomatik yönetim ayarı; JSON kanıtı.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Varsayılan olarak önizleme; yalnızca eksik veya çakışan varsayılan.
- `default-printer-set-recover [--folder PATH]`: Kesintiye uğrayan bir seçimi yinelemeden yeniden denetler.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: En fazla 32 paketten isteğe bağlı tam sürücü başvurularıyla salt okunur aygıt kanıtı.
- `device-code DECIMAL_OR_0xHEX`: Çevrimdışı Aygıt Yöneticisi sorun kodu başvurusu.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Yalnızca başvuru adayları.
- `event-channel-enable CHANNEL [--execute]`: Varsayılan olarak önizleme; yönetilmeyen tek bir tam tanılama kanalını etkinleştirir ve diğer ayarları korur.
- `event-channel-enable-recover`: Etkinleştirme komutunu yinelemeden salt okunur mutabakat.
- `event-channels`: Dört tanılama kanalı için salt okunur durum, saklama, boyut ve doluluk.
- `font-registrations`: Salt okunur sınırlı makine ve geçerli kullanıcı kaydı ve dosya varlığı denetimi.
- `memory-diagnostic-results`: Salt okunur sınırlı Windows Bellek Tanılama geçmişi; JSON kanıtı.
- `minidump-analyze PATH`: Sınırlı yapısal mini döküm meta verileri; bellek sayfaları, semboller veya değişiklik yok.
- `name-rename PROPOSED_NAME [--execute]`: Varsayılan olarak önizleme; yalnızca bağımsız çalışma grubu PC'leri; Windows'u hiçbir zaman yeniden başlatmaz.
- `name-rename-recover`: Bekleyen bir yeniden adlandırmayı yinelemeden doğrular.
- `name-review PROPOSED_NAME`: Salt okunur kimlik ve önerilen ad incelemesi.
- `profiles --recover`: Kaydedilen yokluğu doğrular ve kurtarma kanıtını saklar; silmeyi hiçbir zaman yeniden denemez.
- `profiles --recover-backup`: Bekleyen bir yedekleme yapıtını doğrular ve arşivler; kopyalamayı hiçbir zaman yeniden denemez.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Önce önizleme; korumalı yerel kopyalama, açık yürütme ve var olmayan bir yerel NTFS hedefi gerektirir.
- `profiles --select SID --create-backup-artifact DESTINATION`: Mevcut kopyalanmış bir yedeği karşılaştırır ve sabit yapıtını oluşturur; bir profili kopyalamaz veya kaldırmaz.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Salt okunur inceleme; doğrulama seçili tek bir SID gerektirir.
- `task-check`: Salt okunur görev envanteri, sınıflandırılmış bulgular ve JSON kanıtı.
- `task-definition TASK_FOLDER TASK_NAME`: Salt okunur tam görev XML'i; kullanılamaz olması yok olduğu anlamına gelmez.
- `task-inventory`: Salt okunur görev kimlikleri ve durumları; JSON çıktısı.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Geçmiş kurtarma incelemesi; yeniden denemeye hiçbir zaman yetki vermez.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Bir görev taslağını doğrular ve inceler; kayıt yok.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Varsayılan olarak önizleme; devre dışı, yalnızca oluşturulabilen Local System tanımı.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Salt okunur tam tanım mutabakatı; bir görevi hiçbir zaman yeniden denemez veya çalıştırmaz.
- `task-xml PLAN.json YYYY-MM-DD`: stdout'a devre dışı inceleme XML'i; yalnızca yerel bölge takvim zamanlamaları; kayıt yok.
- `time-config`: Salt okunur Windows Saat modu, eşler ve ilke kanıtı.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Varsayılan olarak önizleme; özel mod eş gerektirir.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Yalnızca önizleme.
- `time-recover --folder PATH`: Bekleyen bir kaydı doğrular ve arşivler; değişiklikleri hiçbir zaman yeniden denemez.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Salt okunur; özel mod eş gerektirir.
- `wmi-provider`: Salt okunur yerel işletim sistemi sağlayıcısı sorgusu; JSON kanıtı.

## Bilgi, doğrulama ve Engine

- `knowledge PACK_PATH CONTEXT CODE`: Yerel başvuru araması; onarım yürütme yok.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Doğrulanmış yerel etkinleştirme; indirme yok.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Etkin yerel paketi doğrular; değişiklik yok.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Yalnızca başvuru değişiklikleri; paket yükleme yok.
- `knowledge-info PACK_PATH`: Paket meta verileri ve bağlam başına girdi sayıları; değişiklik yok.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Gözlemlenen kodlara bağlamsal başvurular ekler.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: En yeni doğrulanmış saklanan paketi seçer; indirme yok.
- `knowledge-search PACK_PATH CONTEXT TEXT`: En fazla 50 başvuru metni eşleşmesi; onarım yürütme yok.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Tek bir tam kimliği en fazla 32 pakette çözümler; çakışmalar hiçbir zaman seçilmez.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Yapılandırılmış her yüklü veritabanını doğrular; indirme veya etkinleştirme yok.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Yapılandırılmış HTTPS uç noktalarından imzalı veritabanı adaylarını getirir ve atomik olarak etkinleştirir.
- `open <file.kbench> [--json]`: Kaydedilmiş bir yakalamayı yeniden açar ve yüklü kurallarla tekrar değerlendirir.
- `scan --output <new-file.kbench> [--json]`: Yeni bir dosyaya kaydedilen salt okunur Windows Update yakalaması.
- `validation-coverage RECORD.json [...]`: Geçerli sürümün yerel VM kanıtını özetler; tanılama veya onarım yok.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Salt okunur tek kullanımlık VM senaryo planı; tanılama veya onarım yok.
- `validation-record RECORD.json`: Sınırlı tek bir yerel VM kanıt kaydını doğrular; tanılama veya onarım yok.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Yerel geçerli sürüm VM kapsam raporu.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Doğrulanmış salt okunur paylaşılan seçenek planını çalıştırır.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Yalnızca envanter.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Odaklı tanılama günlüğü; onarım yok.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Yalnızca günlüğe yazılan inceleme.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Günlüğe dahil edilen isteğe bağlı tam ürün durumu.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Günlüğe dahil edilen isteğe bağlı seçili hedef tanılaması.
- `workflow storage [--volume D:] [--json]`: Günlüğe yazılan salt okunur dosya sistemi denetimi.
- `workflow-journal FILE.json`: Geçmiş içerik; 0 çıkış kodu sağlıklı değil, başarıyla okundu anlamına gelir.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Ana bilgisayardan bağımsız denetimleri gösterir veya doğrular; tarama veya onarım yok.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Yalnızca geçmiş rapor; hiçbir komut çalıştırılmaz.

## Diğer komutlar

- `<analyze|investigate> <text-log-path> [--json]`: Yerel metin kanıtını analiz eder.
- `adapter-bindings`: Salt okunur temel protokol bağlama özeti; JSON kanıtı.
- `adapter-statistics`: Salt okunur bağdaştırıcı bayt, hata ve atma sayaçları; JSON kanıtı.
- `application-events`: Salt okunur son Uygulama hataları, gruplanmış sayılar ve JSON kanıtı.
- `applocker-policy`: Salt okunur geçerli toplama modları ve toplu kural sayıları; kural ayrıntıları hariç; değişiklik yok.
- `common-check [--scope NAME ...]`: Varsayılan olarak tüm tanılama alanları; belirtildiğinde yalnızca seçili alanlar; salt okunur JSON.
- `delete-notifications`: Salt okunur NTFS ve ReFS silme bildirimi yapılandırması; JSON; iyileştirme veya ayar değişikliği yok.
- `diagnostic-scopes [--all]`: Önerilen 11 sonuç grubunu veya salt okunur 49 alanın tamamını listeler; toplama yok.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Odaklı tanılama günlüğü; onarım yok.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Tüm tanılama alanları; `--auto-repair` açıkça belirtilmedikçe onarım yok.
- `gateway-check`: Tam yapılandırılmış varsayılan sonraki atlamaların sınırlı ICMP denetimleri; adres taraması yok.
- `infrastructure-service-events`: Bağlantı, uzaktan yönetim, bildirim ve temel hizmet hatalarının sınırlı tek bir tam anlık görüntüsünü okur.
- `investigate-batch FILE [FILE ...]`: En fazla 12 dosya; JSON Lines; başarısız veya kısmi içe aktarmalar için çıkış kodu 3.
- `licensing-check`: Ürün anahtarları olmadan salt okunur Windows etkinleştirme durumu; JSON kanıtı.
- `mac-vendor PACK_PATH MAC_ADDRESS`: En uzun önek ile yerel paket araması; ağ isteği yok.
- `ncsi-policy`: Salt okunur Windows bağlantı sınıflandırma ilkesi; yoklama hedefleri hariç.
- `neighbor-cache`: Salt okunur, gizlilik açısından sınırlı komşu ulaşılabilirlik kanıtı; JSON sonucu.
- `ntfs-self-healing DRIVE:`: Salt okunur seçili birim çevrimiçi NTFS onarım yapılandırması; onarım veya ayar değişikliği yok.
- `pagefile-check`: Salt okunur disk belleği dosyası yönetimi, ayırma ve kullanım özeti; tam yollar hariç.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Tam yerel paket araması; ağ isteği veya sürücü seçimi yok.
- `physical-disks`: Salt okunur geçerli sağlık, işletim, ortam, veri yolu ve kullanım durumu; JSON; onarım yok.
- `platform-service-events`: Uygulama, kimlik, profil ve aygıt destek hizmeti hatalarının sınırlı tek bir tam anlık görüntüsünü okur.
- `previous-windows [--execute --backup-confirmed]`: Varsayılan olarak önizleme; DISM aracılığıyla Windows geri alma özelliğini kalıcı olarak kaldırır.
- `previous-windows-recover`: Geri alma kaldırmayı yinelemeden salt okunur mutabakat.
- `provisioned-packages`: Yerel Windows'ta yeni kullanıcılar için hazırlanmış paketler; kaldırma yok.
- `recipes [WORKFLOW]`: Yüklü tanımları veya iş akışına özgü bağımlılıkları ve sınırları listeler; yürütme yok.
- `repair <dism|sfc> [--execute]`: Varsayılan olarak önizleme.
- `reserved-storage`: Salt okunur çevrimiçi Windows ayrılmış depolama durumu; JSON kanıtı.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Varsayılan olarak önizleme; güncel denetimlerden sonra tek bir nokta oluşturur.
- `restore-point-recover`: Başka bir nokta oluşturmadan bekleyen bir isteği doğrular.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Yalnızca önizleme.
- `shadow-copies`: Salt okunur geçerli VSS anlık görüntü durumları ve öznitelikleri; kimlikler hariç; anlık görüntü değişikliği yok.
- `shadow-storage`: Salt okunur VSS fark alanı ayırması ve baskısı; JSON; yeniden boyutlandırma veya anlık görüntü işlemi yok.
- `uac-policy`: Salt okunur UAC etkinleştirmesi ve yükseltme istemi ilkesi; hesap veya kimlik bilgisi verisi yok.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Tam yerel UEFI kayıt defteri araması; ağ isteği veya sürücü seçimi yok.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Tam yerel paket araması; ağ isteği veya sürücü seçimi yok.
- `vitals-check`: Salt okunur Windows, donanım, hesap, kaynak, pil ve fiziksel disk anlık görüntüleri; JSON kanıtı.
- `win32-code DECIMAL_OR_0xHEX`: Çevrimdışı yüklü Windows iletisi araması.
- `workflows`: Yüklü iş akışı anahtarlarını ve başlıklarını listeler; tarama veya onarım yok.
