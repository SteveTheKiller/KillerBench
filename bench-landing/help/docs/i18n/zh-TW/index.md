# KillerBench 幫助

KillerBench 是面向技術人員的 Windows 診斷和修復工作台。它會先檢查電腦，並用每項結論背後的證據解釋所發現的問題；只有在你選擇具體的修復並確認後，才會更改 Windows。

KillerBench 正在開發中，目前還沒有公開下載。這些頁面介紹的是目前版本。

## 功能

- 對 Windows Update、裝置、儲存、網路、列印、安全、應用和設定檔執行唯讀診斷，然後產生可讀報告。
- 提供受保護的修復，僅在新證據準確證明問題時才執行，並在之後驗證結果。
- 涵蓋 BitLocker、隱私設定、Windows 精簡和新電腦準備等設定與加固工作。
- 診斷和報告分析在本機執行，無需 AI 或 KillerBench 帳戶。所選操作（例如下載和將修復金鑰備份到 Entra ID）可能會連線外部服務。
- 主控台、報告、確認框以及「開啟」「儲存」和「資料夾」對話方塊都使用應用的目前佈景主題。

## 從哪裡開始

- [下載和設定](setup.html)
- [關於 KillerBench](about.html)

- [首次診斷](first-run.html) 逐步介紹完整系統掃描。
- [工作區](workspace.html) 介紹區段、窗格和快速鍵。
- [如何保證更改安全](safety.html) 說明 KillerBench 在任何更改前後會檢查什麼。
- [操作指南](fix-windows-update.html) 逐步介紹常見工作。
- **區段** 頁面按照與應用相同的分組介紹每個工具。
- [命令列](cli.html) 列出所有 `killerbench-cli` 命令。
- [疑難解答](troubleshooting.html) 介紹被阻止的操作、重新啟動和中斷的請求。

希望將診斷功能嵌入自己工具的開發人員可以閱讀 [Engine 開發人員指南](../engine/index.html)。
