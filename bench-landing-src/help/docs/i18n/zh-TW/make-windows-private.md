# 增強 Windows 隱私

使用本指南可以檢視並加強膝上型電腦的隱私設定。KillerBench 會讀取目前設定，顯示哪些設定已由原則或裝置管理擁有，並且只逐項變更你選擇的設定，同時支援精確撤消。

## 檢視目前設定

1. 開啟 **隱私**。
2. 勾選要檢視的區域。所有區域最初都未選中。
3. 按 **執行診斷**。報告會顯示此電腦和執行 KillerBench 的帳戶的目前值，並標記由原則擁有的設定。

## 變更設定

1. 在 **隱私設定** 中選擇設定，例如相機、麥克風、位置、診斷資料、廣告、剪貼簿歷史記錄、雲內容搜尋或搜尋要點。
2. 選擇新值，然後勾選 **在我選擇「執行診斷」時套用**。
3. 按 **執行診斷**。KillerBench 會檢查原則所有者，記錄目前值，進行一項變更並讀回。

由群組原則或裝置管理擁有的設定保持不變。請在管理這些設定的地方進行變更。

## 撤消變更

每項已驗證的變更都會保留一份記錄。相應的撤消操作會在再次檢查原則後，精確復原之前的值。

## 精簡 Windows

對於建議、消費者內容、Recall 和 AI 功能、工作列和開始選單中的多餘專案、Edge 以及預裝軟體，請開啟 **維護** 並選擇 **Windows 精簡**。檢視是唯讀的，不會自動選擇任何內容。

## 透過命令列

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

請參閱 [privacy-plan](cli.html#cmd-privacy-plan)、[privacy-setting](cli.html#cmd-privacy-setting)、[privacy-setting-undo](cli.html#cmd-privacy-setting-undo) 和 [debloat-review](cli.html#cmd-debloat-review)。指定不受支援的鍵時，會列出受支援的選項。

## 相關內容

- [隱私區段](privacy.html)
- [設定新電腦](set-up-new-pc.html)
