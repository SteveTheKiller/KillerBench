# 檢查和修復磁碟機

當磁碟區報告錯誤、Windows 要求掃描磁碟機或檔案無法開啟時使用本指南。KillerBench 會先以唯讀方式檢查磁碟區，只有在你確認已備份後才會排程或執行檔案系統修復。

## 開始之前

- 備份磁碟區上的重要資料。檔案系統修復可能會更改或刪除損壞的檔案。
- 以系統管理員身份執行 KillerBench。

## 檢查磁碟區

1. 開啟 **儲存**，然後選擇 **儲存**。
2. 在 **要檢查的磁碟區** 中選擇磁碟機，然後按 **執行診斷**。
3. 閱讀報告。它會顯示磁碟和磁碟區的執行狀況、髒位元、NTFS 線上自我修復、控制器和磁碟裝置錯誤、可靠性計數器，以及重置、重試和意外移除等最近的儲存事件。

檢查結果正常表示不提供修復。磁碟溫度、磨損和未糾正的 I/O 計數器屬於需要審閱的證據，它們本身不會觸發修復。

## 修復檔案系統

1. 按 **檢視檔案系統修復**，然後選擇 **定點修復** 或 **離線掃描與修復**。
2. 對於資料磁碟區，如果可以關閉所有正在使用它的程式，請勾選 **允許非系統磁碟區離線**。Windows 磁碟區無法離線，因此其修復會安排在下次重新啟動時進行。
3. 勾選 **此磁碟區上的重要資料已備份**。
4. 確認修復。KillerBench 會在執行前立即重新檢查該磁碟區，並先記錄一個待處理項目。
5. 對於已排程的修復，請在方便時重新啟動。KillerBench 會在重新啟動後驗證結果，而不會再次執行修復。

## 最佳化或重新修整

同一工具可以分析磁碟區，並執行 Windows 建議的最佳化、**重新修剪固態儲存裝置** 或硬碟磁碟重組。每項都是針對一個執行正常的確切磁碟區單獨確認的操作。

## 透過命令列

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

請參閱 [storage-check](cli.html#cmd-storage-check)、[storage-repair-plan](cli.html#cmd-storage-repair-plan)、[storage-repair](cli.html#cmd-storage-repair) 和 [volume-optimize](cli.html#cmd-volume-optimize)。

## 相關內容

- [儲存區段](storage.html)
- [釋放磁碟空間](free-disk-space.html)
- [疑難解答](troubleshooting.html)
