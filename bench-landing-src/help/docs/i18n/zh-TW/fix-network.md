# 解決網路和 Internet 問題

當電腦無法上網、無法存取伺服器或名稱，或者連線頻繁斷開時使用本指南。KillerBench 會依次檢查介面卡、位址、閘道、DNS 和防火牆，並且只有在新證據證明確有必要時，才會續訂租約或啟動服務。

## 檢查連線

1. 開啟 **網路**，然後選擇 **網路**。
2. 要測試特定目標，請填寫 **主機名稱查詢（選用）**，或者 **TCP 目的地（選用）** 和 **TCP 連接埠**。若只檢查本機設定，請留空。不會傳送任何應用程式資料。
3. 如果網路較慢，請設定 **每次查詢或連線的逾時（秒）**。
4. 保持 **自動修復已確認的問題** 關閉，然後按 **執行診斷**。
5. 閱讀報告。它會區分缺少位址、缺少閘道或閘道不可存取、DNS 設定問題、Proxy 設定、防火牆設定檔不匹配以及查詢或連線失敗，並為每種情況給出下一步。

閘道有回應並不能證明可以存取 Internet；透過已設定的解析程式查詢失敗，也不代表該網站已停止執行。

## 修復報告發現的問題

1. 開啟 **自動修復已確認的問題**，再次執行 **網路**。它可以啟動已啟用的 DNS Client、DHCP Client、SMB 用戶端、Network Location Awareness、Network List 和 IP Helper 服務，並為缺少可用 IPv4 位址的已連線實體 DHCP 介面卡續訂租約。
2. 續訂期間連線可能會短暫斷開。靜態位址和虛擬介面卡永遠不會被變更。
3. 對於快取中某個錯誤的名稱，請使用針對該確切主機的 DNS 快取重新整理，而不是清除所有內容。

路由、Hosts 項目、DNS 伺服器、防火牆規則、Winsock 提供者和網路設定檔始終保持不變。

## 無線、有線登入和Proxy

- **無線網路** 會檢查介面卡和 WLAN AutoConfig，並在服務停止時啟動它。
- **有線身份驗證 (802.1X)** 會針對需要在連接埠登入的網路，對 Wired AutoConfig 執行相同的操作。
- **WinHTTP 自動 Proxy** 會顯示電腦和目前帳戶已儲存的Proxy 設定。KillerBench 在診斷期間絕不會取得 PAC URL 或變更Proxy 設定。

## 透過命令列

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

請參閱 [workflow](cli.html#cmd-workflow)、[dns-check](cli.html#cmd-dns-check)、[tcp-check](cli.html#cmd-tcp-check)、[dhcp-renew](cli.html#cmd-dhcp-renew) 和 [dns-cache](cli.html#cmd-dns-cache)。

## 相關內容

- [網路區段](network.html)
- [解決列印問題](fix-printing.html)
- [如何保證變更安全](safety.html)
