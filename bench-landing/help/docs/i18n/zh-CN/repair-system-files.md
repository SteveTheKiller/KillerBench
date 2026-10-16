# 修复系统文件

当 Windows 组件损坏时使用本指南，例如功能无法安装、更新报告组件错误或系统工具崩溃。KillerBench 只有在实时检查确认损坏后才运行 DISM 和 SFC，然后验证结果。

## 开始之前

- 以管理员身份运行 KillerBench。
- 先完成所有待处理的重启。
- 如果计算机受管理，请检查策略是否将 DISM 指向特定的修复来源。KillerBench 会报告该策略，但不会更改它。

## 检查损坏

1. 打开 **修复**，然后选择 **系统文件**。
2. 保持 **自动修复已确认的问题** 关闭，然后按 **运行诊断**。
3. 阅读报告。它会清点维护包、可选功能和功能的状态，检查前提条件，并扫描受保护的系统文件。结果正常则表示不提供修复。

## 修复已确认的损坏

1. 开启 **自动修复已确认的问题**，然后按 **运行修复**。只有当实时检查确认损坏时，KillerBench 才会选择 DISM 或 SFC。
2. 等待验证。只有当后续检查读回已修复的状态时，报告才会显示 **已验证**。
3. 如果报告指出存储无法恢复或修复来源不可用，请就此停止。重复修复不会改变结果。

中断的 DISM 或 SFC 运行会在下次启动时进行核对，而不会重复维护命令。

## 清理组件存储

在 **更新** 中选择 **组件存储清理**。它会先询问 Windows 是否建议清理。可选的维护会删除被取代的组件并再次分析。它绝不会使用 ResetBase，也绝不会手动删除 WinSxS 内容。

## 通过命令行

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

请参阅 [component-repair-source](cli.html#cmd-component-repair-source)、[repair](cli.html#cmd-repair) 和 [servicing-logs](cli.html#cmd-servicing-logs)。`repair` 默认只显示预览，仅在使用 `--execute` 时才会执行。

## 相关内容

- [修复 Windows 更新](fix-windows-update.html)
- [修复分区](repair.html#system-files)
- [如何保证更改安全](safety.html)
