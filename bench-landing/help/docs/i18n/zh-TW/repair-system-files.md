# 修復系統檔案

當 Windows 元件損壞時使用本指南，例如功能無法安裝、更新報告元件錯誤或系統工具當機。KillerBench 只有在實時檢查確認損壞後才執行 DISM 和 SFC，然後驗證結果。

## 開始之前

- 以系統管理員身份執行 KillerBench。
- 先完成所有待處理的重新啟動。
- 如果電腦受管理，請檢查原則是否將 DISM 指向特定的修復來源。KillerBench 會報告該原則，但不會更改它。

## 檢查損壞

1. 開啟 **修復**，然後選擇 **系統檔案**。
2. 保持 **自動修復已確認的問題** 關閉，然後按 **執行診斷**。
3. 閱讀報告。它會清點維護套件、可選功能和功能的狀態，檢查前提條件，並掃描受保護的系統檔案。結果正常則表示不提供修復。

## 修復已確認的損壞

1. 開啟 **自動修復已確認的問題**，然後按 **執行修復**。只有當實時檢查確認損壞時，KillerBench 才會選擇 DISM 或 SFC。
2. 等待驗證。只有當後續檢查讀回已修復的狀態時，報告才會顯示 **已驗證**。
3. 如果報告指出儲存無法復原或修復來源不可用，請就此停止。重複修復不會改變結果。

中斷的 DISM 或 SFC 執行會在下次啟動時進行核對，而不會重複維護命令。

## 清理元件存放區

在 **更新** 中選擇 **元件存放區清理**。它會先詢問 Windows 是否建議清理。可選的維護會刪除被取代的元件並再次分析。它絕不會使用 ResetBase，也絕不會手動刪除 WinSxS 內容。

## 透過命令列

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

請參閱 [component-repair-source](cli.html#cmd-component-repair-source)、[repair](cli.html#cmd-repair) 和 [servicing-logs](cli.html#cmd-servicing-logs)。`repair` 預設只顯示預覽，僅在使用 `--execute` 時才會執行。

## 相關內容

- [修復 Windows Update](fix-windows-update.html)
- [修復區段](repair.html#system-files)
- [如何保證更改安全](safety.html)
