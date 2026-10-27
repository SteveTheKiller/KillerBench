# 解决打印问题

当作业一直停留在队列中、打印机显示脱机或默认打印机不正确时使用本指南。KillerBench 会先读取后台打印程序、队列、作业、驱动程序和端口，然后让你修复某一个确切的队列或作业。

## 检查打印机

1. 打开 **设备**，然后选择 **打印**。
2. 按 **查找本地打印机**，然后选择出现问题的队列。若要检查所有队列，请选择 **使用一般打印检查**。
3. 保持 **自动修复已确认的问题** 关闭，然后按 **运行诊断**。
4. 阅读报告。它会显示 Print Spooler 状态、已暂停的队列、脱机模式、可见的作业、驱动程序状况、端口注册、最近的 PrintService 错误，对于网络打印机，还会显示对打印机分配地址的一次连接尝试。

连接成功或状态代码正常并不能证明打印机有纸、有墨粉或机械正常。

## 修复队列

1. 开启 **自动修复已确认的问题**，再次运行 **打印**。它可以启动已启用的后台打印程序，恢复有作业等待的确切已暂停队列，并清除脱机模式。
2. 清除脱机模式可能会释放等待中的作业，因此请确认这些作业仍需打印。
3. 有人故意暂停且没有作业等待的队列会保持暂停状态。

## 清除一个卡住的作业

1. 按 **查找被阻止的打印作业**。
2. 选择显示 Error 或 Blocked 的作业，然后勾选 **仅取消此选定作业**。
3. 按 **运行诊断**。只会取消该作业，并读回结果。

## 修复默认打印机

选择应作为默认打印机的队列，勾选 **使用此队列修复缺失或冲突的默认打印机**，然后按 **运行诊断**。此操作仅在默认打印机缺失或冲突时执行。

## 通过命令行

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

请参阅 [printer-list](cli.html#cmd-printer-list)、[printer-check](cli.html#cmd-printer-check)、[printer-repair](cli.html#cmd-printer-repair)、[print-job-cancel](cli.html#cmd-print-job-cancel) 和 [default-printer-set](cli.html#cmd-default-printer-set)。

## 相关内容

- [设备分区](devices.html#printing)
- [解决网络和 Internet 问题](fix-network.html)
