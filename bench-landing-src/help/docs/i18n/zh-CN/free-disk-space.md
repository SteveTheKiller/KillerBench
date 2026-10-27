# 释放磁盘空间

当驱动器空间不足时使用本指南。KillerBench 会显示空间的去向，预览每个清理目标将删除的内容，并且只有在你开启永久清理后才会删除。

## 查看剩余空间

1. 打开 **存储**，然后选择 **存储**。
2. 按 **运行诊断**。报告会标记每个可用空间低于 10% 的带驱动器号的卷，并显示卷影副本分配、配额默认值和存储感知策略。
3. 对于崩溃转储和旧的更新内容，还请运行 **诊断** 中的 **关键指标** 和 **更新** 中的 **Windows 更新**；更新缓存的大小会显示在那里。

## 预览清理

1. 打开 **维护**，然后选择 **清理**。
2. 勾选所需的目标，例如 **当前用户临时文件**、**Windows 临时文件**、**当前用户浏览器缓存**、**传递优化缓存** 或 **当前用户回收站**。将指针悬停在目标上可以准确查看其涵盖的文件夹。
3. 设置 **最小天数** 和 **每个目标的最大文件数**。
4. 在 **在重新检查后执行所选的永久清理** 关闭的情况下按 **运行诊断**。报告会列出每个目标的候选文件和预计空间。不会删除任何内容。

## 删除文件

1. 开启 **在重新检查后执行所选的永久清理**，然后按 **运行诊断**。
2. KillerBench 会在删除前重新检查每个候选文件，跳过最近的、受保护的或正在使用的文件，并将每个结果记录到日志中。

永久清理会绕过回收站，且无法撤消。删除错误报告和崩溃转储会丢弃你调查崩溃时可能需要的证据，因此请先检查这些内容。

## 删除先前的 Windows 安装

功能更新之后，**清理** 中的 **先前的 Windows 安装** 可以通过 Windows 支持的回滚删除请求删除回滚副本。它会单独运行，需要勾选 **我已备份前一次安装中需要的文件和设置**，并且会永久删除回退到早期版本的能力。

## 通过命令行

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

请参阅 [volume-capacity](cli.html#cmd-volume-capacity)、[cleanup](cli.html#cmd-cleanup) 和 [previous-windows](cli.html#cmd-previous-windows)。

## 相关内容

- [维护分区](maintenance.html#cleanup)
- [存储分区](storage.html)
- [检查和修复驱动器](check-a-drive.html)
