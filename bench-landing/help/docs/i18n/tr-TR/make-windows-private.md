# Windows'u daha gizli hâle getirme

Bir dizüstü bilgisayardaki gizliliği gözden geçirmek ve sıkılaştırmak için bunu kullanın. KillerBench geçerli ayarları okur, hangilerinin sahibinin zaten ilke veya aygıt yönetimi olduğunu gösterir ve yalnızca seçtiğiniz ayarları, tek tek ve tam bir geri alma olanağıyla değiştirir.

## Geçerli ayarları gözden geçirme

1. **Gizlilik** bölümünü açın.
2. Gözden geçirmek istediğiniz alanları işaretleyin. Başlangıçta hiçbir alan işaretli değildir.
3. **Tanılama çalıştır** düğmesine basın. Rapor bu makine ve KillerBench'i çalıştıran hesap için geçerli değeri gösterir ve ilkenin sahip olduğu ayarları işaretler.

## Bir ayarı değiştirme

1. **Gizlilik ayarı** içinden kamera, mikrofon, konum, tanılama verileri, reklam, pano geçmişi, bulut içerik araması veya arama vurguları gibi bir ayar seçin.
2. Yeni değeri seçin ve **Tanılama çalıştır'ı seçtiğimde uygula** seçeneğini işaretleyin.
3. **Tanılama çalıştır** düğmesine basın. KillerBench bir ilke sahibi olup olmadığını denetler, geçerli değeri kaydeder, tek değişikliği yapar ve geri okur.

Grup ilkesinin veya aygıt yönetiminin sahip olduğu ayarlara dokunulmaz. Bunları yönetildikleri yerde değiştirin.

## Bir değişikliği geri alma

Doğrulanan her değişiklik bir günlük tutar. Eşleşen geri alma işlemi, yine bir ilke denetiminden sonra, tam önceki değeri geri yükler.

## Windows'u sadeleştirme

Öneriler, tüketici içeriği, Recall ve yapay zekâ özellikleri, görev çubuğu ve Başlat dağınıklığı, Edge ve önceden yüklenmiş yazılımlar için **Bakım** bölümünü açın ve **Windows'u sadeleştir** öğesini seçin. İnceleme salt okunurdur ve hiçbir şey otomatik olarak seçilmez.

## Komut satırından

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Bkz. [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) ve [debloat-review](cli.html#cmd-debloat-review). Desteklenmeyen bir anahtar desteklenen seçenekleri listeler.

## İlgili

- [Gizlilik bölümü](privacy.html)
- [Yeni bilgisayar kurma](set-up-new-pc.html)
