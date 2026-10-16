# コマンド ライン

`killerbench-cli` は、デスクトップ アプリと同じ Engine を PowerShell やリモート コンソールから実行します。コンピューターを変更する可能性のあるコマンドは、すべて既定でプレビューを表示し、実行するには `--execute` などの明示的なフラグが必要です。コマンドは JSON の証拠を標準出力に、進行状況を標準エラーに出力します。

手順ガイド: [スクリプトから KillerBench を実行する](use-the-cli.html)。

終了コード 0 はプレビューまたは検証済みの結果が成功したこと、1 は起動、入力、ジャーナルのエラー、2 は引数を認識できなかったこと、3 はブロック、失敗、または未検証、4 はワークフローの検証の前に再起動が必要なこと、130 はユーザーが停止したことを意味します。

引数なしで `killerbench-cli` を実行すると、お使いのコンピューターでこの一覧を表示できます。

## 更新とサービシング

- `bits-job-review JOB_GUID [--all-users] [--observe]`: 正確な転送の確認または 2 回のスナップショットによる観察。再試行なし。
- `bits-jobs [--all-users]`: BITS ジョブの状態、所有者、バイト数。転送の変更なし。
- `bits-retry JOB_GUID [--execute]`: 既定でプレビュー。正確なジョブを明示的に再試行。アプリケーションの完了は主張しません。
- `bits-retry-recover JOB_GUID`: 読み取り専用のスナップショット 2 回。安定した結果を再試行せずにアーカイブ。
- `component-repair-source`: 管理された DISM 修復ソースのポリシーと競合。変更なし。
- `delivery-optimization`: 配信の最適化サービスの状態、読み取り専用。JSON の証拠。
- `delivery-optimization-activity`: プライバシーに配慮して範囲を限定した現在のジョブ数とバイト数の合計。転送の識別情報なし、変更なし。
- `delivery-optimization-policy`: 従来の設定と Policy CSP の設定、読み取り専用。機密の値とプロバイダーの識別情報は除外。
- `feature-service-events`: Time、Search、WMI、Event Log、オーディオの各サービスの正確な障害履歴を読み取ります。
- `feature-update-policy`: 対象リリース、延期、一時停止、セーフガードのポリシー、読み取り専用。JSON の証拠。
- `pending-file-operations`: プライバシーに配慮して範囲を限定した保留中のファイル操作の概要、読み取り専用。JSON の証拠。
- `restart-check`: 保留中の再起動のソース、読み取り専用。JSON の証拠。
- `servicing-logs`: 最新の CBS、テキストと圧縮の CBS アーカイブ、DISM、Windows Setup のログを範囲を限定して分析。修復なし。
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: サービシング アクティビティのスナップショット、読み取り専用。
- `servicing-service-events`: Windows Update サービシング チェーンの正確な障害履歴、読み取り専用。
- `servicing-service-states`: Windows Update サービシング チェーンの 6 つのサービスの現在の状態、読み取り専用。
- `setup-code DECIMAL_OR_0xHEX`: Windows Setup の結果コードのドキュメントに基づく参照。コンピューターの確認や修復なし。
- `update-applicability`: Windows のエディション、ビルド、アーキテクチャの証拠、読み取り専用。
- `update-cache`: 範囲を限定した SoftwareDistribution キャッシュの合計、読み取り専用。ファイル名なし。
- `update-code DECIMAL_OR_0xHEX`: Windows Update の結果コードのドキュメントに基づく参照。コンピューターの確認や修復なし。
- `update-context`: 現在の昇格とデスクトップ セッションの証拠、読み取り専用。アカウントの識別子なし。
- `update-engine-files`: ネイティブの Windows Update エンジンの有無、バージョン、署名の証拠、読み取り専用。
- `update-events`: 過去 7 日間の Windows Update Client の履歴、読み取り専用。生の証拠を含む JSON。
- `update-followup DECIMAL_OR_0xHEX`: ドキュメントに記載された Windows Update の結果コードで選択する、新しい読み取り専用の診断。
- `update-history`: 最新 100 件の Windows Update 操作の結果、読み取り専用。JSON の証拠。
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: `--select` を繰り返し指定します。既定でプレビュー。
- `update-policy`: 管理された Windows Update ポリシー、読み取り専用。JSON の証拠。
- `update-policy-surfaces`: 従来の設定と Policy CSP の構成面の証拠、読み取り専用。値とプロバイダーの識別情報は除外。
- `update-recover`: 保留中の更新プログラムを確認します。インストールや再試行は行いません。
- `update-restart-policy`: コンプライアンスの期限、猶予期間、再起動の抑制、通知の動作、読み取り専用。
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: JSON による検索結果と一致。インストールなし。
- `update-service-events`: Windows Update サービスの正確な障害履歴、読み取り専用。
- `update-service-files`: Windows Update サービスの正確な DLL 登録と署名の証拠、読み取り専用。
- `update-services`: Windows Update サービスの登録、読み取り専用。JSON の証拠。
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: 新しい検索が前提。既定でプレビュー。古いストアを保持します。
- `update-store-reset-recover`: 準備したストアのリセットを繰り返さずに照合します。
- `windows-capabilities`: オンラインの Windows の機能の状態、読み取り専用。JSON の結果。
- `windows-update-task`: Scheduled Start の正確なタスクの状態と実行履歴、読み取り専用。タスクの実行や更新操作なし。

## BitLocker と TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: 既定でプレビュー。暗号化されたデータ ボリュームの自動ロック解除をオンにします。
- `bitlocker-auto-unlock-recover`: 中断された自動ロック解除の要求を繰り返さずに検証します。
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: 既定でプレビュー。新しい回復用バックアップ ファイルのみ。
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: 中断されたバックアップを再度書き込まずに検証します。
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: 既定でプレビュー。必要な方式で再度暗号化する前などに、暗号化の解除を開始します。
- `bitlocker-decrypt-recover`: 中断された暗号化解除の要求を繰り返さずに検証します。
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: 既定でプレビュー。既存の TPM 保護機能と回復用の保護機能が必要です。
- `bitlocker-encrypt-recover`: 保留中の変換の状態を、要求を繰り返さずに検証します。
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: 既定でプレビュー。既存の回復パスワードを Active Directory または Microsoft Entra ID にバックアップします。
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: オプションのプレビューのみ。
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: 既定でプレビュー。TPM または回復パスワードの保護機能を 1 つ追加します。パスワードは表示されません。
- `bitlocker-protector-recover`: 中断された保護機能の要求を繰り返さずに検証します。
- `bitlocker-protectors DRIVE:`: 保護機能の識別情報と種類のみ。回復パスワードなし。
- `bitlocker-resume DRIVE: [--execute]`: 既定でプレビュー。完全に暗号化され保護が中断されている正確なボリュームのみ。
- `bitlocker-resume-recover`: 保留中の保護の状態を、要求を繰り返さずに検証します。

## セキュリティとプライバシー

- `certificate-chains`: 個人用証明書について、キャッシュのみを使ったローカルの信頼チェーン、読み取り専用。失効確認やダウンロードなし。
- `defender-overview [--report]`: 保護、スキャンの時期、脅威の集計の概要、読み取り専用。操作なし。
- `defender-protection [--report]`: 保護の設定と認識されたポリシー面、読み取り専用。機密の詳細は除外。変更なし。
- `defender-scan Quick|Full [--execute] [--report]`: 既定でプレビュー。明示的な保護付きの Microsoft Defender スキャンを 1 回。
- `defender-scan-history [--report]`: クイック スキャンとフル スキャンの時期と期限超過の状態、読み取り専用。機密の詳細は除外。スキャンなし。
- `defender-scan-recover [--report]`: 別のスキャンを開始せずに、読み取り専用で照合します。
- `defender-threats [--report]`: 脅威の履歴の集計、現在の有効なフラグ、重大度の ID。機密の詳細は除外。操作なし。
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: 既定でプレビュー。管理されていないローカル プロファイルのみ。
- `firewall-enable-recover`: 保留中の変更を繰り返さずに確認します。
- `firewall-events`: 範囲を限定した Windows ファイアウォールの警告、エラー、重大イベントの動作履歴を読み取ります。
- `firewall-logging`: ドメイン、プライベート、パブリックのトラフィック ログの構成を、ログを開かずに読み取ります。
- `firewall-rules`: プライバシーに配慮して範囲を限定した有効な規則とポリシー ソースの数を読み取ります。
- `firewall-service-events`: BFE と Windows Defender ファイアウォールについて、サービス コントロール マネージャーの正確な障害履歴を読み取ります。
- `privacy-clipboard-history on|off [--execute]`: 既定でプレビュー。ポリシーを保持したまま、現在のユーザーの正確な値。
- `privacy-clipboard-history-recover`: 中断された変更を繰り返さずに再確認します。
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: 以前の値を保護付きで復元します。
- `privacy-plan --select Camera --select ClipboardSync`: 選択内容の確認のみ。設定の変更なし。
- `privacy-setting KEY on|off [--execute]`: 既定でプレビュー。ポリシーを保持した現在のユーザーの変更。無効なキーを指定すると対応する選択肢を表示。
- `privacy-setting-recover KEY`: 中断された変更を繰り返さずに再確認します。
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: 正確な値を保護付きで復元します。
- `security-check`: TPM、セキュア ブート、仮想化による保護、UAC ポリシー、読み取り専用。JSON の証拠。

## サービス

- `service-binaries`: ローカルのサービス バイナリの有無と Authenticode の状態、読み取り専用。パスは除外し、ローカル以外のパスには接続しません。
- `service-check NAME [NAME ...] | service-check --all`: 対応するサービスの状態と依存関係の証拠、読み取り専用。
- `service-dependencies`: 直接の依存関係の整合性の確認、読み取り専用。サービスの変更なし。
- `service-dlls`: 登録されたサービス DLL の有無と Authenticode の状態、読み取り専用。パスは除外し、ローカル以外のパスには接続しません。
- `service-events`: サービスの障害履歴、読み取り専用。生の証拠を含む JSON。
- `time-events`: 最近の Windows Time のイベント履歴、読み取り専用、JSON の証拠付き。
- `service-logons`: サービス アカウントの種類とデスクトップとの対話の確認、読み取り専用。アカウントの識別情報は除外。
- `service-path-quoting`: サービスの実行可能ファイル パスの引用符の確認、読み取り専用。完全なパスと引数は除外。
- `service-recovery-policy`: インストール済みサービスの回復アクションの概要、読み取り専用。コマンドと再起動メッセージは除外。
- `service-startup-config`: 開始モードの整合性の確認、読み取り専用。サービスの変更なし。
- `service-transitions`: 保留中のサービスの状態を繰り返し観察、読み取り専用。サービスの操作なし。
- `service-triggers`: インストール済みサービスのトリガーの概要、読み取り専用。トリガーの内容は除外。変更なし。

## ストレージ、ファイル、バックアップ

- `backup-events`: VSS と volsnap の履歴。JSON の証拠。変更なし。
- `backup-service-events`: VSS、ソフトウェア シャドウ コピー プロバイダー、Windows Backup、ブロック レベル エンジンの正確な障害履歴を読み取ります。
- `backup-summary`: ローカルの Windows Backup 操作の概要。バックアップの変更なし。
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: 既定でプレビュー。ごみ箱は全部か無しかで、経過日数は適用されません。
- `disk-cleanup-task`: 正確なタスクの状態と実行履歴、読み取り専用。タスクの実行やクリーンアップなし。
- `disk-diagnostic`: 正確なディスク診断タスクとイベント履歴を照合。タスクやディスクの操作なし。
- `disk-diagnostic-tasks`: コレクター タスクとリゾルバー タスクの正確な履歴、読み取り専用。タスクやディスクの操作なし。
- `disk-quotas`: ボリュームごとのクォータの既定値と状態、読み取り専用。ユーザーの識別情報は除外。変更なし。
- `file-history-events`: プライバシーに配慮したファイル履歴の警告とエラーの件数。変更なし。
- `file-history-status`: 現在のアカウントのファイル履歴の保護の状態。変更なし。
- `search-index-rebuild [--execute]`: 既定でプレビュー。以前のインデックスを保持し、新しい状態を検証します。
- `search-index-rebuild-recover`: 修復コマンドを繰り返さずに、読み取り専用で照合します。
- `storage-check DRIVE_LETTER`: 固定の NTFS チェック、読み取り専用。JSON の結果とライブの進行状況。
- `storage-events`: ストレージの履歴、読み取り専用。生の証拠を含む JSON。
- `storage-jobs`: Storage モジュールのジョブの状態と進行状況の集計、読み取り専用。JSON。変更なし。
- `storage-reliability`: 温度、摩耗、I/O のカウンター、読み取り専用。JSON。修復なし。
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: 既定でプレビュー。実行にはバックアップの確認が必要。システム ボリュームは再起動時にスケジュールされます。
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: プレビューのみ。修復やスケジュールなし。
- `storage-repair-recover`: 完了またはスケジュール済みのファイル システム修復を繰り返さずに検証します。
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: 現在の証拠を収集して結び付けます。修復やスケジュールなし。
- `storage-sense-policy`: 認識されたコンピューターのクリーンアップ ポリシー、読み取り専用。ユーザーの設定とファイルの識別情報は除外。
- `storage-sense-task`: 正確なタスクの状態と実行履歴、読み取り専用。タスクの実行やクリーンアップなし。
- `storage-service-events`: ストレージ サービス、ドライブの最適化、VSS、ソフトウェア シャドウ コピー プロバイダーの正確な障害履歴を読み取ります。
- `volume-capacity DRIVE:`: 利用可能な容量、読み取り専用、JSON の証拠付き。
- `volume-dirty DRIVE:`: ダーティ ビットの状態、読み取り専用。修復や再起動なし。
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: 既定で読み取り専用の分析。正常な正確なボリュームと互換性のあるメディアが必要です。
- `volume-optimize-recover`: 保留中の完了した操作を繰り返さずに検証します。
- `vss-providers`: VSS プロバイダーの名前、バージョン、種類、読み取り専用。識別子は除外。登録の変更なし。
- `vss-writers`: 報告されたライターの状態。英語の出力を解釈。リセットなし。

## 印刷

- `print-events`: 過去 7 日間の PrintService Admin の履歴、読み取り専用。JSON の証拠。
- `print-extension-files`: 印刷プロセッサと印刷モニターのバイナリの有無と署名の概要、読み取り専用。完全なパスは除外。
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: 既定でプレビュー。Error または Blocked の正確なジョブのみ。
- `print-job-cancel-recover [--folder PATH]`: 中断されたキャンセルを繰り返さずに再確認します。
- `print-job-list`: 現在表示されているジョブを変更せずに一覧表示します。
- `print-operational-events`: PrintService Operational チャネルの状態と利用可能な履歴、読み取り専用。JSON の証拠。
- `print-policy`: Point and Print の制限とドライバーのインストール ポリシー、読み取り専用。JSON の証拠。
- `print-rendering-policy`: レンダリングとドライバー分離のポリシー、読み取り専用。JSON の証拠。
- `print-spooler-events`: ローカルの Print Spooler の識別情報に関連するサービス コントロール マネージャーの障害、読み取り専用。JSON の証拠。
- `printer-check --queue NAME [--queue NAME ...]`: 選択したローカル キューの設定、読み取り専用。
- `printer-conditions`: ローカル キューの状態コード、読み取り専用。物理的な検証や変更なし。
- `printer-driver-files`: 登録されたファイルの有無と実行可能ファイルの署名の概要、読み取り専用。完全なパスは除外。
- `printer-driver-models`: バージョン 3、バージョン 4、パッケージ対応の概要、読み取り専用。JSON の証拠。
- `printer-drivers`: キューとドライバーの登録の一覧と照合、読み取り専用。JSON の証拠。
- `printer-endpoint-check`: 割り当てられた正確な Standard TCP/IP プリンター エンドポイントへの範囲を限定した接続。検出、SNMP、印刷データなし。
- `printer-list`: 設定を変更せずにローカル キューを検出します。
- `printer-ports`: キューとポートの登録の照合、読み取り専用。JSON の証拠。
- `printer-queue-attributes`: ローカル キューのスプール、保持、共有の属性、読み取り専用。
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: 既定でプレビュー。オフライン モードを解除すると待機中のジョブが送られる場合があります。

## ネットワーク

- `dhcp-check`: 欠落または期限切れの DHCP リースの適格性、読み取り専用。JSON の証拠、更新なし。
- `dhcp-events`: IPv4 と IPv6 の DHCP Client のイベント履歴、読み取り専用。JSON の結果。
- `dhcp-leases`: リースのタイムスタンプ、有効期限、サーバーの証拠、読み取り専用。JSON の結果。
- `dhcp-renew [--execute]`: 既定でプレビュー。接続済みの物理アダプターの、欠落または期限切れの正確な DHCP リースのみ。
- `dhcp-renew-recover`: 別のリースを要求せずに、読み取り専用で照合します。
- `dns-cache HOST [--execute]`: 既定でプレビュー。正確なホストの検証を伴う、保護付きのローカル キャッシュのフラッシュ。
- `dns-cache-recover`: 中断されたキャッシュ操作を、フラッシュを繰り返さずに再確認します。
- `dns-cache-summary`: プライバシーに配慮して範囲を限定したローカル DNS キャッシュの状態とレコードの種類ごとの件数。変更なし。
- `dns-check HOST [--timeout-seconds 1..60]`: 範囲を限定したシステムの名前解決。JSON の結果。
- `dns-client-config`: サフィックス、登録、名前の補完、ポリシーの証拠、読み取り専用。JSON の結果。
- `dns-client-events`: プライバシーに配慮して範囲を限定した DNS Client の警告と障害の履歴、読み取り専用。JSON の結果。
- `dns-nrpt-policy`: 有効な DNS 名前空間ポリシーの証拠、読み取り専用。JSON の結果。
- `ip-addresses`: 現在の IP アドレスの状態、取得元、競合の確認、読み取り専用。JSON の証拠。
- `ip-interface-config`: IP インターフェイスの構成の証拠、読み取り専用。JSON の結果。
- `network-firewall`: 有効なネットワークの割り当てを、対応するファイアウォール プロファイルの有効化と照合します。
- `network-offload`: Windows のグローバルなネットワーク オフロードの構成、読み取り専用。JSON の結果。
- `network-profiles`: 現在のプロファイルのカテゴリと接続の分類、読み取り専用。ネットワーク名は除外。
- `network-service-events`: DHCP、DNS、Workstation、NLA、Network List、IP Helper の正確な障害履歴を読み取ります。
- `proxy-check`: 保存された WinHTTP と現在のアカウントのインターネット プロキシの証拠、読み取り専用。
- `route-priority`: 有効な既定ルートのメトリックの比較、読み取り専用。JSON の証拠。
- `smb-client-config`: SMB の署名、ゲスト アクセス、トランスポートの構成、読み取り専用。
- `smb-connections`: 現在のプロトコル ネゴシエーション、読み取り専用。リモートの識別情報は除外。
- `smb-server-config`: SMB のプロトコル、署名、暗号化、マルチチャネルの構成、読み取り専用。
- `smb-shares`: 範囲を限定した SMB 共有の一覧、読み取り専用。パス、アクセス許可、セッション、アカウントは除外。
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: 範囲を限定した接続チェック。アプリケーション データは送信しません。
- `tcp-port-pressure`: 動的 TCP の範囲とローカル ポートの使用状況の集計、読み取り専用。エンドポイントとプロセスは除外。
- `tcp-settings`: Windows の TCP テンプレートの構成の証拠、読み取り専用。JSON の結果。
- `vpn-profiles`: 現在のアカウントの VPN プロファイルの構成、読み取り専用。資格情報は除外。
- `winrm-listeners`: WinRM リスナーの登録の概要、読み取り専用。証明書の拇印は除外。
- `winsock-catalog`: ネイティブの Winsock プロトコル カタログの構造、読み取り専用。JSON の結果。
- `winsock-namespaces`: ネイティブの Winsock 名前空間プロバイダーの構造、読み取り専用。JSON の結果。
- `winsock-provider-files`: プロトコル プロバイダーのファイルの有無と署名、読み取り専用。JSON の結果。
- `winsock-readiness`: ネイティブの Winsock プロトコル、名前空間プロバイダー、プロバイダー ファイルの準備状態、読み取り専用。JSON の結果。

## アプリ

- `app-folder-review FOLDER_NAME [--report]`: 固定されたルートにある正確なフォルダーの確認。削除なし。
- `app-package-review FULL_NAME [--all-users]`: 正確な対象パッケージと依存パッケージ。削除なし。
- `app-packages [--all-users]`: 登録された Appx/MSIX の一覧のみ。削除なし。
- `browser-extensions`: Chrome、Edge、Brave、Firefox の拡張機能の識別情報の確認。削除なし。
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: 確認のみ。システムの変更なし。
- `depot-review [application and configuration selections] [--json]`: 確認のみ。プロビジョニングなし。
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Authenticode とベンダーの署名者の確認、読み取り専用。何も実行しません。
- `foxit-inventory`: コンピューターの製品一覧、読み取り専用。JSON 出力。
- `installed-apps [--current-user]`: アンインストールの登録と機能フラグ。削除なし。
- `installed-apps-compare BEFORE.json AFTER.json`: 保存した一覧での登録の差分。コンピューターの変更なし。
- `installed-hotfixes`: 範囲を限定したインストール済み Windows 修正プログラムの一覧、読み取り専用。JSON の証拠。
- `installer-activity`: Installer のプロセスとトランザクション マーカーの証拠、読み取り専用。JSON の証拠。
- `installer-check`: Installer のポリシーとエンジンの準備状態、読み取り専用。JSON の証拠。
- `installer-engine-files`: 必須の Installer ファイルの有無と Authenticode の概要、読み取り専用。JSON の証拠。
- `installer-events`: 過去 7 日間の MSI の履歴、読み取り専用。JSON の証拠。
- `installer-logging`: Installer のログ記録とデバッグのポリシー、読み取り専用。JSON の証拠。
- `installer-pending-state`: 保留中のファイル操作と Installer の活動の照合、読み取り専用。JSON の証拠。
- `installer-registration-repair [--execute]`: 既定でプレビュー。正確なサービス登録の不具合のみ。エンジンの登録は解除しません。
- `installer-registration-repair-recover`: 中断された登録を、コマンドを繰り返さずに再確認します。
- `installer-security-policy`: Installer のセキュリティとパッチのポリシー、読み取り専用。JSON の証拠。
- `installer-service-events`: ローカルの Windows Installer の識別情報に関連するサービス コントロール マネージャーの障害、読み取り専用。JSON の証拠。
- `installer-service-registration`: Windows Installer サービスの登録の概要、読み取り専用。JSON の証拠。
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: 正確な適格性の新しいプレビュー。削除プロバイダーなし。
- `mace-package-recover`: 中断されたアプリ パッケージ削除のレコードを 1 件、新たに照合します。削除は繰り返しません。
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: 確認のみ。削除なし。
- `msi-cache`: 登録された Windows Installer キャッシュの可視性、読み取り専用。JSON の証拠。
- `msi-code NONNEGATIVE_DECIMAL`: Windows Installer の戻りコードのドキュメントに基づく参照。コンピューターの確認や修復なし。
- `msi-product {PRODUCT-CODE}`: 正確な MSI の登録状態、読み取り専用。JSON の証拠。
- `msi-repair {PRODUCT-CODE} [--execute]`: 既定でプレビュー。壊れているか構成に問題がある正確な製品のみ。
- `msi-repair-recover`: 中断された正確な製品の修復を繰り返さずに再確認します。
- `msi-sources`: プライバシーに配慮して範囲を限定した、登録済み Windows Installer ソースのメタデータ、読み取り専用。JSON の証拠。
- `orca-cache-reset --mode New|Classic|Both [--execute]`: 既定でプレビュー。実行中のアカウントの正確なキャッシュを回復用の名前で保持します。
- `orca-cache-reset-recover`: 中断されたキャッシュの保持を、移動を繰り返さずに検証します。
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: 確認のみ。Outlook の変更なし。
- `startup-apps`: スタートアップの登録と、起動時またはログオン時のタスクの確認。無効化なし。
- `third-party-update ID SOURCE VERSION [--accept-agreements] [--execute]`: 既定でプレビュー。正確な 1 つのバージョンのみ。
- `third-party-update-recover`: 保留中の正確なバージョンを、更新を再試行せずに検証します。
- `third-party-updates`: Microsoft.WinGet.Client によるアプリケーション更新の検索、読み取り専用。アプリケーションの変更なし。

## システム

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: ローカルでの Bluetooth SIG 割り当ての正確な検索。ネットワーク要求なし。
- `crash-config`: 保存された Windows のダンプ モードとパス。変更なし。
- `crash-dumps`: Windows の既定のダンプ ファイルのメタデータ。内容の分析や変更なし。
- `crash-events`: 最近のシステム エラー レポート。診断や変更なし。
- `crash-review [--knowledge PACK_PATH ...]`: 最大 32 パック。クラッシュ、シャットダウン、範囲を限定したミニダンプの構造、オプションの正確なバグ チェック参照をまとめて確認。変更なし。
- `default-printer`: 既定の選択と自動管理の設定、読み取り専用。JSON の証拠。
- `default-printer-set QUEUE [--execute] [--folder PATH]`: 既定でプレビュー。欠落または競合する既定のプリンターのみ。
- `default-printer-set-recover [--folder PATH]`: 中断された選択を繰り返さずに再確認します。
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: 最大 32 パックからのオプションの正確なドライバー参照を含むデバイスの証拠、読み取り専用。
- `device-code DECIMAL_OR_0xHEX`: デバイス マネージャーの問題コードのオフライン参照。
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: 参照の候補のみ。
- `event-channel-enable CHANNEL [--execute]`: 既定でプレビュー。管理されていない診断チャネルを正確に 1 つ有効にし、ほかの設定は保持します。
- `event-channel-enable-recover`: 有効化コマンドを繰り返さずに、読み取り専用で照合します。
- `event-channels`: 4 つの診断チャネルの状態、保持、サイズ、使用率、読み取り専用。
- `font-registrations`: コンピューターと現在のユーザーの登録とファイルの有無を範囲を限定して確認、読み取り専用。
- `memory-diagnostic-results`: 範囲を限定した Windows メモリ診断の履歴、読み取り専用。JSON の証拠。
- `minidump-analyze PATH`: 範囲を限定したミニダンプの構造メタデータ。メモリ ページ、シンボル、変更なし。
- `name-rename PROPOSED_NAME [--execute]`: 既定でプレビュー。スタンドアロンのワークグループ PC のみ。Windows を再起動しません。
- `name-rename-recover`: 保留中の名前の変更を繰り返さずに検証します。
- `name-review PROPOSED_NAME`: 識別情報と提案された名前の確認、読み取り専用。
- `profiles --recover`: 記録された不在を検証し、回復用の証拠を保持します。削除は再試行しません。
- `profiles --recover-backup`: 保留中のバックアップ成果物を検証してアーカイブします。コピーは再試行しません。
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: まずプレビュー。保護付きのネイティブ コピーには、明示的な実行と存在しないローカル NTFS の保存先が必要です。
- `profiles --select SID --create-backup-artifact DESTINATION`: 既存のコピー済みバックアップを比較し、固定の成果物を作成します。プロファイルのコピーや削除は行いません。
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: 読み取り専用の確認。検証には選択した SID が 1 つ必要です。
- `task-check`: タスクの一覧、分類された所見、JSON の証拠、読み取り専用。
- `task-definition TASK_FOLDER TASK_NAME`: 正確なタスクの XML、読み取り専用。利用できないことは存在しないことを意味しません。
- `task-inventory`: タスクの識別情報と状態、読み取り専用。JSON 出力。
- `task-journal RECORD.json [--completion COMPLETION.json]`: 過去の回復の確認。再試行は許可しません。
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: タスクの下書きを検証して確認します。登録なし。
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: 既定でプレビュー。無効で作成専用の Local System 定義。
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: 正確な定義の照合、読み取り専用。タスクの再試行や実行はしません。
- `task-xml PLAN.json YYYY-MM-DD`: 確認用の無効な XML を stdout に出力。ローカル タイム ゾーンのカレンダー スケジュールのみ。登録なし。
- `time-config`: Windows Time のモード、ピア、ポリシーの証拠、読み取り専用。
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: 既定でプレビュー。カスタム モードにはピアが必要です。
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: プレビューのみ。
- `time-recover --folder PATH`: 保留中のレコードを検証してアーカイブします。変更は再試行しません。
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: 読み取り専用。カスタム モードにはピアが必要です。
- `wmi-provider`: ローカルのオペレーティング システム プロバイダーへのクエリ、読み取り専用。JSON の証拠。

## ナレッジ、検証、Engine

- `knowledge PACK_PATH CONTEXT CODE`: ローカルでの参照の検索。修復の実行なし。
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: 検証済みのローカルでの有効化。ダウンロードなし。
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: 有効なローカル パックを検証します。変更なし。
- `knowledge-compare BEFORE_JSON AFTER_JSON`: 参照の変更のみ。パックのインストールなし。
- `knowledge-info PACK_PATH`: パックのメタデータとコンテキストごとのエントリ数。変更なし。
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: 観察されたコードにコンテキストに応じた参照を付けます。
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: 保持されている検証済みの最新のパックを選択します。ダウンロードなし。
- `knowledge-search PACK_PATH CONTEXT TEXT`: 参照テキストの一致を最大 50 件。修復の実行なし。
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: 最大 32 パックにわたって正確な識別情報を 1 つ解決します。競合時は選択しません。
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: 構成されたインストール済みのデータベースをすべて検証します。ダウンロードや有効化なし。
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: 構成された HTTPS エンドポイントから署名済みのデータベース候補を取得し、アトミックに有効化します。
- `open <file.kbench> [--json]`: 保存されたキャプチャを開き直し、インストール済みのルールで再評価します。
- `scan --output <new-file.kbench> [--json]`: 新しいファイルに保存される読み取り専用の Windows Update キャプチャ。
- `validation-coverage RECORD.json [...]`: 現在のバージョンのネイティブ VM の証拠をまとめます。診断や修復なし。
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: 使い捨て VM のシナリオ計画、読み取り専用。診断や修復なし。
- `validation-record RECORD.json`: 範囲を限定したネイティブ VM の証拠レコードを 1 件検証します。診断や修復なし。
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: 現在のバージョンのローカル VM の対象範囲レポート。
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 検証済みの読み取り専用の共有オプション プランを実行します。
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: 一覧のみ。
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 対象を絞った診断のジャーナル。修復なし。
- `workflow computer-name --name PROPOSED_NAME [--json]`: ジャーナルに記録する確認のみ。
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: オプションで正確な製品の状態をジャーナルに含めます。
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: オプションで選択した対象の診断をジャーナルに含めます。
- `workflow storage [--volume D:] [--json]`: ジャーナルに記録する読み取り専用のファイル システム チェック。
- `workflow-journal FILE.json`: 過去の内容。終了コード 0 は読み取りに成功したことを意味し、正常であることではありません。
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: ホストに依存しないコントロールを表示または検証します。スキャンや修復なし。
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: 過去のレポートのみ。コマンドは実行しません。

## その他のコマンド

- `<analyze|investigate> <text-log-path> [--json]`: ローカルのテキストの証拠を分析します。
- `adapter-bindings`: コア プロトコルのバインドの概要、読み取り専用。JSON の証拠。
- `adapter-statistics`: アダプターのバイト、エラー、破棄のカウンター、読み取り専用。JSON の証拠。
- `application-events`: 最近の Application のエラー、グループ別の件数、JSON の証拠、読み取り専用。
- `applocker-policy`: 有効な収集モードと規則数の集計、読み取り専用。規則の詳細は除外。変更なし。
- `common-check [--scope NAME ...]`: 既定ではすべての診断領域。指定した場合は選択した領域のみ。読み取り専用の JSON。
- `delete-notifications`: NTFS と ReFS の削除通知の構成、読み取り専用。JSON。最適化や設定の変更なし。
- `diagnostic-scopes [--all]`: 推奨される 11 の結果グループ、または読み取り専用の 49 領域すべてを一覧表示します。収集なし。
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 対象を絞った診断のジャーナル。修復なし。
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: すべての診断領域。`--auto-repair` を明示しない限り修復なし。
- `gateway-check`: 構成された既定の次ホップに対する正確で範囲を限定した ICMP チェック。アドレスのスキャンなし。
- `infrastructure-service-events`: 接続、リモート管理、通知、コア サービスの正確で範囲を限定した障害スナップショットを 1 件読み取ります。
- `investigate-batch FILE [FILE ...]`: 最大 12 ファイル。JSON Lines。失敗または一部のみのインポートは終了コード 3。
- `licensing-check`: プロダクト キーを含まない Windows のライセンス認証の状態、読み取り専用。JSON の証拠。
- `mac-vendor PACK_PATH MAC_ADDRESS`: 最長プレフィックスによるローカル パックの検索。ネットワーク要求なし。
- `ncsi-policy`: Windows の接続分類ポリシー、読み取り専用。プローブの接続先は除外。
- `neighbor-cache`: プライバシーに配慮して範囲を限定した近隣への到達性の証拠、読み取り専用。JSON の結果。
- `ntfs-self-healing DRIVE:`: 選択したボリュームのオンライン NTFS 修復の構成、読み取り専用。修復や設定の変更なし。
- `pagefile-check`: ページング ファイルの管理、割り当て、使用状況の概要、読み取り専用。完全なパスは除外。
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: ローカル パックの正確な検索。ネットワーク要求やドライバーの選択なし。
- `physical-disks`: 現在の正常性、動作、メディア、バス、使用状況、読み取り専用。JSON。修復なし。
- `platform-service-events`: アプリケーション、ID、プロファイル、デバイス サポートの各サービスの正確で範囲を限定した障害スナップショットを 1 件読み取ります。
- `previous-windows [--execute --backup-confirmed]`: 既定でプレビュー。DISM を使って Windows のロールバック機能を完全に削除します。
- `previous-windows-recover`: ロールバックの削除を繰り返さずに、読み取り専用で照合します。
- `provisioned-packages`: ローカルの Windows で新しいユーザー向けにステージングされたパッケージ。削除なし。
- `recipes [WORKFLOW]`: インストール済みの定義、またはワークフロー固有の依存関係と上限を一覧表示します。実行なし。
- `repair <dism|sfc> [--execute]`: 既定でプレビュー。
- `reserved-storage`: オンラインの Windows の予約済みストレージの状態、読み取り専用。JSON の証拠。
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: 既定でプレビュー。新たなチェックの後で復元ポイントを 1 つ作成します。
- `restore-point-recover`: 保留中の要求を、別のポイントを作成せずに検証します。
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: プレビューのみ。
- `shadow-copies`: 現在の VSS スナップショットの状態と属性、読み取り専用。識別情報は除外。スナップショットの変更なし。
- `shadow-storage`: VSS 差分領域の割り当てと使用率、読み取り専用。JSON。サイズ変更やスナップショットの操作なし。
- `uac-policy`: UAC の有効化と昇格プロンプトのポリシー、読み取り専用。アカウントや資格情報のデータなし。
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: ローカルの UEFI レジストリの正確な検索。ネットワーク要求やドライバーの選択なし。
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: ローカル パックの正確な検索。ネットワーク要求やドライバーの選択なし。
- `vitals-check`: Windows、ハードウェア、アカウント、リソース、バッテリー、物理ディスクのスナップショット、読み取り専用。JSON の証拠。
- `win32-code DECIMAL_OR_0xHEX`: インストール済み Windows のメッセージのオフライン検索。
- `workflows`: インストール済みのワークフローのキーとタイトルを一覧表示します。スキャンや修復なし。
