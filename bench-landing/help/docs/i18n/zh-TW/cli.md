# 命令列

`killerbench-cli` 可以從 PowerShell 或遠端主控台執行與桌面應用相同的 Engine。所有可能更改電腦的命令預設只顯示預覽，執行前需要 `--execute` 等明確的旗標。命令會將 JSON 證據輸出到標準輸出，將進度輸出到標準錯誤。

操作指南：[從指令碼執行 KillerBench](use-the-cli.html)。

結束代碼 0 表示預覽或已驗證的結果成功，1 表示啟動、輸入或記錄錯誤，2 表示無法識別引數，3 表示被阻止、失敗或未驗證，4 表示工作流程需要重新啟動後才能驗證，130 表示你已停止它。

不帶引數執行 `killerbench-cli`，即可在你自己的電腦上輸出此清單。

## 更新和維護

- `bits-job-review JOB_GUID [--all-users] [--observe]`: 確切的傳輸檢查或兩次快照觀察；不重試。
- `bits-jobs [--all-users]`: BITS 作業狀態、所有者和位元組數；不更改傳輸。
- `bits-retry JOB_GUID [--execute]`: 預設預覽；明確重試確切的作業，不聲稱應用程式已完成。
- `bits-retry-recover JOB_GUID`: 兩次唯讀快照；在不再次重試的情況下存檔穩定結果。
- `component-repair-source`: 受管理的 DISM 修復來源原則和衝突；不更改。
- `delivery-optimization`: 唯讀的傳遞最佳化服務狀態；JSON 證據。
- `delivery-optimization-activity`: 有隱私範圍限制的目前作業數和位元組總數；不含傳輸識別，不更改。
- `delivery-optimization-policy`: 唯讀的經典設定和 Policy CSP 設定；排除敏感值和提供程式識別。
- `feature-service-events`: 讀取時間、搜尋、WMI、事件記錄和音訊服務的確切故障歷史記錄。
- `feature-update-policy`: 唯讀的目標版本、延遲、暫停和安全保護原則；JSON 證據。
- `pending-file-operations`: 唯讀的、有隱私範圍限制的待處理檔案操作摘要；JSON 證據。
- `restart-check`: 唯讀的待處理重新啟動來源；JSON 證據。
- `servicing-logs`: 有範圍限制地分析最新的 CBS 記錄、純文字和壓縮的 CBS 存檔、DISM 和 Windows Setup 記錄；不修復。
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: 唯讀的維護活動快照。
- `servicing-service-events`: 唯讀的 Windows Update維護鏈確切故障歷史記錄。
- `servicing-service-states`: 唯讀的 Windows Update維護鏈六個服務的目前狀態。
- `setup-code DECIMAL_OR_0xHEX`: 有文件依據的 Windows Setup 結果代碼參考；不檢查電腦，不修復。
- `update-applicability`: 唯讀的 Windows 版本、內部版本和架構證據。
- `update-cache`: 有範圍限制的唯讀 SoftwareDistribution 快取總量；不含檔名。
- `update-code DECIMAL_OR_0xHEX`: 有文件依據的 Windows Update結果代碼參考；不檢查電腦，不修復。
- `update-context`: 唯讀的目前權限提高和桌面工作階段證據；不含帳戶識別碼。
- `update-engine-files`: 唯讀的原生 Windows Update引擎存在、版本和簽名證據。
- `update-events`: 唯讀的過去七天 Windows Update Client 歷史記錄；包含原始證據的 JSON。
- `update-followup DECIMAL_OR_0xHEX`: 根據有文件依據的 Windows Update結果代碼選擇的新的唯讀診斷。
- `update-history`: 唯讀的最新 100 個 Windows Update操作結果；JSON 證據。
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: 重複使用 `--select`；預設預覽。
- `update-policy`: 唯讀的受管理 Windows Update原則；JSON 證據。
- `update-policy-surfaces`: 唯讀的經典設定和 Policy CSP 設定面證據；排除值和提供程式識別。
- `update-recover`: 檢查待處理的更新；絕不安裝或重試。
- `update-restart-policy`: 唯讀的合規截止日期、寬限期、重新啟動抑制和通知行為。
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: JSON 發現結果和匹配項；不安裝。
- `update-service-events`: 唯讀的 Windows Update 服務確切故障歷史記錄。
- `update-service-files`: 唯讀的 Windows Update 服務確切 DLL 註冊和簽名證據。
- `update-services`: 唯讀的 Windows Update 服務註冊；JSON 證據。
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: 需要先進行新的搜尋；預設預覽；保留舊儲存。
- `update-store-reset-recover`: 核對已準備的儲存重置，而不重複執行。
- `windows-capabilities`: 唯讀的線上 Windows 功能狀態；JSON 結果。
- `windows-update-task`: 唯讀的 Scheduled Start 確切工作狀態和執行歷史記錄；不執行工作，不執行更新操作。

## BitLocker 和 TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: 預設預覽；為已加密的資料磁碟區啟用自動解鎖。
- `bitlocker-auto-unlock-recover`: 驗證中斷的自動解鎖請求，而不重複執行。
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: 預設預覽；僅建立新的復原備份檔案。
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: 驗證中斷的備份，而不再次寫入。
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: 預設預覽；開始解密，例如在使用所需方法重新加密之前。
- `bitlocker-decrypt-recover`: 驗證中斷的解密請求，而不重複執行。
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: 預設預覽；需要現有的 TPM 保護裝置和修復保護裝置。
- `bitlocker-encrypt-recover`: 驗證待處理的轉換狀態，而不重複請求。
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: 預設預覽；將現有的修復密碼備份到 Active Directory 或 Microsoft Entra ID。
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: 僅預覽選項。
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: 預設預覽；新增一個 TPM 或修復密碼保護裝置；絕不輸出密碼。
- `bitlocker-protector-recover`: 驗證中斷的保護裝置請求，而不重複執行。
- `bitlocker-protectors DRIVE:`: 僅保護裝置識別和類型；不含修復密碼。
- `bitlocker-resume DRIVE: [--execute]`: 預設預覽；僅限已完全加密且保護已暫停的確切磁碟區。
- `bitlocker-resume-recover`: 驗證待處理的保護狀態，而不重複請求。

## 安全和隱私

- `certificate-chains`: 唯讀的個人憑證本機僅快取信任鏈；不檢查撤銷，不下載。
- `defender-overview [--report]`: 唯讀的保護、掃描時間和威脅彙總概覽；不執行操作。
- `defender-protection [--report]`: 唯讀的保護首選項和已識別的原則面；排除敏感詳細資訊；不更改。
- `defender-scan Quick|Full [--execute] [--report]`: 預設預覽；一次明確的受保護 Microsoft Defender 掃描。
- `defender-scan-history [--report]`: 唯讀的快速掃描和完整掃描時間及逾期狀態；排除敏感詳細資訊；不掃描。
- `defender-scan-recover [--report]`: 唯讀核對，不啟動新的掃描。
- `defender-threats [--report]`: 威脅歷史彙總、目前活動旗標和嚴重性 ID；排除敏感詳細資訊；不執行操作。
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: 預設預覽；僅限不受管理的本機設定檔。
- `firewall-enable-recover`: 檢查待處理的更改，而不重複執行。
- `firewall-events`: 讀取有範圍限制的 Windows 防火牆警告、錯誤和嚴重操作歷史記錄。
- `firewall-logging`: 在不開啟記錄的情況下讀取網域、私人和公用流量記錄設定。
- `firewall-rules`: 讀取有隱私範圍限制的有效規則和原則來源數量。
- `firewall-service-events`: 讀取 BFE 和 Windows Defender 防火牆的確切服務控制管理器故障歷史記錄。
- `privacy-clipboard-history on|off [--execute]`: 預設預覽；在保留原則的情況下設定目前使用者的確切值。
- `privacy-clipboard-history-recover`: 重新檢查中斷的更改，而不重複執行。
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: 受保護地還原之前的值。
- `privacy-plan --select Camera --select ClipboardSync`: 僅檢視所選內容；不更改設定。
- `privacy-setting KEY on|off [--execute]`: 預設預覽；保留原則的目前使用者更改；無效的鍵會列出受支援的選項。
- `privacy-setting-recover KEY`: 重新檢查中斷的更改，而不重複執行。
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: 受保護地還原確切值。
- `security-check`: 唯讀的 TPM、安全啟動、虛擬化保護和 UAC 原則；JSON 證據。

## 服務

- `service-binaries`: 唯讀的本機服務二進位檔存在情況和 Authenticode 狀態；排除路徑，不連線非本機路徑。
- `service-check NAME [NAME ...] | service-check --all`: 唯讀的受支援服務狀態和相依性證據。
- `service-dependencies`: 唯讀的直接相依性完整性檢視；不更改服務。
- `service-dlls`: 唯讀的已註冊服務 DLL 存在情況和 Authenticode 狀態；排除路徑，不連線非本機路徑。
- `service-events`: 唯讀的服務故障歷史記錄；包含原始證據的 JSON。
- `time-events`: 唯讀的最近 Windows 時間事件歷史記錄，附 JSON 證據。
- `service-logons`: 唯讀的服務帳戶類別和桌面互動檢視；排除帳戶識別。
- `service-path-quoting`: 唯讀的服務可執行檔案路徑引號檢視；排除完整路徑和引數。
- `service-recovery-policy`: 唯讀的已安裝服務復原操作摘要；排除命令和重新啟動訊息。
- `service-startup-config`: 唯讀的啟動模式一致性檢視；不更改服務。
- `service-transitions`: 唯讀地重複觀察待處理的服務狀態；不執行服務操作。
- `service-triggers`: 唯讀的已安裝服務觸發器摘要；排除觸發器負載；不更改。

## 儲存、檔案和備份

- `backup-events`: VSS 和 volsnap 歷史記錄；JSON 證據；不更改。
- `backup-service-events`: 讀取 VSS、軟體陰影複製提供程式、Windows 備份和塊級備份引擎的確切故障歷史記錄。
- `backup-summary`: 本機 Windows 備份操作摘要；不更改備份。
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: 預設預覽；回收站要麼全部清空要麼不清空，天數不適用。
- `disk-cleanup-task`: 唯讀的確切工作狀態和執行歷史記錄；不執行工作，不清理。
- `disk-diagnostic`: 關聯確切的磁碟診斷工作和事件歷史記錄；不執行工作或磁碟操作。
- `disk-diagnostic-tasks`: 唯讀的確切收集器和解析器工作歷史記錄；不執行工作或磁碟操作。
- `disk-quotas`: 唯讀的每磁碟區配額預設值和狀態；排除使用者識別；不更改。
- `file-history-events`: 按隱私投影的檔案歷史記錄警告和錯誤數量；不更改。
- `file-history-status`: 目前帳戶的檔案歷史記錄保護狀態；不更改。
- `search-index-rebuild [--execute]`: 預設預覽；保留之前的索引並驗證新狀態。
- `search-index-rebuild-recover`: 唯讀核對，不重複修復命令。
- `storage-check DRIVE_LETTER`: 唯讀的固定 NTFS 檢查；JSON 結果和實時進度。
- `storage-events`: 唯讀的儲存歷史記錄；包含原始證據的 JSON。
- `storage-jobs`: 唯讀的 Storage 模組作業彙總狀態和進度；JSON；不更改。
- `storage-reliability`: 唯讀的溫度、磨損和 I/O 計數器；JSON；不修復。
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: 預設預覽；執行需要確認備份；系統磁碟區會安排在重新啟動時進行。
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: 僅預覽；不修復，不排程。
- `storage-repair-recover`: 驗證已完成或已排程的檔案系統修復，而不重複執行。
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: 收集並繫結目前證據；不修復，不排程。
- `storage-sense-policy`: 唯讀的已識別電腦清理原則；排除使用者設定和檔案識別。
- `storage-sense-task`: 唯讀的確切工作狀態和執行歷史記錄；不執行工作，不清理。
- `storage-service-events`: 讀取儲存服務、最佳化磁碟機、VSS 和軟體陰影複製提供程式的確切故障歷史記錄。
- `volume-capacity DRIVE:`: 唯讀的可用空間，附 JSON 證據。
- `volume-dirty DRIVE:`: 唯讀的髒位元狀態；不修復，不重新啟動。
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: 預設唯讀分析；需要執行正常的確切磁碟區和相容的介質。
- `volume-optimize-recover`: 驗證待處理的已完成操作，而不重複執行。
- `vss-providers`: 唯讀的 VSS 提供程式名稱、版本和類型；排除識別碼；不更改註冊。
- `vss-writers`: 報告的寫入器狀態；解釋英語輸出；不重置。

## 列印

- `print-events`: 唯讀的過去七天 PrintService Admin 歷史記錄；JSON 證據。
- `print-extension-files`: 唯讀的列印處理器和列印監視器二進位檔存在情況和簽名摘要；排除完整路徑。
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: 預設預覽；僅限處於 Error 或 Blocked 狀態的確切作業。
- `print-job-cancel-recover [--folder PATH]`: 重新檢查中斷的取消操作，而不重複執行。
- `print-job-list`: 列出目前可見的作業而不更改它們。
- `print-operational-events`: 唯讀的 PrintService Operational 通道狀態和可用歷史記錄；JSON 證據。
- `print-policy`: 唯讀的 Point and Print 限制和驅動程式安裝原則；JSON 證據。
- `print-rendering-policy`: 唯讀的渲染和驅動程式隔離原則；JSON 證據。
- `print-spooler-events`: 唯讀的與本機 Print Spooler 識別相關的服務控制管理器故障；JSON 證據。
- `printer-check --queue NAME [--queue NAME ...]`: 唯讀的所選本機佇列設定。
- `printer-conditions`: 唯讀的本機佇列狀況代碼；不進行實體驗證，不更改。
- `printer-driver-files`: 唯讀的已註冊檔案存在情況和可執行檔案簽名摘要；排除完整路徑。
- `printer-driver-models`: 唯讀的版本 3、版本 4 和套件感知摘要；JSON 證據。
- `printer-drivers`: 唯讀的佇列和驅動程式註冊清單及匹配；JSON 證據。
- `printer-endpoint-check`: 對確切分配的 Standard TCP/IP 印表機端點進行有範圍限制的連線；不發現，不使用 SNMP，不傳送列印資料。
- `printer-list`: 在不更改設定的情況下發現本機佇列。
- `printer-ports`: 唯讀的佇列和連接埠註冊匹配；JSON 證據。
- `printer-queue-attributes`: 唯讀的本機佇列背景列印、保留和共享屬性。
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: 預設預覽；清除離線模式可能會釋放等待中的作業。

## 網路

- `dhcp-check`: 唯讀的缺失或已過期 DHCP 租約適用性；JSON 證據，不續訂。
- `dhcp-events`: 唯讀的 IPv4 和 IPv6 DHCP Client 事件歷史記錄；JSON 結果。
- `dhcp-leases`: 唯讀的租約時間戳、到期時間和伺服器證據；JSON 結果。
- `dhcp-renew [--execute]`: 預設預覽；僅限已連線實體介面卡的確切缺失或已過期 DHCP 租約。
- `dhcp-renew-recover`: 唯讀核對，不請求新的租約。
- `dns-cache HOST [--execute]`: 預設預覽；受保護的本機快取重新整理，並驗證確切的主機。
- `dns-cache-recover`: 重新檢查中斷的快取操作，而不重複重新整理。
- `dns-cache-summary`: 有隱私範圍限制的本機 DNS 快取狀態和記錄類型計數；不更改。
- `dns-check HOST [--timeout-seconds 1..60]`: 有範圍限制的系統名稱解析；JSON 結果。
- `dns-client-config`: 唯讀的字尾、註冊、名稱回溯和原則證據；JSON 結果。
- `dns-client-events`: 唯讀的、有隱私範圍限制的 DNS Client 警告和故障歷史記錄；JSON 結果。
- `dns-nrpt-policy`: 唯讀的有效 DNS 命名空間原則證據；JSON 結果。
- `ip-addresses`: 唯讀的目前 IP 位址狀態、來源和衝突檢視；JSON 證據。
- `ip-interface-config`: 唯讀的 IP 介面設定證據；JSON 結果。
- `network-firewall`: 將活動的網路分配與相應防火牆設定檔的啟用狀態進行關聯。
- `network-offload`: 唯讀的 Windows 全域網路卸載設定；JSON 結果。
- `network-profiles`: 唯讀的目前設定檔類別和連線分類；排除網路名稱。
- `network-service-events`: 讀取 DHCP、DNS、Workstation、NLA、Network List 和 IP Helper 的確切故障歷史記錄。
- `proxy-check`: 唯讀的已儲存 WinHTTP 和目前帳戶 Internet Proxy證據。
- `route-priority`: 唯讀的有效預設路由躍點數比較；JSON 證據。
- `smb-client-config`: 唯讀的 SMB 簽名、來賓存取和傳輸設定。
- `smb-connections`: 唯讀的活動通訊協定協商；排除遠端識別。
- `smb-server-config`: 唯讀的 SMB 通訊協定、簽名、加密和多通道設定。
- `smb-shares`: 有範圍限制的唯讀 SMB 共享清單；排除路徑、權限、工作階段和帳戶。
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: 有範圍限制的連線檢查；不傳送應用程式資料。
- `tcp-port-pressure`: 唯讀的動態 TCP 範圍和本機連接埠使用匯總；排除端點和處理程序。
- `tcp-settings`: 唯讀的 Windows TCP 模板設定證據；JSON 結果。
- `vpn-profiles`: 唯讀的目前帳戶 VPN 設定檔設定；排除認證。
- `winrm-listeners`: 唯讀的 WinRM 接聽程式註冊摘要；排除憑證指紋。
- `winsock-catalog`: 唯讀的原生 Winsock 通訊協定目錄結構；JSON 結果。
- `winsock-namespaces`: 唯讀的原生 Winsock 命名空間提供程式結構；JSON 結果。
- `winsock-provider-files`: 唯讀的通訊協定提供程式檔案存在情況和簽名；JSON 結果。
- `winsock-readiness`: 唯讀的原生 Winsock 通訊協定、命名空間提供程式和提供程式檔案就緒狀態；JSON 結果。

## 應用

- `app-folder-review FOLDER_NAME [--report]`: 固定根目錄下確切資料夾的檢視；不刪除。
- `app-package-review FULL_NAME [--all-users]`: 確切的目標套件和依賴套件；不刪除。
- `app-packages [--all-users]`: 僅清點已註冊的 Appx/MSIX；不刪除。
- `browser-extensions`: 檢視 Chrome、Edge、Brave 和 Firefox 擴充套件的識別；不刪除。
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: 僅檢視；不更改系統。
- `depot-review [application and configuration selections] [--json]`: 僅檢視；不佈建。
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: 唯讀的 Authenticode 和供應商簽名者檢查；不執行任何內容。
- `foxit-inventory`: 唯讀的電腦產品清單；JSON 輸出。
- `installed-apps [--current-user]`: 解除安裝註冊和功能旗標；不刪除。
- `installed-apps-compare BEFORE.json AFTER.json`: 已儲存清單中的註冊差異；不更改電腦。
- `installed-hotfixes`: 有範圍限制的唯讀已安裝 Windows 修補程式清單；JSON 證據。
- `installer-activity`: 唯讀的 Installer 處理程序和交易標記證據；JSON 證據。
- `installer-check`: 唯讀的 Installer 原則和引擎就緒狀態；JSON 證據。
- `installer-engine-files`: 唯讀的必需 Installer 檔案存在情況和 Authenticode 摘要；JSON 證據。
- `installer-events`: 唯讀的過去七天 MSI 歷史記錄；JSON 證據。
- `installer-logging`: 唯讀的 Installer 記錄功能和除錯原則；JSON 證據。
- `installer-pending-state`: 唯讀的待處理檔案操作與 Installer 活動關聯；JSON 證據。
- `installer-registration-repair [--execute]`: 預設預覽；僅限確切的服務註冊缺陷；絕不取消註冊引擎。
- `installer-registration-repair-recover`: 重新檢查中斷的註冊，而不重複命令。
- `installer-security-policy`: 唯讀的 Installer 安全和修補程式原則；JSON 證據。
- `installer-service-events`: 唯讀的與本機 Windows Installer 識別相關的服務控制管理器故障；JSON 證據。
- `installer-service-registration`: 唯讀的 Windows Installer 服務註冊摘要；JSON 證據。
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: 新的確切適用條件預覽；不含刪除提供程式。
- `mace-package-recover`: 重新核對一條中斷的應用套件刪除記錄；絕不重複刪除。
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: 僅檢視；不刪除。
- `msi-cache`: 唯讀的已註冊 Windows Installer 快取可見性；JSON 證據。
- `msi-code NONNEGATIVE_DECIMAL`: 有文件依據的 Windows Installer 返回代碼參考；不檢查電腦，不修復。
- `msi-product {PRODUCT-CODE}`: 唯讀的確切 MSI 註冊狀態；JSON 證據。
- `msi-repair {PRODUCT-CODE} [--execute]`: 預設預覽；僅限確切的損壞或設定錯誤的產品。
- `msi-repair-recover`: 重新檢查中斷的確切產品修復，而不重複執行。
- `msi-sources`: 唯讀的、有隱私範圍限制的已註冊 Windows Installer 來源中繼資料；JSON 證據。
- `orca-cache-reset --mode New|Classic|Both [--execute]`: 預設預覽；以復原名稱保留執行帳戶的確切快取。
- `orca-cache-reset-recover`: 驗證中斷的快取保留，而不重複移動。
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: 僅檢視；不更改 Outlook。
- `startup-apps`: 檢視啟動註冊以及啟動或登入工作；不禁用。
## 系統

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: 本機確切查詢 Bluetooth SIG 分配；不發出網路請求。
- `crash-config`: 已儲存的 Windows 傾印模式和路徑；不更改。
- `crash-dumps`: 預設 Windows 傾印檔案中繼資料；不分析內容，不更改。
- `crash-events`: 最近的系統錯誤報告；不診斷，不更改。
- `crash-review [--knowledge PACK_PATH ...]`: 最多 32 個知識套件；綜合檢視當機、關機、有範圍限制的小型傾印結構和可選的確切錯誤檢查參考；不更改。
- `default-printer`: 唯讀的預設選擇和自動管理設定；JSON 證據。
- `default-printer-set QUEUE [--execute] [--folder PATH]`: 預設預覽；僅限缺失或衝突的預設印表機。
- `default-printer-set-recover [--folder PATH]`: 重新檢查中斷的選擇，而不重複執行。
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: 唯讀的裝置證據，可附帶來自最多 32 個知識套件的確切驅動程式參考。
- `device-code DECIMAL_OR_0xHEX`: 離線裝置管理器問題代碼參考。
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: 僅參考候選項。
- `event-channel-enable CHANNEL [--execute]`: 預設預覽；啟用一個確切的、不受管理的診斷通道，並保留其他設定。
- `event-channel-enable-recover`: 唯讀核對，不重複啟用命令。
- `event-channels`: 唯讀的四個診斷通道的狀態、保留、大小和飽和度。
- `font-registrations`: 有範圍限制的唯讀電腦和目前使用者註冊及檔案存在檢查。
- `memory-diagnostic-results`: 有範圍限制的唯讀 Windows 記憶體診斷歷史記錄；JSON 證據。
- `minidump-analyze PATH`: 有範圍限制的小型傾印結構中繼資料；不含記憶體頁、符號，不更改。
- `name-rename PROPOSED_NAME [--execute]`: 預設預覽；僅限獨立的工作組電腦；絕不重新啟動 Windows。
- `name-rename-recover`: 驗證待處理的重新命名，而不重複執行。
- `name-review PROPOSED_NAME`: 唯讀的識別和建議名稱檢視。
- `profiles --recover`: 驗證已記錄的不存在狀態並保留復原證據；絕不重試刪除。
- `profiles --recover-backup`: 驗證並存檔待處理的備份工件；絕不重試複製。
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: 先預覽；受保護的原生複製需要明確執行，且本機 NTFS 目標必須不存在。
- `profiles --select SID --create-backup-artifact DESTINATION`: 比較現有的已複製備份並建立其固定工件；不複製或刪除設定檔。
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: 唯讀檢視；驗證需要選擇一個 SID。
- `task-check`: 唯讀的工作清單、分類結論和 JSON 證據。
- `task-definition TASK_FOLDER TASK_NAME`: 唯讀的確切工作 XML；不可用並不表示不存在。
- `task-inventory`: 唯讀的工作識別和狀態；JSON 輸出。
- `task-journal RECORD.json [--completion COMPLETION.json]`: 歷史復原檢視；絕不授權重試。
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: 驗證並檢視工作草稿；不註冊。
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: 預設預覽；已禁用的、僅建立的 Local System 定義。
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: 唯讀的確切定義核對；絕不重試或執行工作。
- `task-xml PLAN.json YYYY-MM-DD`: 將已禁用的檢視用 XML 輸出到 stdout；僅限本機時區的行事曆排程；不註冊。
- `time-config`: 唯讀的 Windows 時間模式、對等節點和原則證據。
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: 預設預覽；自訂模式需要對等節點。
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: 僅預覽。
- `time-recover --folder PATH`: 驗證並存檔待處理記錄；絕不重試更改。
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: 唯讀；自訂模式需要對等節點。
- `wmi-provider`: 唯讀的本機作業系統提供程式查詢；JSON 證據。

## 知識、驗證和 Engine

- `knowledge PACK_PATH CONTEXT CODE`: 本機參考查詢；不執行修復。
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: 已驗證的本機啟用；不下載。
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: 驗證目前的本機知識套件；不更改。
- `knowledge-compare BEFORE_JSON AFTER_JSON`: 僅參考更改；不安裝知識套件。
- `knowledge-info PACK_PATH`: 知識套件中繼資料和每個上下文的項目數；不更改。
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: 為觀察到的代碼附加上下文參考。
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: 選擇最新的已驗證保留知識套件；不下載。
- `knowledge-search PACK_PATH CONTEXT TEXT`: 最多 50 個參考文字匹配項；不執行修復。
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: 在最多 32 個知識套件中解析一個確切識別；存在衝突時絕不選擇。
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: 驗證每個已設定的已安裝資料庫；不下載，不啟用。
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: 從已設定的 HTTPS 端點取得並原子啟用已簽名的資料庫候選項。
- `open <file.kbench> [--json]`: 重新開啟已儲存的擷取，並使用已安裝的規則重新評估。
- `scan --output <new-file.kbench> [--json]`: 儲存到新檔案的唯讀 Windows Update擷取。
- `validation-coverage RECORD.json [...]`: 彙總目前版本的原生虛擬機器證據；不診斷，不修復。
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: 唯讀的一次性虛擬機器場景計畫；不診斷，不修復。
- `validation-record RECORD.json`: 驗證一條有範圍限制的原生虛擬機器證據記錄；不診斷，不修復。
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: 目前版本的本機虛擬機器覆蓋範圍報告。
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 執行已驗證的唯讀共享選項計畫。
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: 僅清單。
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 專用診斷記錄；不修復。
- `workflow computer-name --name PROPOSED_NAME [--json]`: 僅寫入記錄的檢視。
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: 可選的確切產品狀態會包含在記錄中。
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: 可選的所選目標診斷會包含在記錄中。
- `workflow storage [--volume D:] [--json]`: 寫入記錄的唯讀檔案系統檢查。
- `workflow-journal FILE.json`: 歷史內容；結束代碼 0 表示讀取成功，而不是執行正常。
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: 顯示或驗證與主機無關的控制項；不掃描，不修復。
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: 僅歷史報告；不執行命令。

## 其他命令

- `<analyze|investigate> <text-log-path> [--json]`: 分析本機文字證據。
- `adapter-bindings`: 唯讀的核心通訊協定繫結摘要；JSON 證據。
- `adapter-statistics`: 唯讀的介面卡位元組、錯誤和丟棄計數器；JSON 證據。
- `application-events`: 唯讀的最近 Application 錯誤、分組計數和 JSON 證據。
- `applocker-policy`: 唯讀的有效收集模式和規則彙總計數；排除規則詳細資訊；不更改。
- `common-check [--scope NAME ...]`: 預設包括所有診斷區域；指定時僅限所選區域；唯讀 JSON。
- `delete-notifications`: 唯讀的 NTFS 和 ReFS 刪除通知設定；JSON；不最佳化，不更改設定。
- `diagnostic-scopes [--all]`: 列出 11 個建議的結果組，或全部 49 個唯讀區域；不收集。
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 專用診斷記錄；不修復。
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 所有診斷區域；除非明確指定 `--auto-repair`，否則不修復。
- `gateway-check`: 對已設定的確切預設下一躍點進行有範圍限制的 ICMP 檢查；不掃描位址。
- `infrastructure-service-events`: 讀取一個有範圍限制的確切連線、遠端管理、通知和核心服務故障快照。
- `investigate-batch FILE [FILE ...]`: 最多 12 個檔案；JSON Lines；匯入失敗或部分匯入時結束代碼為 3。
- `licensing-check`: 唯讀的 Windows 啟用狀態，不含產品金鑰；JSON 證據。
- `mac-vendor PACK_PATH MAC_ADDRESS`: 按最長字首進行本機知識套件查詢；不發出網路請求。
- `ncsi-policy`: 唯讀的 Windows 連線分類原則；排除探測目標。
- `neighbor-cache`: 唯讀的、有隱私範圍限制的鄰居可存取性證據；JSON 結果。
- `ntfs-self-healing DRIVE:`: 唯讀的所選磁碟區線上 NTFS 修復設定；不修復，不更改設定。
- `pagefile-check`: 唯讀的分頁檔案管理、分配和使用摘要；排除完整路徑。
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: 確切的本機知識套件查詢；不發出網路請求，不選擇驅動程式。
- `physical-disks`: 唯讀的目前執行狀況、操作、介質、匯流排和使用狀態；JSON；不修復。
- `platform-service-events`: 讀取一個有範圍限制的確切應用程式、識別、設定檔和裝置支援服務故障快照。
- `previous-windows [--execute --backup-confirmed]`: 預設預覽；透過 DISM 永久刪除 Windows 復原功能。
- `previous-windows-recover`: 唯讀核對，不重複刪除復原。
- `provisioned-packages`: 本機 Windows 中為新使用者暫存的套件；不刪除。
- `recipes [WORKFLOW]`: 列出已安裝的定義或特定工作流程的相依性和限制；不執行。
- `repair <dism|sfc> [--execute]`: 預設預覽。
- `reserved-storage`: 唯讀的線上 Windows 保留儲存狀態；JSON 證據。
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: 預設預覽；在新的檢查後建立一個還原點。
- `restore-point-recover`: 驗證待處理的請求，而不建立另一個還原點。
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: 僅預覽。
- `shadow-copies`: 唯讀的目前 VSS 快照狀態和屬性；排除識別；不更改快照。
- `shadow-storage`: 唯讀的 VSS 差異區域分配和壓力；JSON；不調整大小，不執行快照操作。
- `uac-policy`: 唯讀的 UAC 啟用狀態和提升提示原則；不含帳戶或認證資料。
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: 本機 UEFI 登錄確切查詢；不發出網路請求，不選擇驅動程式。
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: 確切的本機知識套件查詢；不發出網路請求，不選擇驅動程式。
- `vitals-check`: 唯讀的 Windows、硬體、帳戶、資源、電池和實體磁碟快照；JSON 證據。
- `win32-code DECIMAL_OR_0xHEX`: 離線查詢已安裝 Windows 的訊息。
- `workflows`: 列出已安裝工作流程的鍵和標題；不掃描，不修復。
