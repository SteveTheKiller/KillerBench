# 从脚本运行 KillerBench

当你需要在脚本、RMM 作业或远程控制台中获取 KillerBench 证据时，请使用命令行。`killerbench-cli` 运行与桌面应用相同的 Engine，并输出 JSON。

## 入门

1. 在 `killerbench-cli.exe` 所在位置打开 PowerShell，或将其文件夹添加到路径中。
2. 运行 `killerbench-cli help`，列出所有命令及其安全边界。
3. 运行 `killerbench-cli diagnostic-scopes`，查看 11 个建议的诊断组。

## 运行扫描并保存报告

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` 仅用于诊断，并会拒绝 `--auto-repair`。需要执行具体操作时，请使用专门的修复工作流程。`--report` 会写入与应用导出的报告相同的主题样式报告。使用 `--scope` 可以只检查部分区域，使用 `common-check` 可以在没有工作流程日志的情况下获取合并的 JSON。

## 了解退出代码

| 代码 | 含义 |
| --- | --- |
| 0 | 预览或已验证的结果成功。 |
| 1 | 启动、输入或日志错误导致命令停止。 |
| 2 | 无法识别参数。 |
| 3 | 被阻止、失败或未验证。 |
| 4 | 工作流程需要先重启才能验证。 |
| 130 | 你停止了它。 |

只读命令的退出代码 0 表示命令已运行，而不是计算机运行正常。请阅读 JSON 了解结论。

## 进行更改

所有可能更改计算机的命令默认都只显示预览。请阅读预览，然后使用 `--execute` 运行同一命令。如果请求被中断，请运行相应的 `-recover` 命令；它会报告实际结果，并且绝不会重复更改。

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## 重新打开以前的结果

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## 从 AI 助手使用

[KillerMCP](../mcp.html) 可以调用已安装的 CLI 来查询设备管理器和 Windows 错误代码。

## 相关内容

- [命令行参考](cli.html)
- [Engine 开发者指南](../engine/index.html)
- [疑难解答](troubleshooting.html)
