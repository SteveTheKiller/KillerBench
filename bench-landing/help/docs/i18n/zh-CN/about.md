# 关于 KillerBench

KillerBench 是由 Steve the Killer 开发的 Windows 诊断、修复和维护工作台，属于 [Killer Tools 系列](https://killertools.net)。1.0.0 版本尚未发布。其桌面应用、Engine 和 CLI 共享诊断证据和受支持的修复工作流程。

## 可用性和范围

目前还没有公开的 KillerBench 下载或 Engine 软件包。[下载和设置](setup.html) 介绍当前的 Windows x64 目标和 Desktop Runtime 要求。实机修复验证仍在进行中；某个工具的存在并不能证明所有故障、中断或 Windows 配置都已经过测试。

## 源代码和分发声明

KillerBench 采用 **GPLv3** 许可证。其分发许可证为 [GNU 通用公共许可证第 3 版](../license.txt)。该许可证规定了复制、修改和分发受保护作品的权限和条件。

公开的 [KillerBench 仓库](https://github.com/SteveTheKiller/KillerBench) 目前提供项目信息，完整的实现并未在那里公开。这些开发人员指南描述的是经授权的源代码检出，并不提供公开的源代码下载。

在分发二进制文件之前，发布者必须通过 [GPLv3 第 6 节](https://www.gnu.org/licenses/gpl-3.0.en.html#section6) 允许的方式提供对应源代码，包括许可证要求的源代码和生成材料。首个公开软件包的源代码提供说明和随附的第三方声明仍待定。GPLv3 不授予重新分发第三方字体的权限；当前的字体许可仍需核实。重新分发之前，请检查已发布软件包的许可证、源代码说明和声明。

## 隐私和外部服务

诊断和日志分析在本地运行，无需 AI 服务或 KillerBench 账户。日志和保留的证据使用本地账户的 `%LOCALAPPDATA%\KillerBench` 文件夹。所选操作可能会连接 Windows 更新、软件下载服务、目录服务或其他指定的目标。启动时的更新检查会连接 GitHub，知识更新和 MCP 安装程序页面会连接其已配置的服务。使用 MCP 的 AI 客户端可能会收到返回的参考结果。

本网站会将主题和强调色选择保存在浏览器的本地存储中。其指南不会在你的计算机上运行诊断。原始日志、导入的文件和导出的证据可能包含计算机名称、账户、路径或敏感的源文本。共享前请先检查；不要在公开报告中包含恢复密钥、凭据或客户数据。

## 支持

项目信息可在公开仓库中查看。如需支持，请使用 [作者网站](https://thekiller.net) 上公布的联系方式，并提供应用版本、Windows 内部版本、具体工具、预期结果和实际结果。发送前请检查证据并删除敏感内容，客户信息请使用经批准的私密渠道。对于结果不确定的修复，请保留日志。

## 相关指南

- [如何保证更改安全](safety.html)
- [报告和证据](reports.html)
- [Engine 开发人员指南](../engine/)
- [KillerMCP](../mcp.html)
