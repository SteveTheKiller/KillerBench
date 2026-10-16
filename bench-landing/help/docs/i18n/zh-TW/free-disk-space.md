# 釋放磁碟空間

當磁碟機空間不足時使用本指南。KillerBench 會顯示空間的去向，預覽每個清理目標將刪除的內容，並且只有在你開啟永久清理後才會刪除。

## 檢視剩餘空間

1. 開啟 **儲存**，然後選擇 **儲存**。
2. 按 **執行診斷**。報告會標記每個可用空間低於 10% 的有磁碟機代號的磁碟區，並顯示陰影複製分配、配額預設值和儲存感知原則。
3. 對於當機傾印和舊的更新內容，還請執行 **診斷** 中的 **關鍵指標** 和 **更新** 中的 **Windows Update**；更新快取的大小會顯示在那裡。

## 預覽清理

1. 開啟 **維護**，然後選擇 **清理**。
2. 勾選所需的目標，例如 **目前使用者暫存檔案**、**Windows 暫存檔案**、**目前使用者瀏覽器快取**、**傳遞最佳化快取** 或 **目前使用者資源回收筒**。將指標懸停在目標上可以準確檢視其涵蓋的資料夾。
3. 設定 **最少天數** 和 **每個目標的最大檔案數**。
4. 在 **在新的檢查後執行所選的永久清理** 關閉的情況下按 **執行診斷**。報告會列出每個目標的候選檔案和預計空間。不會刪除任何內容。

## 刪除檔案

1. 開啟 **在新的檢查後執行所選的永久清理**，然後按 **執行診斷**。
2. KillerBench 會在刪除前重新檢查每個候選檔案，跳過最近的、受保護的或正在使用的檔案，並將每個結果寫入記錄。

永久清理會繞過回收站，且無法撤消。刪除錯誤報告和當機傾印會丟棄你調查當機時可能需要的證據，因此請先檢查這些內容。

## 刪除先前的 Windows 安裝

功能更新之後，**清理** 中的 **先前的 Windows 安裝** 可以透過 Windows 支援的復原刪除請求刪除復原副本。它會單獨執行，需要勾選 **我已備份任何需要從先前安裝中取得的檔案和設定**，並且會永久刪除回退到早期版本的能力。

## 透過命令列

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

請參閱 [volume-capacity](cli.html#cmd-volume-capacity)、[cleanup](cli.html#cmd-cleanup) 和 [previous-windows](cli.html#cmd-previous-windows)。

## 相關內容

- [維護區段](maintenance.html#cleanup)
- [儲存區段](storage.html)
- [檢查和修復磁碟機](check-a-drive.html)
