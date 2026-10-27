# 關於 KillerBench

KillerBench 是由 Steve the Killer 開發的 Windows 診斷、修復和維護工具台，屬於 [Killer Tools 系列](https://killertools.net)。1.0.0 版本尚未釋出。其桌面應用程式、Engine 和 CLI 共享診斷證據和受支援的修復工作流程。

## 可用性和範圍

目前還沒有公開的 KillerBench 下載或 Engine 軟體套件。[下載和設定](setup.html) 介紹目前的 Windows x64 目標和 Desktop Runtime 要求。實機修復驗證仍在進行中；某個工具的存在並不能證明所有故障、中斷或 Windows 設定都已經過測試。

## 原始碼和分發宣告

KillerBench 採用 **GPLv3** 授權。其分發授權為 [GNU 通用公共授權第 3 版](../license.txt)。該授權規定了複製、修改和分發受本授權約束的作品的權限和條件。

公開的 [KillerBench 倉庫](https://github.com/SteveTheKiller/KillerBench) 目前提供專案資訊，完整的實現並未在那裡公開。這些開發人員指南描述的是經授權的原始碼檢出，並不提供公開的原始碼下載。

在分發二進位檔之前，釋出者必須透過 [GPLv3 第 6 節](https://www.gnu.org/licenses/gpl-3.0.en.html#section6) 允許的方式提供對應原始碼，包括授權要求的原始碼和建置材料。首個公開軟體套件的原始碼提供說明和隨附的第三方宣告仍待定。GPLv3 不授予重新分發第三方字型的權限；目前的字型許可仍需核實。重新分發之前，請檢查已釋出軟體套件的授權、原始碼說明和宣告。

## 隱私和外部服務

診斷和記錄分析在本機執行，無需 AI 服務或 KillerBench 帳戶。記錄和保留的證據使用本機帳戶的 `%LOCALAPPDATA%\KillerBench` 資料夾。所選操作可能會連線 Windows Update、軟體下載服務、目錄服務或其他指定的目標。啟動時的更新檢查會連線 GitHub，知識更新和 MCP 安裝程式頁面會連線其已設定的服務。使用 MCP 的 AI 用戶端可能會收到傳回的參考結果。

本網站會將主題和輔色選擇儲存在瀏覽器的本機儲存中。其指南不會在你的電腦上執行診斷。原始記錄、匯入的檔案和匯出的證據可能包含電腦名稱、帳戶、路徑或敏感的源文字。共享前請先檢查；不要在公開報告中包含修復金鑰、認證或客戶資料。

## 支援

專案資訊可在公開倉庫中檢視。如需支援，請使用 [作者網站](https://thekiller.net) 上公佈的聯絡方式，並提供應用版本、Windows 內部版本、具體工具、預期結果和實際結果。傳送前請檢查證據並刪除敏感內容，客戶資訊請使用經核准的私密渠道。對於結果不確定的修復，請保留記錄。

## 相關指南

- [如何保證變更安全](safety.html)
- [報告和證據](reports.html)
- [Engine 開發人員指南](../engine/)
- [KillerMCP](../mcp.html)
