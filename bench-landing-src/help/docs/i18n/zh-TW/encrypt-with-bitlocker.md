# 使用 BitLocker 加密磁碟機

使用本指南可以啟用 BitLocker、備份修復金鑰並確認磁碟機已受保護。KillerBench 會在開始加密之前備份並驗證修復密碼，並且絕不會顯示或記錄修復密碼。

## 開始之前

- 以系統管理員身份執行 KillerBench。
- Windows 磁碟機需要 TPM 保護裝置和修復密碼保護裝置。資料磁碟機只需要修復密碼。
- 確定修復金鑰備份的儲存位置：位於要加密的磁碟機之外的檔案，以及選用的 Active Directory 或 Microsoft Entra ID。

## 檢查磁碟機

1. 開啟 **BitLocker**，然後在 **磁碟區** 中選擇磁碟機。
2. 將操作保持為 **檢查**，然後按 **執行診斷**。報告會顯示加密狀態、保護狀態和保護裝置識別。
3. 如果 TPM 存在但尚未就緒，請先在 **安全** 中準備它。KillerBench 絕不會清除 TPM。

## 新增所需的保護裝置

1. 開啟 **更多 BitLocker 動作**。
2. 選擇 **新增 TPM 保護裝置** 或 **新增修復密碼**，按 **預覽**，勾選 **按下「執行動作」時在所選磁碟機上進行此變更**，然後按 **執行動作**。
3. 立即備份新的修復密碼。KillerBench 不會顯示它。

## 備份修復金鑰並加密

1. 按 **尋找修復保護裝置**，然後選擇修復密碼保護裝置。
2. 輸入位於要加密的磁碟機之外的備份檔案的完整路徑。
3. 選擇 **加密**，選擇加密方法，然後勾選 **在我選擇「執行診斷」時備份所選的修復金鑰，然後開始加密**。
4. 按 **執行診斷**。KillerBench 會寫入備份、驗證備份，然後開始一次加密。
5. 加密會在背景繼續進行。稍後執行 **檢查**，確認磁碟機報告已完全加密且保護已開啟。

## 備份到 Active Directory 或 Entra ID

在 **更多 BitLocker 動作** 中，為所選保護裝置選擇目錄備份。KillerBench 會報告 Windows 已接受要求，但無法讀取目錄中的副本，因此請在 Active Directory 或 Entra 管理中心中確認。

## 資料磁碟機和自動解鎖

資料磁碟機加密後，**開啟自動解除鎖定** 可以讓它在 Windows 啟動時解鎖。Windows 磁碟機必須已完全加密且保護已開啟。

## 透過命令列

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

請參閱 [bitlocker-protectors](cli.html#cmd-bitlocker-protectors)、[bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add)、[bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) 和 [bitlocker-escrow](cli.html#cmd-bitlocker-escrow)。每個命令都有對應的 `-recover` 命令，用於處理中斷的要求。

## 相關內容

- [BitLocker 區段](bitlocker.html)
- [安全區段](security.html)
- [如何保證變更安全](safety.html)
