# KillerBench 帮助

KillerBench 是面向技术人员的 Windows 诊断和修复工具台。它会先检查计算机，并用每项结论背后的证据解释所发现的问题；只有在你选择具体的修复并确认后，才会更改 Windows。

KillerBench 正在开发中，目前还没有公开下载。这些页面介绍的是当前版本。

## 功能

- 对 Windows 更新、设备、存储、网络、打印、安全、应用和配置文件运行只读诊断，然后生成可读报告。
- 提供受保护的修复，仅在新证据准确证明问题时才运行，并在之后验证结果。
- 涵盖 BitLocker、隐私设置、Windows 精简和新计算机准备等设置与加固工作。
- 诊断和报告分析在本地运行，无需 AI 或 KillerBench 账户。所选操作（例如下载和将恢复密钥备份到 Entra ID）可能会连接外部服务。
- 控制台、报告、确认框以及“打开”“保存”和“文件夹”对话框都使用应用的当前主题。

## 从哪里开始

- [下载和设置](setup.html)
- [关于 KillerBench](about.html)

- [首次诊断](first-run.html) 逐步介绍完整系统扫描。
- [工作区](workspace.html) 介绍分区、窗格和快捷键。
- [如何保证更改安全](safety.html) 说明 KillerBench 在任何更改前后会检查什么。
- [操作指南](fix-windows-update.html) 逐步介绍常见工作。
- **分区** 页面按照与应用相同的分组介绍每个工具。
- [命令行](cli.html) 列出所有 `killerbench-cli` 命令。
- [疑难解答](troubleshooting.html) 介绍被阻止的操作、重启和中断的请求。

希望将诊断功能嵌入自己工具的开发人员可以阅读 [Engine 开发人员指南](../engine/index.html)。
