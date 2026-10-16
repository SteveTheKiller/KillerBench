# 调查崩溃和蓝屏

当 Windows 意外重启、显示停止错误或某个应用程序反复崩溃时使用本指南。KillerBench 会将崩溃、关机、硬件错误和可靠性证据汇总到一份报告中。它只描述记录的内容，绝不猜测根本原因。

## 检查计算机

1. 打开 **诊断**，然后选择 **关键指标**。
2. 按 **运行诊断**。报告会汇总内存、磁盘运行状况、设备问题、可靠性监视器历史记录、硬件错误记录、崩溃报告、意外关机和 Windows 内存诊断结果。
3. 阅读结论及其下方的证据。过去的崩溃可以解释发生了什么，但不能证明故障仍然存在。

## 缩小范围

- **硬件错误历史记录** 会结合设备问题和磁盘运行状况，读取 WHEA 和内存诊断记录。
- **可靠性历史记录** 会汇总过去 14 天的应用程序崩溃、无响应和意外关机。
- **应用程序故障** 专注于崩溃的程序。
- **驱动程序安装历史记录** 显示与当前存在的设备相匹配的失败驱动程序安装。
- **设备问题** 列出带有设备管理器错误代码的设备。

## 读取崩溃转储

KillerBench 会列出转储配置和转储文件的元数据，并可以分析小型转储的结构，以获取异常和模块名称。桌面应用中绝不会打开转储内容。加载错误检查知识包后，停止代码会解析为其文档中说明的含义。

在调查完成之前，请保留转储。**清理** 可以删除它们，但这样也会丢弃证据。

## 通过命令行

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

请参阅 [vitals-check](cli.html#cmd-vitals-check)、[crash-review](cli.html#cmd-crash-review) 和 [device-code](cli.html#cmd-device-code)。

## 相关内容

- [诊断分区](diagnostics.html#vitals)
- [报告和证据](reports.html)
