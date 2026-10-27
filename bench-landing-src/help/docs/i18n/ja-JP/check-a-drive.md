# ドライブを確認して修復する

ボリュームがエラーを報告する、Windows がドライブのスキャンを求める、ファイルが開けない場合に使います。KillerBench はまずボリュームを読み取り専用で確認し、バックアップを確認した後にのみファイル システムの修復をスケジュールまたは実行します。

## 始める前に

- ボリューム上の重要なデータをバックアップしてください。ファイル システムの修復では、破損したファイルが変更または削除される場合があります。
- KillerBench を管理者として実行してください。

## ボリュームを確認する

1. **ストレージ** を開き、**ストレージ** を選びます。
2. **チェックするボリューム** でドライブを選び、**診断を実行** を押します。
3. レポートを読みます。ディスクとボリュームの正常性、ダーティ ビット、NTFS のオンライン自己修復、コントローラーとディスク デバイスのエラー、信頼性カウンター、リセット、再試行、予期しない取り外しなど最近のストレージ イベントが表示されます。

問題がなければ修復は提示されません。ディスクの温度、摩耗、未訂正の I/O カウンターは確認用の証拠であり、それだけで修復が実行されることはありません。

## ファイル システムを修復する

1. **ファイルシステム修復を確認** を押し、**スポット修復** または **オフラインスキャンと修復** を選びます。
2. データ ボリュームの場合、それを使用しているものをすべて閉じられるなら **システム以外のボリュームをオフラインにすることを許可** にチェックを入れます。Windows のボリュームはオフラインにできないため、その修復は次回の再起動時にスケジュールされます。
3. **このボリュームの重要なデータはバックアップ済みです** にチェックを入れます。
4. 修復を確認します。KillerBench は実行の直前に対象のボリュームを再確認し、先に保留中のエントリを記録します。
5. スケジュールされた修復の場合は、都合のよいときに再起動します。KillerBench は再起動後に、修復を再実行せずに結果を検証します。

## 最適化または再トリム

同じツールで、ボリュームを分析し、Windows が推奨する最適化、**ソリッドステートストレージを再トリム**、またはハード ディスクのデフラグを実行できます。いずれも、正常な 1 つのボリュームに対する個別に確認する操作です。

## コマンド ラインから

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

[storage-check](cli.html#cmd-storage-check)、[storage-repair-plan](cli.html#cmd-storage-repair-plan)、[storage-repair](cli.html#cmd-storage-repair)、[volume-optimize](cli.html#cmd-volume-optimize) を参照してください。

## 関連項目

- [ストレージ セクション](storage.html)
- [ディスク領域を解放する](free-disk-space.html)
- [トラブルシューティング](troubleshooting.html)
