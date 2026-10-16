# スクリプトから KillerBench を実行する

スクリプト、RMM ジョブ、リモート コンソールで KillerBench の証拠を使いたい場合は、コマンド ラインを使います。`killerbench-cli` はデスクトップ アプリと同じ Engine を実行し、JSON を出力します。

## はじめに

1. `killerbench-cli.exe` がある場所で PowerShell を開くか、そのフォルダーをパスに追加します。
2. `killerbench-cli help` を実行すると、すべてのコマンドとその安全性の境界が一覧表示されます。
3. `killerbench-cli diagnostic-scopes` を実行すると、推奨される 11 の診断グループが表示されます。

## スキャンを実行してレポートを保存する

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` は、`--auto-repair` を追加しない限り読み取り専用です。`--report` は、アプリがエクスポートするのと同じテーマ付きのレポートを書き込みます。一部の領域だけを確認するには `--scope` を、ワークフロー ジャーナルなしで JSON をまとめて取得するには `common-check` を使います。

## 終了コードを読む

| コード | 意味 |
| --- | --- |
| 0 | プレビューまたは検証済みの結果が成功しました。 |
| 1 | 起動、入力、またはジャーナルのエラーでコマンドが停止しました。 |
| 2 | 引数を認識できませんでした。 |
| 3 | ブロック、失敗、または未検証です。 |
| 4 | ワークフローの検証の前に再起動が必要です。 |
| 130 | ユーザーが停止しました。 |

読み取り専用コマンドの終了コード 0 は、コマンドが実行されたことを意味し、コンピューターが正常であることを意味するものではありません。結論については JSON を読んでください。

## 変更を行う

コンピューターを変更できるすべてのコマンドは、既定でプレビューを表示します。プレビューを読んでから、同じコマンドを `--execute` 付きで実行します。要求が中断された場合は、対応する `-recover` コマンドを実行します。このコマンドは実際の結果を報告し、変更を繰り返すことはありません。

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## 以前の結果を開き直す

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## AI アシスタントから使う

[KillerMCP](../mcp.html) は、インストール済みの CLI を呼び出して、デバイス マネージャーと Windows のエラー コードを調べることができます。

## 関連項目

- [コマンド ライン リファレンス](cli.html)
- [Engine 開発者ガイド](../engine/index.html)
- [トラブルシューティング](troubleshooting.html)
