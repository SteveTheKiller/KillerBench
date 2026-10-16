# Windows のプライバシーを強化する

ノート PC のプライバシーを確認して強化するために使います。KillerBench は現在の設定を読み取り、ポリシーやデバイス管理がすでに所有している設定を示し、選択した設定だけを 1 つずつ、正確に元に戻せる形で変更します。

## 現在の設定を確認する

1. **プライバシー** を開きます。
2. 確認したい領域にチェックを入れます。すべての領域は未選択の状態で始まります。
3. **診断を実行** を押します。レポートには、このコンピューターと KillerBench を実行しているアカウントの現在の値が表示され、ポリシーが所有する設定が示されます。

## 設定を変更する

1. **プライバシー設定** で、カメラ、マイク、位置情報、診断データ、広告、クリップボードの履歴、クラウド コンテンツ検索、検索のハイライトなどの設定を選びます。
2. 新しい値を選び、**[診断を実行] を選択したときに適用する** にチェックを入れます。
3. **診断を実行** を押します。KillerBench はポリシーの所有者を確認し、現在の値を記録して、1 つの変更を行い、読み直します。

グループ ポリシーやデバイス管理が所有する設定は変更されません。それらは管理されている場所で変更してください。

## 変更を元に戻す

検証済みの変更にはそれぞれジャーナルが残ります。対応する元に戻す操作は、再度ポリシーを確認した後で、以前の値を正確に復元します。

## Windows を軽量化する

おすすめ、コンシューマー向けコンテンツ、Recall と AI 機能、タスク バーとスタート メニューの不要な項目、Edge、プリインストール ソフトウェアについては、**メンテナンス** を開いて **Windows の軽量化** を選びます。確認は読み取り専用で、自動的に選択されるものはありません。

## コマンド ラインから

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

[privacy-plan](cli.html#cmd-privacy-plan)、[privacy-setting](cli.html#cmd-privacy-setting)、[privacy-setting-undo](cli.html#cmd-privacy-setting-undo)、[debloat-review](cli.html#cmd-debloat-review) を参照してください。サポートされていないキーを指定すると、サポートされている選択肢が一覧表示されます。

## 関連項目

- [プライバシー セクション](privacy.html)
- [新しいコンピューターをセットアップする](set-up-new-pc.html)
