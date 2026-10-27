# ネットワークとインターネットの問題を解決する

コンピューターがインターネットに接続できない、サーバーや名前に到達できない、接続が頻繁に切れる場合に使います。KillerBench はアダプター、アドレス、ゲートウェイ、DNS、ファイアウォールを順に確認し、新しい証拠で必要性が証明された場合にのみリースの更新やサービスの開始を行います。

## 接続を確認する

1. **ネットワーク** を開き、**ネットワーク** を選びます。
2. 特定の対象をテストするには、**ホスト名の検索（オプション）**、または **TCP宛先（オプション）** と **TCPポート** に入力します。ローカルの構成だけを確認する場合は空欄のままにします。アプリケーション データは送信されません。
3. ネットワークが遅い場合は、**検索または接続ごとのタイムアウト（秒）** を設定します。
4. **確認された問題を自動修復** をオフのままにして、**診断を実行** を押します。
5. レポートを読みます。アドレスの欠落、ゲートウェイの欠落または到達不能、DNS 構成の問題、プロキシ設定、ファイアウォール プロファイルの不一致、検索または接続の失敗が区別され、それぞれに次の手順が示されます。

ゲートウェイから応答があってもインターネットへのアクセスが証明されるわけではなく、構成されたリゾルバーでの検索が失敗してもサイトが停止しているとは限りません。

## レポートで見つかった問題を修復する

1. **確認された問題を自動修復** をオンにして、もう一度 **ネットワーク** を実行します。有効な DNS Client、DHCP Client、SMB クライアント、Network Location Awareness、Network List、IP Helper の各サービスを開始でき、使用可能な IPv4 アドレスがない接続済みの物理 DHCP アダプターのリースを更新できます。
2. 更新の際は、接続が一時的に切れることがあります。静的アドレスと仮想アダプターが変更されることはありません。
3. キャッシュされた 1 つの名前が誤っている場合は、すべてを消去するのではなく、そのホストに限定した DNS キャッシュのフラッシュを使います。

ルート、Hosts のエントリ、DNS サーバー、ファイアウォール規則、Winsock プロバイダー、ネットワーク プロファイルは常に保持されます。

## ワイヤレス、有線サインイン、プロキシ

- **ワイヤレス ネットワーク** はアダプターと WLAN AutoConfig を確認し、サービスが停止している場合は開始できます。
- **有線認証 (802.1X)** は、ポートでのサインインが必要なネットワーク向けに Wired AutoConfig で同じことを行います。
- **WinHTTP 自動プロキシ** は、コンピューターと現在のアカウントの保存済みプロキシ設定を表示します。KillerBench が診断中に PAC URL を取得したりプロキシ設定を変更したりすることはありません。

## コマンド ラインから

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

[workflow](cli.html#cmd-workflow)、[dns-check](cli.html#cmd-dns-check)、[tcp-check](cli.html#cmd-tcp-check)、[dhcp-renew](cli.html#cmd-dhcp-renew)、[dns-cache](cli.html#cmd-dns-cache) を参照してください。

## 関連項目

- [ネットワーク セクション](network.html)
- [印刷の問題を解決する](fix-printing.html)
- [変更の安全性を保つ仕組み](safety.html)
