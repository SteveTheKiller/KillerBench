# 從指令碼執行 KillerBench

當你需要在指令碼、RMM 作業或遠端主控台中取得 KillerBench 證據時，請使用命令列。`killerbench-cli` 執行與桌面應用程式相同的 Engine，並輸出 JSON。

## 入門

1. 在 `killerbench-cli.exe` 所在位置開啟 PowerShell，或將其資料夾新增至路徑中。
2. 執行 `killerbench-cli help`，列出所有命令及其安全邊界。
3. 執行 `killerbench-cli diagnostic-scopes`，檢視 11 個建議的診斷群組。

## 執行掃描並儲存報告

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` 僅用於診斷，並會拒絕 `--auto-repair`。需要執行特定操作時，請使用專門的修復工作流程。`--report` 會寫入與應用程式匯出的報告相同的主題樣式報告。使用 `--scope` 可以只檢查部分區域，使用 `common-check` 可以在沒有工作流程記錄的情況下取得合併的 JSON。

## 了解結束代碼

| 代碼 | 含義 |
| --- | --- |
| 0 | 預覽或已驗證的結果成功。 |
| 1 | 啟動、輸入或記錄錯誤導致命令停止。 |
| 2 | 無法識別引數。 |
| 3 | 被阻止、失敗或未驗證。 |
| 4 | 工作流程需要先重新啟動才能驗證。 |
| 130 | 你停止了它。 |

唯讀命令的結束代碼 0 表示命令已執行，而不是電腦執行正常。請閱讀 JSON 了解結論。

## 進行變更

所有可能變更電腦的命令預設都只顯示預覽。請閱讀預覽，然後使用 `--execute` 執行同一命令。如果要求被中斷，請執行相應的 `-recover` 命令；它會報告實際結果，並且絕不會重複變更。

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## 重新開啟以前的結果

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## 從 AI 助手使用

[KillerMCP](../mcp.html) 可以呼叫已安裝的 CLI 來查詢裝置管理員和 Windows 錯誤碼。

## 相關內容

- [命令列參考](cli.html)
- [Engine 開發者指南](../engine/index.html)
- [疑難解答](troubleshooting.html)
