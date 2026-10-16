# 检查和修复驱动器

当卷报告错误、Windows 要求扫描驱动器或文件无法打开时使用本指南。KillerBench 会先以只读方式检查卷，只有在你确认已备份后才会计划或运行文件系统修复。

## 开始之前

- 备份卷上的重要数据。文件系统修复可能会更改或删除损坏的文件。
- 以管理员身份运行 KillerBench。

## 检查卷

1. 打开 **存储**，然后选择 **存储**。
2. 在 **要检查的卷** 中选择驱动器，然后按 **运行诊断**。
3. 阅读报告。它会显示磁盘和卷的运行状况、脏位、NTFS 联机自我修复、控制器和磁盘设备错误、可靠性计数器，以及重置、重试和意外移除等最近的存储事件。

检查结果正常表示不提供修复。磁盘温度、磨损和未纠正的 I/O 计数器属于需要审阅的证据，它们本身不会触发修复。

## 修复文件系统

1. 按 **查看文件系统修复**，然后选择 **定点修复** 或 **离线扫描和修复**。
2. 对于数据卷，如果可以关闭所有正在使用它的程序，请勾选 **允许非系统卷离线**。Windows 卷无法离线，因此其修复会安排在下次重启时进行。
3. 勾选 **此卷上的重要数据已备份**。
4. 确认修复。KillerBench 会在执行前立即重新检查该卷，并先记录一个待处理条目。
5. 对于已计划的修复，请在方便时重启。KillerBench 会在重启后验证结果，而不会再次运行修复。

## 优化或重新修整

同一工具可以分析卷，并运行 Windows 建议的优化、**重新对固态存储进行修整** 或硬盘碎片整理。每项都是针对一个运行正常的确切卷单独确认的操作。

## 通过命令行

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

请参阅 [storage-check](cli.html#cmd-storage-check)、[storage-repair-plan](cli.html#cmd-storage-repair-plan)、[storage-repair](cli.html#cmd-storage-repair) 和 [volume-optimize](cli.html#cmd-volume-optimize)。

## 相关内容

- [存储分区](storage.html)
- [释放磁盘空间](free-disk-space.html)
- [疑难解答](troubleshooting.html)
