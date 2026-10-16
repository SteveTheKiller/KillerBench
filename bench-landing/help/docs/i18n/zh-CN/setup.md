# 下载和设置

KillerBench 0.1.0 尚未发布。目前还没有公开的 KillerBench 软件包或下载。本页介绍当前的软件包要求，以便你在不下载非官方副本的情况下做好准备。

## Windows 和运行时

- 当前的发布目标是 **Windows x64**。Windows 11 x64 是当前实机验证的基准。不提供原生 ARM64、x86 和 Windows Server 发行版。
- KillerBench 有两种打包方式。安装程序 **依赖框架**，需要 **.NET Desktop Runtime 10 for Windows x64**。普通的 .NET Runtime、ASP.NET Core Runtime 和 .NET Framework 不提供 WPF。便携版自带运行时，无需单独安装。命令行工具 `killerbench-cli.exe` 需要 .NET 10 Runtime for Windows x64。SDK 仅用于开发，日常使用不需要。
- 请使用 [.NET 10 on Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows) 支持的 Windows 版本。运行时受支持并不表示 KillerBench 的修复已在每个受支持的版本或内部版本上经过验证。Windows 10 的支持仍待定，其受支持的版本受 Microsoft 当前生命周期要求的限制。
- 请仅从 [Microsoft 的 .NET 10 下载页面](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) 获取 Desktop Runtime，并选择 **.NET Desktop Runtime**、**Windows**、**x64**。请遵守所在组织的软件安装规定。

如果 `dotnet` 可用，`dotnet --list-runtimes` 应列出 x64 安装的 `Microsoft.WindowsDesktop.App 10.0.x`。仅为其他体系结构安装的运行时不满足 x64 安装程序软件包的要求。

## 软件包发布后

1. 请使用本网站链接的官方下载。目前还没有可用的 KillerBench 下载链接。
2. 阅读该软件包的要求、[GPLv3 许可证](../license.txt)、对应源代码的获取说明、随附的第三方声明以及签名和校验和信息。仅凭文件名无法证明真实性。
3. 为当前账户运行安装程序，或在提升权限提示后为所有用户运行安装程序。它会添加开始菜单快捷方式、可选的桌面快捷方式以及“添加或删除程序”条目。便携版无需安装即可运行。
4. 以普通账户启动 KillerBench，并按照 [首次诊断](first-run.html) 操作。诊断时的访问失败会保持可见；提升权限是另一项有意的选择。

不要为了运行未经验证的副本而绕过 Windows 安全警告。在选择任何会更改计算机的操作之前，请保留最新的备份。

启动时，KillerBench 会在 GitHub 上检查是否有更新的稳定版本，并在安装前征求你的同意。下载内容必须与公布的 SHA-256 校验和匹配。可以在“关于”中通过 **启动时检查更新** 关闭此检查。

## 当前限制

当前版本包含十二个分区、只读诊断、报告以及单独选择的修复功能。实机修复验证仍在进行中。界面能够运行、生成成功或测试数据的结果都不能证明在真实计算机上的修复行为。请参阅 [如何保证更改安全](safety.html#development-status)。

产品首页上的示意控制台是合成输出。应用的最终截图和首个公开软件包仍在准备中。

## 源代码、许可证和支持

KillerBench 采用 GPLv3 许可证。首个公开软件包的源代码提供说明和第三方字体许可仍待定。有关源代码可用性、分发声明、证据隐私和支持信息，请阅读 [关于 KillerBench](about.html)。[Engine 指南](../engine/) 面向拥有授权源代码检出的开发人员，并不表示已有公开的 Engine 软件包。
