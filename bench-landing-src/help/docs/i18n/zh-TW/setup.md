# 下載和設定

KillerBench 1.0.0 尚未釋出。目前還沒有公開的 KillerBench 軟體套件或下載。本頁介紹目前的軟體套件要求，以便你在不下載非官方副本的情況下做好準備。

## Windows 和執行階段

- 目前的釋出目標是 **Windows x64**。Windows 11 x64 是目前實機驗證的基準。不提供原生 ARM64、x86 和 Windows Server 發行版。
- KillerBench 有兩種打包方式。安裝程式 **依賴框架**，需要 **.NET Desktop Runtime 10 for Windows x64**。普通的 .NET Runtime、ASP.NET Core Runtime 和 .NET Framework 不提供 WPF。可攜式版本自帶執行階段，無需單獨安裝。命令列工具 `killerbench-cli.exe` 需要 .NET 10 Runtime for Windows x64。SDK 僅用於開發，日常使用不需要。
- 請使用 [.NET 10 on Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows) 支援的 Windows 版本。執行階段受支援並不表示 KillerBench 的修復已在每個受支援的版本或內部版本上經過驗證。Windows 10 的支援仍待定，其受支援的版本受 Microsoft 目前生命週期要求的限制。
- 請僅從 [Microsoft 的 .NET 10 下載頁面](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) 取得 Desktop Runtime，並選擇 **.NET Desktop Runtime**、**Windows**、**x64**。請遵守所在組織的軟體安裝規定。

如果 `dotnet` 可用，`dotnet --list-runtimes` 應列出 x64 安裝的 `Microsoft.WindowsDesktop.App 10.0.x`。僅為其他架構安裝的執行階段不滿足 x64 安裝程式軟體套件的要求。

## 軟體套件釋出後

1. 請使用本網站連結的官方下載。目前還沒有可用的 KillerBench 下載連結。
2. 閱讀該軟體套件的要求、[GPLv3 授權](../license.txt)、對應原始碼的取得說明、隨附的第三方宣告以及簽名和校驗和資訊。僅憑檔名無法證明真實性。
3. 為目前帳戶執行安裝程式，或在提高權限提示後為所有使用者執行安裝程式。它會新增開始選單捷徑、選用的桌面捷徑以及「新增或刪除程式」項目。可攜式版本無需安裝即可執行。
4. 以普通帳戶啟動 KillerBench，並按照 [首次診斷](first-run.html) 操作。診斷時的存取失敗會保持可見；提高權限是另一項有意的選擇。

不要為了執行未經驗證的副本而繞過 Windows 安全警告。在選擇任何會變更電腦的操作之前，請保留最新的備份。

啟動時，KillerBench 會在 GitHub 上檢查是否有更新的穩定版本，並在安裝前徵求你的同意。下載內容必須與公佈的 SHA-256 校驗和匹配。可以在「關於」中透過 **啟動時檢查更新** 關閉此檢查。

## 目前限制

目前版本包含十二個區段、唯讀診斷、報告以及單獨選擇的修復功能。實機修復驗證仍在進行中。介面能夠執行、建置成功或測試資料的結果都不能證明在真實電腦上的修復行為。請參閱 [如何保證變更安全](safety.html#development-status)。

產品首頁上的示意主控台是合成輸出。首個公開軟體套件仍在準備中。

## 原始碼、授權和支援

KillerBench 採用 GPLv3 授權。首個公開軟體套件的原始碼提供說明和第三方字型許可仍待定。有關原始碼可用性、分發宣告、證據隱私和支援資訊，請閱讀 [關於 KillerBench](about.html)。[Engine 指南](../engine/) 面向擁有授權原始碼檢出的開發人員，並不表示已有公開的 Engine 軟體套件。
