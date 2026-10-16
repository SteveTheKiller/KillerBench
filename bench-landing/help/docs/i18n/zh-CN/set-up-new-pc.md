# 设置新计算机

准备新的或刚重置的电脑时使用本指南。KillerBench 会查看计算机、你计划安装的应用程序以及你想为它设置的名称，然后指向执行每项更改的受保护工具。

## 从基准开始

1. 从 **诊断** 运行 **完整系统扫描**，并保留报告作为计算机初始状态的记录。
2. 安装待处理的更新。如果有任何更新失败，请参阅 [修复 Windows 更新](fix-windows-update.html)。

## 查看应用程序和配置

1. 打开 **机器准备**，然后选择 **机器准备**。
2. 选择计划设置的应用程序和配置组，然后按 **运行诊断**。查看会将它们与已安装的内容进行比较。KillerBench 不会下载或运行安装程序。
3. 运行供应商的安装程序之前，请先通过命令行进行验证。只有当安装程序带有该供应商确切组织名称的有效签名时，KillerBench 才会信任它。

## 为计算机命名

1. 选择 **计算机名称**，输入名称，然后按 **运行诊断**，根据当前标识和域角色查看该名称。
2. 在工作组计算机上，勾选 **在重新检查后重命名此工作组 PC** 以进行重命名。需要重启才能完成。

## 保护和整理

- [使用 BitLocker 加密驱动器](encrypt-with-bitlocker.html) 并备份恢复密钥。
- [增强 Windows 隐私](make-windows-private.html)，并查看 **维护** 中的 **Windows 精简**。
- 一切就绪后，使用 **准备一个还原点** 创建还原点。

## 通过命令行

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

请参阅 [workflow](cli.html#cmd-workflow) 和 [restore-point](cli.html#cmd-restore-point)。

## 相关内容

- [机器准备分区](onboarding.html)
- [首次诊断](first-run.html)
