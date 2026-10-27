# 修复 Windows 更新

当更新失败、停滞或反复重新安装时使用本指南。KillerBench 会先读取更新链，用其背后的证据解释故障，并且只有在新证据准确证明该问题时，才会启动服务或修复组件存储。

## 开始之前

- 以管理员身份运行 KillerBench。更新服务、组件存储和受保护的系统文件需要提升权限才能获得完整证据。
- 如果 Windows 已有待处理的重启，请先重启。在重启状态清除之前，许多更新修复都会被阻止。
- 执行维护工作时，请让计算机连接交流电源。

## 找出更新失败的原因

1. 打开 **更新**，并在工具列表中选择 **Windows 更新**。
2. 第一次运行时保持 **自动修复已确认的问题** 关闭，然后按 **运行诊断**。
3. 阅读 **可读报告**。它会列出已停止的更新服务、待处理的重启、可用空间不足、组件存储损坏以及 Windows 记录的确切错误代码，并给出建议的下一步。
4. 如果报告中出现错误代码，相应的后续检查会自动运行。你也可以使用 `killerbench-cli update-code 0x80070002` 自行查询。

## 监视看似停滞的更新

在 **更新** 中选择 **维护活动监视器**，然后按 **运行诊断**。它会反复拍摄更新、Setup 和 Installer 活动、传输总量以及重启指示的快照。活动仅作为证据显示，它本身绝不会断定存在停滞或故障。

## 修复报告发现的问题

1. 开启 **自动修复已确认的问题**，再次运行 **Windows 更新**。它可以启动已确认停止的已启用更新服务，并且仅在实时检查确认损坏时才运行 DISM 或 SFC。
2. 对于单个服务，请改用其专用工具，例如 **BITS 服务**、**加密服务**、**Windows 模块安装程序** 或 **更新协调器服务**。每个工具只针对该服务提供恢复。
3. 当报告要求重启时，请先重启，再运行其他任何内容。KillerBench 会记录需要重启的结果，并将后续工作推迟到重启完成之后。

每项修复都会记录到日志中并读回。完整流程请参阅 [如何保证更改安全](safety.html)。

## 安装特定更新

1. 在 **更新** 中选择 **可用更新**，然后按 **运行诊断**，从已配置的来源查找更新。
2. 选择所需的更新，然后按 **预览所选内容**。预览会准确列出将要安装的内容以及是否需要重启。
3. 阅读预览，勾选确认复选框，然后按 **安装**。每次运行后复选框都会被清除，KillerBench 绝不会替你选择或批准更新。

## 通过命令行

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

请参阅 [update-search](cli.html#cmd-update-search)、[update-history](cli.html#cmd-update-history)、[servicing-monitor](cli.html#cmd-servicing-monitor) 和 [update-plan](cli.html#cmd-update-plan)。如果安装被中断，请运行 [update-recover](cli.html#cmd-update-recover)；它会检查结果，并且绝不会重试更新。

## 相关内容

- [更新分区](updates.html)
- [修复系统文件](repair-system-files.html)
- [疑难解答](troubleshooting.html)
