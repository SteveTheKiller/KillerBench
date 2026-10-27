# 增强 Windows 隐私

使用本指南可以查看并加强笔记本电脑的隐私设置。KillerBench 会读取当前设置，显示哪些设置已由策略或设备管理拥有，并且只逐项更改你选择的设置，同时支持精确撤消。

## 查看当前设置

1. 打开 **隐私**。
2. 勾选要查看的区域。所有区域最初都未选中。
3. 按 **运行诊断**。报告会显示此计算机和运行 KillerBench 的账户的当前值，并标记由策略拥有的设置。

## 更改设置

1. 在 **隐私设置** 中选择设置，例如相机、麦克风、位置、诊断数据、广告、剪贴板历史记录、云内容搜索或搜索要点。
2. 选择新值，然后勾选 **在我选择“运行诊断”时应用**。
3. 按 **运行诊断**。KillerBench 会检查策略所有者，记录当前值，进行一项更改并读回。

由组策略或设备管理拥有的设置保持不变。请在管理这些设置的地方进行更改。

## 撤消更改

每项已验证的更改都会保留一份日志。相应的撤消操作会在再次检查策略后，精确恢复之前的值。

## 精简 Windows

对于建议、消费者内容、Recall 和 AI 功能、任务栏和开始菜单中的多余项目、Edge 以及预装软件，请打开 **维护** 并选择 **Windows 精简**。查看是只读的，不会自动选择任何内容。

## 通过命令行

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

请参阅 [privacy-plan](cli.html#cmd-privacy-plan)、[privacy-setting](cli.html#cmd-privacy-setting)、[privacy-setting-undo](cli.html#cmd-privacy-setting-undo) 和 [debloat-review](cli.html#cmd-debloat-review)。指定不受支持的键时，会列出受支持的选项。

## 相关内容

- [隐私分区](privacy.html)
- [设置新计算机](set-up-new-pc.html)
