# 新しいコンピューターをセットアップする

新しい PC やリセットしたばかりの PC を準備するときに使います。KillerBench はコンピューター、インストール予定のアプリケーション、付けたい名前を確認し、それぞれの変更を行う保護付きのツールを案内します。

## 基準から始める

1. **診断** から **システム全体のスキャン** を実行し、そのレポートをコンピューターの初期状態の記録として保存します。
2. 保留中の更新プログラムをインストールします。失敗したものがあれば、[Windows Update を修正する](fix-windows-update.html) を参照してください。

## アプリケーションと構成を確認する

1. **マシンの準備** を開き、**マシンの準備** を選びます。
2. セットアップ予定のアプリケーションと構成グループを選び、**診断を実行** を押します。確認では、それらをすでにインストールされているものと比較します。KillerBench がインストーラーをダウンロードしたり実行したりすることはありません。
3. ベンダーのインストーラーを実行する前に、コマンド ラインで検証してください。KillerBench は、そのベンダーの正確な組織名による有効な署名がある場合にのみインストーラーを信頼します。

## コンピューターに名前を付ける

1. **コンピューター名** を選び、名前を入力して **診断を実行** を押し、現在の識別情報とドメインでの役割に照らして名前を確認します。
2. ワークグループのコンピューターでは、**新たなチェックの後でこのワークグループ PC の名前を変更** にチェックを入れて名前を変更します。完了するには再起動が必要です。

## 保護と整理

- [BitLocker でドライブを暗号化](encrypt-with-bitlocker.html) し、回復キーをバックアップします。
- [Windows のプライバシーを強化](make-windows-private.html) し、**メンテナンス** の **Windows の軽量化** を確認します。
- すべての準備が整ったら、**復元ポイントを1つ作成する** で復元ポイントを作成します。

## コマンド ラインから

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

[workflow](cli.html#cmd-workflow) と [restore-point](cli.html#cmd-restore-point) を参照してください。

## 関連項目

- [マシンの準備セクション](onboarding.html)
- [初めての診断](first-run.html)
