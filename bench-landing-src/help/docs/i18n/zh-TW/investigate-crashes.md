# 調查當機和藍色畫面

當 Windows 意外重新啟動、顯示停止錯誤或某個應用程式反覆當機時使用本指南。KillerBench 會將當機、關機、硬體錯誤和可靠性證據彙總到一份報告中。它只描述記錄的內容，絕不猜測根本原因。

## 檢查電腦

1. 開啟 **診斷**，然後選擇 **關鍵指標**。
2. 按 **執行診斷**。報告會彙總記憶體、磁碟執行狀況、裝置問題、可靠性監視器歷史記錄、硬體錯誤記錄、當機報告、意外關機和 Windows 記憶體診斷結果。
3. 閱讀結論及其下方的證據。過去的當機可以解釋發生了什麼，但不能證明故障仍然存在。

## 縮小範圍

- **硬體錯誤歷史記錄** 會結合裝置問題和磁碟執行狀況，讀取 WHEA 和記憶體診斷記錄。
- **可靠性歷史記錄** 會彙總過去 14 天的應用程式當機、無回應和意外關機。
- **應用程式故障** 專注於當機的程式。
- **驅動程式安裝歷史記錄** 顯示與目前存在的裝置相匹配的失敗驅動程式安裝。
- **裝置問題** 列出帶有裝置管理員錯誤碼的裝置。

## 讀取當機傾印

KillerBench 會列出傾印設定和傾印檔案的中繼資料，並可以分析小型傾印的結構，以取得異常和模組名稱。桌面應用程式中絕不會開啟傾印內容。載入錯誤檢查知識套件後，停止代碼會解析為其文件中說明的含義。

在調查完成之前，請保留傾印。**清理** 可以刪除它們，但這樣也會丟棄證據。

## 透過命令列

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

請參閱 [vitals-check](cli.html#cmd-vitals-check)、[crash-review](cli.html#cmd-crash-review) 和 [device-code](cli.html#cmd-device-code)。

## 相關內容

- [診斷區段](diagnostics.html#vitals)
- [報告和證據](reports.html)
