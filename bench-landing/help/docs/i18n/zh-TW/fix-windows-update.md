# 修復 Windows Update

當更新失敗、停滯或反覆重新安裝時使用本指南。KillerBench 會先讀取更新鏈，用其背後的證據解釋故障，並且只有在新證據準確證明該問題時，才會啟動服務或修復元件存放區。

## 開始之前

- 以系統管理員身份執行 KillerBench。更新服務、元件存放區和受保護的系統檔案需要提高權限才能獲得完整證據。
- 如果 Windows 已有待處理的重新啟動，請先重新啟動。在重新啟動狀態清除之前，許多更新修復都會被阻止。
- 執行維護工作時，請讓電腦連線交流電源。

## 找出更新失敗的原因

1. 開啟 **更新**，並在工具列表中選擇 **Windows Update**。
2. 第一次執行時保持 **自動修復已確認的問題** 關閉，然後按 **執行診斷**。
3. 閱讀 **可讀報告**。它會列出已停止的更新服務、待處理的重新啟動、可用空間不足、元件存放區損壞以及 Windows 記錄的確切錯誤碼，並給出建議的下一步。
4. 如果報告中出現錯誤碼，相應的後續檢查會自動執行。你也可以使用 `killerbench-cli update-code 0x80070002` 自行查詢。

## 監視看似停滯的更新

在 **更新** 中選擇 **維護活動監視器**，然後按 **執行診斷**。它會反覆拍攝更新、Setup 和 Installer 活動、傳輸總量以及重新啟動指示的快照。活動僅作為證據顯示，它本身絕不會斷定存在停滯或故障。

## 修復報告發現的問題

1. 開啟 **自動修復已確認的問題**，再次執行 **Windows Update**。它可以啟動已確認停止的已啟用更新服務，並且僅在即時檢查確認損壞時才執行 DISM 或 SFC。
2. 對於單個服務，請改用其專用工具，例如 **BITS 服務**、**加密服務**、**Windows 模組安裝程式** 或 **Update Orchestrator 服務**。每個工具只針對該服務提供復原。
3. 當報告要求重新啟動時，請先重新啟動，再執行其他任何內容。KillerBench 會記錄需要重新啟動的結果，並將後續工作推遲到重新啟動完成之後。

每項修復都會寫入記錄並讀回。完整流程請參閱 [如何保證變更安全](safety.html)。

## 安裝特定更新

1. 在 **更新** 中選擇 **可用更新**，然後按 **執行診斷**，從已設定的來源查詢更新。
2. 選擇所需的更新，然後按 **預覽已選擇項目**。預覽會準確列出將要安裝的內容以及是否需要重新啟動。
3. 閱讀預覽，勾選確認核取方塊，然後按 **安裝**。每次執行後核取方塊都會被清除，KillerBench 絕不會替你選擇或核准更新。

## 透過命令列

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

請參閱 [update-search](cli.html#cmd-update-search)、[update-history](cli.html#cmd-update-history)、[servicing-monitor](cli.html#cmd-servicing-monitor) 和 [update-plan](cli.html#cmd-update-plan)。如果安裝被中斷，請執行 [update-recover](cli.html#cmd-update-recover)；它會檢查結果，並且絕不會重試更新。

## 相關內容

- [更新區段](updates.html)
- [修復系統檔案](repair-system-files.html)
- [疑難解答](troubleshooting.html)
