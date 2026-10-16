# 設定新電腦

準備新的或剛重置的電腦時使用本指南。KillerBench 會檢視電腦、你計畫安裝的應用程式以及你想為它設定的名稱，然後指向執行每項更改的受保護工具。

## 從基準開始

1. 從 **診斷** 執行 **完整系統掃描**，並保留報告作為電腦初始狀態的記錄。
2. 安裝待處理的更新。如果有任何更新失敗，請參閱 [修復 Windows Update](fix-windows-update.html)。

## 檢視應用程式和設定

1. 開啟 **電腦準備**，然後選擇 **電腦準備**。
2. 選擇計畫設定的應用程式和設定組，然後按 **執行診斷**。檢視會將它們與已安裝的內容進行比較。KillerBench 不會下載或執行安裝程式。
3. 執行供應商的安裝程式之前，請先透過命令列進行驗證。只有當安裝程式帶有該供應商確切組織名稱的有效簽名時，KillerBench 才會信任它。

## 為電腦命名

1. 選擇 **電腦名稱**，輸入名稱，然後按 **執行診斷**，根據目前識別和域角色檢視該名稱。
2. 在工作組電腦上，勾選 **在完成新檢查後重新命名這部工作群組電腦** 以進行重新命名。需要重新啟動才能完成。

## 保護和整理

- [使用 BitLocker 加密磁碟機](encrypt-with-bitlocker.html) 並備份修復金鑰。
- [增強 Windows 隱私](make-windows-private.html)，並檢視 **維護** 中的 **Windows 精簡**。
- 一切就緒後，使用 **準備一個還原點** 建立還原點。

## 透過命令列

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

請參閱 [workflow](cli.html#cmd-workflow) 和 [restore-point](cli.html#cmd-restore-point)。

## 相關內容

- [電腦準備區段](onboarding.html)
- [首次診斷](first-run.html)
