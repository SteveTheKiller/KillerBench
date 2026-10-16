# 命令行

`killerbench-cli` 可以从 PowerShell 或远程控制台运行与桌面应用相同的 Engine。所有可能更改计算机的命令默认只显示预览，执行前需要 `--execute` 等明确的标志。命令会将 JSON 证据输出到标准输出，将进度输出到标准错误。

操作指南：[从脚本运行 KillerBench](use-the-cli.html)。

退出代码 0 表示预览或已验证的结果成功，1 表示启动、输入或日志错误，2 表示无法识别参数，3 表示被阻止、失败或未验证，4 表示工作流程需要重启后才能验证，130 表示你已停止它。

不带参数运行 `killerbench-cli`，即可在你自己的计算机上打印此列表。

## 更新和维护

- `bits-job-review JOB_GUID [--all-users] [--observe]`: 确切的传输检查或两次快照观察；不重试。
- `bits-jobs [--all-users]`: BITS 作业状态、所有者和字节数；不更改传输。
- `bits-retry JOB_GUID [--execute]`: 默认预览；明确重试确切的作业，不声称应用程序已完成。
- `bits-retry-recover JOB_GUID`: 两次只读快照；在不再次重试的情况下存档稳定结果。
- `component-repair-source`: 受管理的 DISM 修复来源策略和冲突；不更改。
- `delivery-optimization`: 只读的传递优化服务状态；JSON 证据。
- `delivery-optimization-activity`: 有隐私范围限制的当前作业数和字节总数；不含传输标识，不更改。
- `delivery-optimization-policy`: 只读的经典设置和 Policy CSP 设置；排除敏感值和提供程序标识。
- `feature-service-events`: 读取时间、搜索、WMI、事件日志和音频服务的确切故障历史记录。
- `feature-update-policy`: 只读的目标版本、延迟、暂停和安全保护策略；JSON 证据。
- `pending-file-operations`: 只读的、有隐私范围限制的待处理文件操作摘要；JSON 证据。
- `restart-check`: 只读的待处理重启来源；JSON 证据。
- `servicing-logs`: 有范围限制地分析最新的 CBS 日志、纯文本和压缩的 CBS 存档、DISM 和 Windows Setup 日志；不修复。
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: 只读的维护活动快照。
- `servicing-service-events`: 只读的 Windows 更新维护链确切故障历史记录。
- `servicing-service-states`: 只读的 Windows 更新维护链六个服务的当前状态。
- `setup-code DECIMAL_OR_0xHEX`: 有文档依据的 Windows Setup 结果代码参考；不检查计算机，不修复。
- `update-applicability`: 只读的 Windows 版本、内部版本和体系结构证据。
- `update-cache`: 有范围限制的只读 SoftwareDistribution 缓存总量；不含文件名。
- `update-code DECIMAL_OR_0xHEX`: 有文档依据的 Windows 更新结果代码参考；不检查计算机，不修复。
- `update-context`: 只读的当前权限提升和桌面会话证据；不含账户标识符。
- `update-engine-files`: 只读的原生 Windows 更新引擎存在、版本和签名证据。
- `update-events`: 只读的过去七天 Windows Update Client 历史记录；包含原始证据的 JSON。
- `update-followup DECIMAL_OR_0xHEX`: 根据有文档依据的 Windows 更新结果代码选择的新的只读诊断。
- `update-history`: 只读的最新 100 个 Windows 更新操作结果；JSON 证据。
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: 重复使用 `--select`；默认预览。
- `update-policy`: 只读的受管理 Windows 更新策略；JSON 证据。
- `update-policy-surfaces`: 只读的经典设置和 Policy CSP 配置面证据；排除值和提供程序标识。
- `update-recover`: 检查待处理的更新；绝不安装或重试。
- `update-restart-policy`: 只读的合规截止日期、宽限期、重启抑制和通知行为。
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: JSON 发现结果和匹配项；不安装。
- `update-service-events`: 只读的 Windows 更新服务确切故障历史记录。
- `update-service-files`: 只读的 Windows 更新服务确切 DLL 注册和签名证据。
- `update-services`: 只读的 Windows 更新服务注册；JSON 证据。
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: 需要先进行新的搜索；默认预览；保留旧存储。
- `update-store-reset-recover`: 核对已准备的存储重置，而不重复执行。
- `windows-capabilities`: 只读的联机 Windows 功能状态；JSON 结果。
- `windows-update-task`: 只读的 Scheduled Start 确切任务状态和运行历史记录；不运行任务，不执行更新操作。

## BitLocker 和 TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: 默认预览；为已加密的数据卷启用自动解锁。
- `bitlocker-auto-unlock-recover`: 验证中断的自动解锁请求，而不重复执行。
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: 默认预览；仅创建新的恢复备份文件。
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: 验证中断的备份，而不再次写入。
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: 默认预览；开始解密，例如在使用所需方法重新加密之前。
- `bitlocker-decrypt-recover`: 验证中断的解密请求，而不重复执行。
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: 默认预览；需要现有的 TPM 保护器和恢复保护器。
- `bitlocker-encrypt-recover`: 验证待处理的转换状态，而不重复请求。
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: 默认预览；将现有的恢复密码备份到 Active Directory 或 Microsoft Entra ID。
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: 仅预览选项。
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: 默认预览；添加一个 TPM 或恢复密码保护器；绝不打印密码。
- `bitlocker-protector-recover`: 验证中断的保护器请求，而不重复执行。
- `bitlocker-protectors DRIVE:`: 仅保护器标识和类型；不含恢复密码。
- `bitlocker-resume DRIVE: [--execute]`: 默认预览；仅限已完全加密且保护已暂停的确切卷。
- `bitlocker-resume-recover`: 验证待处理的保护状态，而不重复请求。

## 安全和隐私

- `certificate-chains`: 只读的个人证书本地仅缓存信任链；不检查吊销，不下载。
- `defender-overview [--report]`: 只读的保护、扫描时间和威胁汇总概览；不执行操作。
- `defender-protection [--report]`: 只读的保护首选项和已识别的策略面；排除敏感详细信息；不更改。
- `defender-scan Quick|Full [--execute] [--report]`: 默认预览；一次明确的受保护 Microsoft Defender 扫描。
- `defender-scan-history [--report]`: 只读的快速扫描和完整扫描时间及逾期状态；排除敏感详细信息；不扫描。
- `defender-scan-recover [--report]`: 只读核对，不启动新的扫描。
- `defender-threats [--report]`: 威胁历史汇总、当前活动标志和严重性 ID；排除敏感详细信息；不执行操作。
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: 默认预览；仅限不受管理的本地配置文件。
- `firewall-enable-recover`: 检查待处理的更改，而不重复执行。
- `firewall-events`: 读取有范围限制的 Windows 防火墙警告、错误和严重操作历史记录。
- `firewall-logging`: 在不打开日志的情况下读取域、专用和公用流量日志配置。
- `firewall-rules`: 读取有隐私范围限制的有效规则和策略来源数量。
- `firewall-service-events`: 读取 BFE 和 Windows Defender 防火墙的确切服务控制管理器故障历史记录。
- `privacy-clipboard-history on|off [--execute]`: 默认预览；在保留策略的情况下设置当前用户的确切值。
- `privacy-clipboard-history-recover`: 重新检查中断的更改，而不重复执行。
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: 受保护地还原之前的值。
- `privacy-plan --select Camera --select ClipboardSync`: 仅查看所选内容；不更改设置。
- `privacy-setting KEY on|off [--execute]`: 默认预览；保留策略的当前用户更改；无效的键会列出受支持的选项。
- `privacy-setting-recover KEY`: 重新检查中断的更改，而不重复执行。
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: 受保护地还原确切值。
- `security-check`: 只读的 TPM、安全启动、虚拟化保护和 UAC 策略；JSON 证据。

## 服务

- `service-binaries`: 只读的本地服务二进制文件存在情况和 Authenticode 状态；排除路径，不连接非本地路径。
- `service-check NAME [NAME ...] | service-check --all`: 只读的受支持服务状态和依赖项证据。
- `service-dependencies`: 只读的直接依赖项完整性查看；不更改服务。
- `service-dlls`: 只读的已注册服务 DLL 存在情况和 Authenticode 状态；排除路径，不连接非本地路径。
- `service-events`: 只读的服务故障历史记录；包含原始证据的 JSON。
- `time-events`: 只读的最近 Windows 时间事件历史记录，附 JSON 证据。
- `service-logons`: 只读的服务账户类别和桌面交互查看；排除账户标识。
- `service-path-quoting`: 只读的服务可执行文件路径引号查看；排除完整路径和参数。
- `service-recovery-policy`: 只读的已安装服务恢复操作摘要；排除命令和重启消息。
- `service-startup-config`: 只读的启动模式一致性查看；不更改服务。
- `service-transitions`: 只读地重复观察待处理的服务状态；不执行服务操作。
- `service-triggers`: 只读的已安装服务触发器摘要；排除触发器负载；不更改。

## 存储、文件和备份

- `backup-events`: VSS 和 volsnap 历史记录；JSON 证据；不更改。
- `backup-service-events`: 读取 VSS、软件卷影复制提供程序、Windows 备份和块级备份引擎的确切故障历史记录。
- `backup-summary`: 本地 Windows 备份操作摘要；不更改备份。
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: 默认预览；回收站要么全部清空要么不清空，天数不适用。
- `disk-cleanup-task`: 只读的确切任务状态和运行历史记录；不运行任务，不清理。
- `disk-diagnostic`: 关联确切的磁盘诊断任务和事件历史记录；不执行任务或磁盘操作。
- `disk-diagnostic-tasks`: 只读的确切收集器和解析器任务历史记录；不执行任务或磁盘操作。
- `disk-quotas`: 只读的每卷配额默认值和状态；排除用户标识；不更改。
- `file-history-events`: 按隐私投影的文件历史记录警告和错误数量；不更改。
- `file-history-status`: 当前账户的文件历史记录保护状态；不更改。
- `search-index-rebuild [--execute]`: 默认预览；保留之前的索引并验证新状态。
- `search-index-rebuild-recover`: 只读核对，不重复修复命令。
- `storage-check DRIVE_LETTER`: 只读的固定 NTFS 检查；JSON 结果和实时进度。
- `storage-events`: 只读的存储历史记录；包含原始证据的 JSON。
- `storage-jobs`: 只读的 Storage 模块作业汇总状态和进度；JSON；不更改。
- `storage-reliability`: 只读的温度、磨损和 I/O 计数器；JSON；不修复。
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: 默认预览；执行需要确认备份；系统卷会安排在重启时进行。
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: 仅预览；不修复，不计划。
- `storage-repair-recover`: 验证已完成或已计划的文件系统修复，而不重复执行。
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: 收集并绑定当前证据；不修复，不计划。
- `storage-sense-policy`: 只读的已识别计算机清理策略；排除用户设置和文件标识。
- `storage-sense-task`: 只读的确切任务状态和运行历史记录；不运行任务，不清理。
- `storage-service-events`: 读取存储服务、优化驱动器、VSS 和软件卷影复制提供程序的确切故障历史记录。
- `volume-capacity DRIVE:`: 只读的可用空间，附 JSON 证据。
- `volume-dirty DRIVE:`: 只读的脏位状态；不修复，不重启。
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: 默认只读分析；需要运行正常的确切卷和兼容的介质。
- `volume-optimize-recover`: 验证待处理的已完成操作，而不重复执行。
- `vss-providers`: 只读的 VSS 提供程序名称、版本和类型；排除标识符；不更改注册。
- `vss-writers`: 报告的写入器状态；解释英语输出；不重置。

## 打印

- `print-events`: 只读的过去七天 PrintService Admin 历史记录；JSON 证据。
- `print-extension-files`: 只读的打印处理器和打印监视器二进制文件存在情况和签名摘要；排除完整路径。
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: 默认预览；仅限处于 Error 或 Blocked 状态的确切作业。
- `print-job-cancel-recover [--folder PATH]`: 重新检查中断的取消操作，而不重复执行。
- `print-job-list`: 列出当前可见的作业而不更改它们。
- `print-operational-events`: 只读的 PrintService Operational 通道状态和可用历史记录；JSON 证据。
- `print-policy`: 只读的 Point and Print 限制和驱动程序安装策略；JSON 证据。
- `print-rendering-policy`: 只读的渲染和驱动程序隔离策略；JSON 证据。
- `print-spooler-events`: 只读的与本地 Print Spooler 标识相关的服务控制管理器故障；JSON 证据。
- `printer-check --queue NAME [--queue NAME ...]`: 只读的所选本地队列设置。
- `printer-conditions`: 只读的本地队列状况代码；不进行物理验证，不更改。
- `printer-driver-files`: 只读的已注册文件存在情况和可执行文件签名摘要；排除完整路径。
- `printer-driver-models`: 只读的版本 3、版本 4 和包感知摘要；JSON 证据。
- `printer-drivers`: 只读的队列和驱动程序注册清单及匹配；JSON 证据。
- `printer-endpoint-check`: 对确切分配的 Standard TCP/IP 打印机终结点进行有范围限制的连接；不发现，不使用 SNMP，不发送打印数据。
- `printer-list`: 在不更改设置的情况下发现本地队列。
- `printer-ports`: 只读的队列和端口注册匹配；JSON 证据。
- `printer-queue-attributes`: 只读的本地队列后台打印、保留和共享属性。
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: 默认预览；清除脱机模式可能会释放等待中的作业。

## 网络

- `dhcp-check`: 只读的缺失或已过期 DHCP 租约适用性；JSON 证据，不续订。
- `dhcp-events`: 只读的 IPv4 和 IPv6 DHCP Client 事件历史记录；JSON 结果。
- `dhcp-leases`: 只读的租约时间戳、到期时间和服务器证据；JSON 结果。
- `dhcp-renew [--execute]`: 默认预览；仅限已连接物理适配器的确切缺失或已过期 DHCP 租约。
- `dhcp-renew-recover`: 只读核对，不请求新的租约。
- `dns-cache HOST [--execute]`: 默认预览；受保护的本地缓存刷新，并验证确切的主机。
- `dns-cache-recover`: 重新检查中断的缓存操作，而不重复刷新。
- `dns-cache-summary`: 有隐私范围限制的本地 DNS 缓存状态和记录类型计数；不更改。
- `dns-check HOST [--timeout-seconds 1..60]`: 有范围限制的系统名称解析；JSON 结果。
- `dns-client-config`: 只读的后缀、注册、名称回溯和策略证据；JSON 结果。
- `dns-client-events`: 只读的、有隐私范围限制的 DNS Client 警告和故障历史记录；JSON 结果。
- `dns-nrpt-policy`: 只读的有效 DNS 命名空间策略证据；JSON 结果。
- `ip-addresses`: 只读的当前 IP 地址状态、来源和冲突查看；JSON 证据。
- `ip-interface-config`: 只读的 IP 接口配置证据；JSON 结果。
- `network-firewall`: 将活动的网络分配与相应防火墙配置文件的启用状态进行关联。
- `network-offload`: 只读的 Windows 全局网络卸载配置；JSON 结果。
- `network-profiles`: 只读的当前配置文件类别和连接分类；排除网络名称。
- `network-service-events`: 读取 DHCP、DNS、Workstation、NLA、Network List 和 IP Helper 的确切故障历史记录。
- `proxy-check`: 只读的已保存 WinHTTP 和当前账户 Internet 代理证据。
- `route-priority`: 只读的有效默认路由跃点数比较；JSON 证据。
- `smb-client-config`: 只读的 SMB 签名、来宾访问和传输配置。
- `smb-connections`: 只读的活动协议协商；排除远程标识。
- `smb-server-config`: 只读的 SMB 协议、签名、加密和多通道配置。
- `smb-shares`: 有范围限制的只读 SMB 共享清单；排除路径、权限、会话和账户。
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: 有范围限制的连接检查；不发送应用程序数据。
- `tcp-port-pressure`: 只读的动态 TCP 范围和本地端口使用汇总；排除终结点和进程。
- `tcp-settings`: 只读的 Windows TCP 模板配置证据；JSON 结果。
- `vpn-profiles`: 只读的当前账户 VPN 配置文件配置；排除凭据。
- `winrm-listeners`: 只读的 WinRM 侦听器注册摘要；排除证书指纹。
- `winsock-catalog`: 只读的原生 Winsock 协议目录结构；JSON 结果。
- `winsock-namespaces`: 只读的原生 Winsock 命名空间提供程序结构；JSON 结果。
- `winsock-provider-files`: 只读的协议提供程序文件存在情况和签名；JSON 结果。
- `winsock-readiness`: 只读的原生 Winsock 协议、命名空间提供程序和提供程序文件就绪状态；JSON 结果。

## 应用

- `app-folder-review FOLDER_NAME [--report]`: 固定根目录下确切文件夹的查看；不删除。
- `app-package-review FULL_NAME [--all-users]`: 确切的目标包和依赖包；不删除。
- `app-packages [--all-users]`: 仅清点已注册的 Appx/MSIX；不删除。
- `browser-extensions`: 查看 Chrome、Edge、Brave 和 Firefox 扩展的标识；不删除。
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: 仅查看；不更改系统。
- `depot-review [application and configuration selections] [--json]`: 仅查看；不预配。
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: 只读的 Authenticode 和供应商签名者检查；不运行任何内容。
- `foxit-inventory`: 只读的计算机产品清单；JSON 输出。
- `installed-apps [--current-user]`: 卸载注册和功能标志；不删除。
- `installed-apps-compare BEFORE.json AFTER.json`: 已保存清单中的注册差异；不更改计算机。
- `installed-hotfixes`: 有范围限制的只读已安装 Windows 修补程序清单；JSON 证据。
- `installer-activity`: 只读的 Installer 进程和事务标记证据；JSON 证据。
- `installer-check`: 只读的 Installer 策略和引擎就绪状态；JSON 证据。
- `installer-engine-files`: 只读的必需 Installer 文件存在情况和 Authenticode 摘要；JSON 证据。
- `installer-events`: 只读的过去七天 MSI 历史记录；JSON 证据。
- `installer-logging`: 只读的 Installer 日志记录和调试策略；JSON 证据。
- `installer-pending-state`: 只读的待处理文件操作与 Installer 活动关联；JSON 证据。
- `installer-registration-repair [--execute]`: 默认预览；仅限确切的服务注册缺陷；绝不取消注册引擎。
- `installer-registration-repair-recover`: 重新检查中断的注册，而不重复命令。
- `installer-security-policy`: 只读的 Installer 安全和修补程序策略；JSON 证据。
- `installer-service-events`: 只读的与本地 Windows Installer 标识相关的服务控制管理器故障；JSON 证据。
- `installer-service-registration`: 只读的 Windows Installer 服务注册摘要；JSON 证据。
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: 新的确切适用条件预览；不含删除提供程序。
- `mace-package-recover`: 重新核对一条中断的应用包删除记录；绝不重复删除。
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: 仅查看；不删除。
- `msi-cache`: 只读的已注册 Windows Installer 缓存可见性；JSON 证据。
- `msi-code NONNEGATIVE_DECIMAL`: 有文档依据的 Windows Installer 返回代码参考；不检查计算机，不修复。
- `msi-product {PRODUCT-CODE}`: 只读的确切 MSI 注册状态；JSON 证据。
- `msi-repair {PRODUCT-CODE} [--execute]`: 默认预览；仅限确切的损坏或配置错误的产品。
- `msi-repair-recover`: 重新检查中断的确切产品修复，而不重复执行。
- `msi-sources`: 只读的、有隐私范围限制的已注册 Windows Installer 来源元数据；JSON 证据。
- `orca-cache-reset --mode New|Classic|Both [--execute]`: 默认预览；以恢复名称保留运行账户的确切缓存。
- `orca-cache-reset-recover`: 验证中断的缓存保留，而不重复移动。
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: 仅查看；不更改 Outlook。
- `startup-apps`: 查看启动注册以及启动或登录任务；不禁用。
- `third-party-update ID SOURCE VERSION [--accept-agreements] [--execute]`: 默认预览；仅限一个确切的版本。
- `third-party-update-recover`: 验证待处理的确切版本，而不重试更新。
- `third-party-updates`: 只读的 Microsoft.WinGet.Client 应用程序更新发现；不更改应用程序。

## 系统

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: 本地确切查找 Bluetooth SIG 分配；不发出网络请求。
- `crash-config`: 已存储的 Windows 转储模式和路径；不更改。
- `crash-dumps`: 默认 Windows 转储文件元数据；不分析内容，不更改。
- `crash-events`: 最近的系统错误报告；不诊断，不更改。
- `crash-review [--knowledge PACK_PATH ...]`: 最多 32 个知识包；综合查看崩溃、关机、有范围限制的小型转储结构和可选的确切错误检查参考；不更改。
- `default-printer`: 只读的默认选择和自动管理设置；JSON 证据。
- `default-printer-set QUEUE [--execute] [--folder PATH]`: 默认预览；仅限缺失或冲突的默认打印机。
- `default-printer-set-recover [--folder PATH]`: 重新检查中断的选择，而不重复执行。
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: 只读的设备证据，可附带来自最多 32 个知识包的确切驱动程序参考。
- `device-code DECIMAL_OR_0xHEX`: 离线设备管理器问题代码参考。
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: 仅参考候选项。
- `event-channel-enable CHANNEL [--execute]`: 默认预览；启用一个确切的、不受管理的诊断通道，并保留其他设置。
- `event-channel-enable-recover`: 只读核对，不重复启用命令。
- `event-channels`: 只读的四个诊断通道的状态、保留、大小和饱和度。
- `font-registrations`: 有范围限制的只读计算机和当前用户注册及文件存在检查。
- `memory-diagnostic-results`: 有范围限制的只读 Windows 内存诊断历史记录；JSON 证据。
- `minidump-analyze PATH`: 有范围限制的小型转储结构元数据；不含内存页、符号，不更改。
- `name-rename PROPOSED_NAME [--execute]`: 默认预览；仅限独立的工作组电脑；绝不重启 Windows。
- `name-rename-recover`: 验证待处理的重命名，而不重复执行。
- `name-review PROPOSED_NAME`: 只读的标识和建议名称查看。
- `profiles --recover`: 验证已记录的不存在状态并保留恢复证据；绝不重试删除。
- `profiles --recover-backup`: 验证并存档待处理的备份工件；绝不重试复制。
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: 先预览；受保护的原生复制需要明确执行，且本地 NTFS 目标必须不存在。
- `profiles --select SID --create-backup-artifact DESTINATION`: 比较现有的已复制备份并创建其固定工件；不复制或删除配置文件。
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: 只读查看；验证需要选择一个 SID。
- `task-check`: 只读的任务清单、分类结论和 JSON 证据。
- `task-definition TASK_FOLDER TASK_NAME`: 只读的确切任务 XML；不可用并不表示不存在。
- `task-inventory`: 只读的任务标识和状态；JSON 输出。
- `task-journal RECORD.json [--completion COMPLETION.json]`: 历史恢复查看；绝不授权重试。
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: 验证并查看任务草稿；不注册。
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: 默认预览；已禁用的、仅创建的 Local System 定义。
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: 只读的确切定义核对；绝不重试或运行任务。
- `task-xml PLAN.json YYYY-MM-DD`: 将已禁用的查看用 XML 输出到 stdout；仅限本地时区的日历计划；不注册。
- `time-config`: 只读的 Windows 时间模式、对等节点和策略证据。
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: 默认预览；自定义模式需要对等节点。
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: 仅预览。
- `time-recover --folder PATH`: 验证并存档待处理记录；绝不重试更改。
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: 只读；自定义模式需要对等节点。
- `wmi-provider`: 只读的本地操作系统提供程序查询；JSON 证据。

## 知识、验证和 Engine

- `knowledge PACK_PATH CONTEXT CODE`: 本地参考查找；不执行修复。
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: 已验证的本地激活；不下载。
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: 验证当前的本地知识包；不更改。
- `knowledge-compare BEFORE_JSON AFTER_JSON`: 仅参考更改；不安装知识包。
- `knowledge-info PACK_PATH`: 知识包元数据和每个上下文的条目数；不更改。
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: 为观察到的代码附加上下文参考。
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: 选择最新的已验证保留知识包；不下载。
- `knowledge-search PACK_PATH CONTEXT TEXT`: 最多 50 个参考文本匹配项；不执行修复。
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: 在最多 32 个知识包中解析一个确切标识；存在冲突时绝不选择。
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: 验证每个已配置的已安装数据库；不下载，不激活。
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: 从已配置的 HTTPS 终结点获取并原子激活已签名的数据库候选项。
- `open <file.kbench> [--json]`: 重新打开已保存的捕获，并使用已安装的规则重新评估。
- `scan --output <new-file.kbench> [--json]`: 保存到新文件的只读 Windows 更新捕获。
- `validation-coverage RECORD.json [...]`: 汇总当前版本的原生虚拟机证据；不诊断，不修复。
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: 只读的一次性虚拟机场景计划；不诊断，不修复。
- `validation-record RECORD.json`: 验证一条有范围限制的原生虚拟机证据记录；不诊断，不修复。
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: 当前版本的本地虚拟机覆盖范围报告。
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 运行已验证的只读共享选项计划。
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: 仅清单。
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 专用诊断日志；不修复。
- `workflow computer-name --name PROPOSED_NAME [--json]`: 仅记录日志的查看。
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: 可选的确切产品状态会包含在日志中。
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: 可选的所选目标诊断会包含在日志中。
- `workflow storage [--volume D:] [--json]`: 记录日志的只读文件系统检查。
- `workflow-journal FILE.json`: 历史内容；退出代码 0 表示读取成功，而不是运行正常。
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: 显示或验证与主机无关的控件；不扫描，不修复。
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: 仅历史报告；不运行命令。

## 其他命令

- `<analyze|investigate> <text-log-path> [--json]`: 分析本地文本证据。
- `adapter-bindings`: 只读的核心协议绑定摘要；JSON 证据。
- `adapter-statistics`: 只读的适配器字节、错误和丢弃计数器；JSON 证据。
- `application-events`: 只读的最近 Application 错误、分组计数和 JSON 证据。
- `applocker-policy`: 只读的有效收集模式和规则汇总计数；排除规则详细信息；不更改。
- `common-check [--scope NAME ...]`: 默认包括所有诊断区域；指定时仅限所选区域；只读 JSON。
- `delete-notifications`: 只读的 NTFS 和 ReFS 删除通知配置；JSON；不优化，不更改设置。
- `diagnostic-scopes [--all]`: 列出 11 个建议的结果组，或全部 49 个只读区域；不收集。
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 专用诊断日志；不修复。
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: 所有诊断区域；除非明确指定 `--auto-repair`，否则不修复。
- `gateway-check`: 对已配置的确切默认下一跃点进行有范围限制的 ICMP 检查；不扫描地址。
- `infrastructure-service-events`: 读取一个有范围限制的确切连接、远程管理、通知和核心服务故障快照。
- `investigate-batch FILE [FILE ...]`: 最多 12 个文件；JSON Lines；导入失败或部分导入时退出代码为 3。
- `licensing-check`: 只读的 Windows 激活状态，不含产品密钥；JSON 证据。
- `mac-vendor PACK_PATH MAC_ADDRESS`: 按最长前缀进行本地知识包查找；不发出网络请求。
- `ncsi-policy`: 只读的 Windows 连接分类策略；排除探测目标。
- `neighbor-cache`: 只读的、有隐私范围限制的邻居可访问性证据；JSON 结果。
- `ntfs-self-healing DRIVE:`: 只读的所选卷联机 NTFS 修复配置；不修复，不更改设置。
- `pagefile-check`: 只读的分页文件管理、分配和使用摘要；排除完整路径。
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: 确切的本地知识包查找；不发出网络请求，不选择驱动程序。
- `physical-disks`: 只读的当前运行状况、操作、介质、总线和使用状态；JSON；不修复。
- `platform-service-events`: 读取一个有范围限制的确切应用程序、标识、配置文件和设备支持服务故障快照。
- `previous-windows [--execute --backup-confirmed]`: 默认预览；通过 DISM 永久删除 Windows 回滚功能。
- `previous-windows-recover`: 只读核对，不重复删除回滚。
- `provisioned-packages`: 本地 Windows 中为新用户暂存的包；不删除。
- `recipes [WORKFLOW]`: 列出已安装的定义或特定工作流程的依赖项和限制；不执行。
- `repair <dism|sfc> [--execute]`: 默认预览。
- `reserved-storage`: 只读的联机 Windows 保留存储状态；JSON 证据。
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: 默认预览；在新的检查后创建一个还原点。
- `restore-point-recover`: 验证待处理的请求，而不创建另一个还原点。
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: 仅预览。
- `shadow-copies`: 只读的当前 VSS 快照状态和属性；排除标识；不更改快照。
- `shadow-storage`: 只读的 VSS 差异区域分配和压力；JSON；不调整大小，不执行快照操作。
- `uac-policy`: 只读的 UAC 启用状态和提升提示策略；不含账户或凭据数据。
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: 本地 UEFI 注册表确切查找；不发出网络请求，不选择驱动程序。
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: 确切的本地知识包查找；不发出网络请求，不选择驱动程序。
- `vitals-check`: 只读的 Windows、硬件、账户、资源、电池和物理磁盘快照；JSON 证据。
- `win32-code DECIMAL_OR_0xHEX`: 离线查找已安装 Windows 的消息。
- `workflows`: 列出已安装工作流程的键和标题；不扫描，不修复。
