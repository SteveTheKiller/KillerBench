# 使用 BitLocker 加密驱动器

使用本指南可以启用 BitLocker、备份恢复密钥并确认驱动器已受保护。KillerBench 会在开始加密之前备份并验证恢复密码，并且绝不会显示或记录恢复密码。

## 开始之前

- 以管理员身份运行 KillerBench。
- Windows 驱动器需要 TPM 保护器和恢复密码保护器。数据驱动器只需要恢复密码。
- 确定恢复密钥备份的保存位置：位于要加密的驱动器之外的文件，以及可选的 Active Directory 或 Microsoft Entra ID。

## 检查驱动器

1. 打开 **BitLocker**，然后在 **卷** 中选择驱动器。
2. 将操作保持为 **检查**，然后按 **运行诊断**。报告会显示加密状态、保护状态和保护器标识。
3. 如果 TPM 存在但尚未就绪，请先在 **安全** 中准备它。KillerBench 绝不会清除 TPM。

## 添加所需的保护器

1. 打开 **更多 BitLocker 操作**。
2. 选择 **添加 TPM 保护器** 或 **添加恢复密码**，按 **预览**，勾选 **按下“运行操作”时对所选驱动器进行此更改**，然后按 **运行操作**。
3. 立即备份新的恢复密码。KillerBench 不会显示它。

## 备份恢复密钥并加密

1. 按 **查找恢复保护器**，然后选择恢复密码保护器。
2. 输入位于要加密的驱动器之外的备份文件的完整路径。
3. 选择 **加密**，选择加密方法，然后勾选 **在我选择“运行诊断”时备份所选的恢复密钥，然后开始加密**。
4. 按 **运行诊断**。KillerBench 会写入备份、验证备份，然后开始一次加密。
5. 加密会在后台继续进行。稍后运行 **检查**，确认驱动器报告已完全加密且保护已开启。

## 备份到 Active Directory 或 Entra ID

在 **更多 BitLocker 操作** 中，为所选保护器选择目录备份。KillerBench 会报告 Windows 已接受请求，但无法读取目录中的副本，因此请在 Active Directory 或 Entra 管理中心中确认。

## 数据驱动器和自动解锁

数据驱动器加密后，**启用自动解锁** 可以让它在 Windows 启动时解锁。Windows 驱动器必须已完全加密且保护已开启。

## 通过命令行

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

请参阅 [bitlocker-protectors](cli.html#cmd-bitlocker-protectors)、[bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add)、[bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) 和 [bitlocker-escrow](cli.html#cmd-bitlocker-escrow)。每个命令都有对应的 `-recover` 命令，用于处理中断的请求。

## 相关内容

- [BitLocker 分区](bitlocker.html)
- [安全分区](security.html)
- [如何保证更改安全](safety.html)
