# Raporlar ve kanıtlar

Her çalıştırma okuyabileceğiniz, filtreleyebileceğiniz ve dışa aktarabileceğiniz bir rapor üretir. Rapor her zaman kanıtlananı yalnızca gözlemlenenden ayırır.

## Göreceğiniz durumlar

| Durum | Anlamı |
| --- | --- |
| İncelenecek | Kanıt, dikkat gerektiren bir sorunu destekliyor. |
| Bilgi | Kanıt toplandı ve bağlam olarak gösteriliyor. |
| Tamamlanmayan veya Bilinmiyor | Bir denetim tamamlanamadı veya kanıt kullanılamadı. Hiçbir zaman sağlıklı sayılmaz. |
| Geçmiş | Geçmişteki bir olay. Geçmişi açıklar ancak güncel bir arızayı kanıtlamaz. |
| Doğrulandı | Bir onarım veya değişiklik, sonuç geri okunarak onaylandı. |

## Ham çıktı ve okunabilir rapor

**Ham çıktı**, teknik kanıtları özgün satır numaralarıyla saklar; böylece her bulguyu kaynağına kadar izleyebilirsiniz. **Okunabilir rapor** aynı çalıştırmayı sade bir dille özetler ve bir sonraki güvenli adımı en başa koyar.

## Kaydedilmiş günlükler

KillerBench, makinede hiçbir şey çalıştırmadan CBS ve Setup günlükleri gibi kaydedilmiş Windows günlüklerini de okuyabilir. Bulgular, onları üreten satırın tam kendisine bağlanır. Oturum kaynaklarındaki **Örnek yükle**, güvenle inceleyebileceğiniz yapay bir örnek açar.

## Dışa aktarma

**Raporu dışa aktar**, raporu kaynak yolları, ayrıştırılmış girdiler ve kanıt alıntılarıyla birlikte kaydeder; böylece bir talebe eklemeden önce gözden geçirebilirsiniz. Ham günlükler ve kanıt alıntıları bilgisayar adları, hesaplar, yollar veya hassas kaynak metin içerebilir. Korumalı toplayıcılar sakladıklarını sınırlar, ancak içe aktarılan kanıtların karartılması garanti edilmez. Paylaşmadan önce hassas bilgileri inceleyip kaldırın.
