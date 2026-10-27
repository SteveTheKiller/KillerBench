# 解決列印問題

當作業一直停留在佇列中、印表機顯示離線或預設印表機不正確時使用本指南。KillerBench 會先讀取列印多工緩衝處理器、佇列、作業、驅動程式和連接埠，然後讓你修復某一個確切的佇列或作業。

## 檢查印表機

1. 開啟 **裝置**，然後選擇 **列印**。
2. 按 **尋找本機印表機**，然後選擇出現問題的佇列。若要檢查所有佇列，請選擇 **使用一般列印檢查**。
3. 保持 **自動修復已確認的問題** 關閉，然後按 **執行診斷**。
4. 閱讀報告。它會顯示 Print Spooler 狀態、已暫停的佇列、離線模式、可見的作業、驅動程式狀況、連接埠註冊、最近的 PrintService 錯誤，對於網路印表機，還會顯示對印表機分配位址的一次連線嘗試。

連線成功或狀態代碼正常並不能證明印表機有紙、有墨粉或機械正常。

## 修復佇列

1. 開啟 **自動修復已確認的問題**，再次執行 **列印**。它可以啟動已啟用的列印多工緩衝處理器，復原有作業等待的確切已暫停佇列，並清除離線模式。
2. 清除離線模式可能會釋放等待中的作業，因此請確認這些作業仍需列印。
3. 有人故意暫停且沒有作業等待的佇列會保持暫停狀態。

## 清除一個卡住的作業

1. 按 **尋找遭封鎖的列印工作**。
2. 選擇顯示 Error 或 Blocked 的作業，然後勾選 **僅取消此所選作業**。
3. 按 **執行診斷**。只會取消該作業，並讀回結果。

## 修復預設印表機

選擇應作為預設印表機的佇列，勾選 **使用此佇列修復遺失或衝突的預設印表機**，然後按 **執行診斷**。此操作僅在預設印表機缺失或衝突時執行。

## 透過命令列

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

請參閱 [printer-list](cli.html#cmd-printer-list)、[printer-check](cli.html#cmd-printer-check)、[printer-repair](cli.html#cmd-printer-repair)、[print-job-cancel](cli.html#cmd-print-job-cancel) 和 [default-printer-set](cli.html#cmd-default-printer-set)。

## 相關內容

- [裝置區段](devices.html#printing)
- [解決網路和 Internet 問題](fix-network.html)
