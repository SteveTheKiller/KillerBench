# 解决网络和 Internet 问题

当计算机无法上网、无法访问服务器或名称，或者连接频繁断开时使用本指南。KillerBench 会依次检查适配器、地址、网关、DNS 和防火墙，并且只有在新证据证明确有必要时，才会续订租约或启动服务。

## 检查连接

1. 打开 **网络**，然后选择 **网络**。
2. 要测试特定目标，请填写 **主机名查找（可选）**，或者 **TCP 目标（可选）** 和 **TCP 端口**。若只检查本地配置，请留空。不会发送任何应用程序数据。
3. 如果网络较慢，请设置 **每次查找或连接的超时（秒）**。
4. 保持 **自动修复已确认的问题** 关闭，然后按 **运行诊断**。
5. 阅读报告。它会区分缺少地址、缺少网关或网关不可访问、DNS 配置问题、代理设置、防火墙配置文件不匹配以及查找或连接失败，并为每种情况给出下一步。

网关有响应并不能证明可以访问 Internet；通过已配置的解析程序查找失败，也不代表该网站已停止运行。

## 修复报告发现的问题

1. 开启 **自动修复已确认的问题**，再次运行 **网络**。它可以启动已启用的 DNS Client、DHCP Client、SMB 客户端、Network Location Awareness、Network List 和 IP Helper 服务，并为缺少可用 IPv4 地址的已连接物理 DHCP 适配器续订租约。
2. 续订期间连接可能会短暂断开。静态地址和虚拟适配器永远不会被更改。
3. 对于缓存中某个错误的名称，请使用针对该确切主机的 DNS 缓存刷新，而不是清除所有内容。

路由、Hosts 条目、DNS 服务器、防火墙规则、Winsock 提供程序和网络配置文件始终保持不变。

## 无线、有线登录和代理

- **无线网络** 会检查适配器和 WLAN AutoConfig，并在服务停止时启动它。
- **有线身份验证 (802.1X)** 会针对需要在端口登录的网络，对 Wired AutoConfig 执行相同的操作。
- **WinHTTP 自动代理** 会显示计算机和当前账户已保存的代理设置。KillerBench 在诊断期间绝不会获取 PAC URL 或更改代理设置。

## 通过命令行

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

请参阅 [workflow](cli.html#cmd-workflow)、[dns-check](cli.html#cmd-dns-check)、[tcp-check](cli.html#cmd-tcp-check)、[dhcp-renew](cli.html#cmd-dhcp-renew) 和 [dns-cache](cli.html#cmd-dns-cache)。

## 相关内容

- [网络分区](network.html)
- [解决打印问题](fix-printing.html)
- [如何保证更改安全](safety.html)
