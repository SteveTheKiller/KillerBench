# BitLocker でドライブを暗号化する

BitLocker を有効にし、回復キーをバックアップして、ドライブが保護されていることを確認するために使います。KillerBench は暗号化を開始する前に回復パスワードをバックアップして検証し、回復パスワードを表示したりログに記録したりすることはありません。

## 始める前に

- KillerBench を管理者として実行してください。
- Windows ドライブには TPM 保護機能と回復パスワード保護機能が必要です。データ ドライブに必要なのは回復パスワードだけです。
- 回復キーのバックアップ先を決めます。暗号化するドライブ以外にあるファイルと、必要に応じて Active Directory または Microsoft Entra ID です。

## ドライブを確認する

1. **BitLocker** を開き、**ボリューム** でドライブを選びます。
2. 操作を **検査** のままにして、**診断を実行** を押します。レポートには、暗号化の状態、保護の状態、保護機能の識別情報が表示されます。
3. TPM はあっても準備ができていない場合は、先に **セキュリティ** で準備してください。KillerBench が TPM をクリアすることはありません。

## 必要な保護機能を追加する

1. **その他の BitLocker 操作** を開きます。
2. **TPM 保護機能を追加** または **回復パスワードを追加** を選び、**プレビュー** を押し、**[操作を実行] を押したら選択したドライブにこの変更を行う** にチェックを入れてから、**操作を実行** を押します。
3. 新しい回復パスワードはすぐにバックアップしてください。KillerBench はそれを表示しません。

## 回復キーをバックアップして暗号化する

1. **回復用の保護機能を検索** を押し、回復パスワード保護機能を選びます。
2. 暗号化するドライブ以外の場所にあるバックアップ ファイルの完全なパスを入力します。
3. **暗号化** を選び、暗号化方式を選んで、**[診断を実行] を選択したときに、選択した回復キーをバックアップしてから暗号化を開始する** にチェックを入れます。
4. **診断を実行** を押します。KillerBench はバックアップを書き込んで検証し、その後で暗号化を 1 回だけ開始します。
5. 暗号化はバックグラウンドで続行されます。後で **検査** を実行し、ドライブが完全に暗号化され保護がオンになっていると報告されることを確認してください。

## Active Directory または Entra ID にバックアップする

**その他の BitLocker 操作** で、選択した保護機能のディレクトリへのバックアップを選びます。KillerBench は Windows が要求を受け付けたことを報告しますが、ディレクトリ上のコピーは読み取れないため、Active Directory または Entra 管理センターで確認してください。

## データ ドライブと自動ロック解除

データ ドライブを暗号化した後、**自動ロック解除をオンにする** を使うと、Windows の起動時にドライブのロックが解除されます。Windows ドライブは、保護がオンの状態で完全に暗号化されている必要があります。

## コマンド ラインから

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

[bitlocker-protectors](cli.html#cmd-bitlocker-protectors)、[bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add)、[bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt)、[bitlocker-escrow](cli.html#cmd-bitlocker-escrow) を参照してください。それぞれに、中断された要求のための対応する `-recover` コマンドがあります。

## 関連項目

- [BitLocker セクション](bitlocker.html)
- [セキュリティ セクション](security.html)
- [変更の安全性を保つ仕組み](safety.html)
