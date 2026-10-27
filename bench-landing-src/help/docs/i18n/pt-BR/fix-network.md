# Corrigir problemas de rede e internet

Use este guia quando um computador não tem internet, não consegue alcançar um servidor ou um nome, ou perde a conexão com frequência. O KillerBench verifica em ordem o adaptador, o endereço, o gateway, o DNS e o firewall, e só renova uma concessão ou inicia um serviço quando evidências atuais comprovam que é necessário.

## Verificar a conexão

1. Abra **Rede** e escolha **Rede**.
2. Para testar algo específico, preencha **Pesquisa de nome de host (opcional)**, ou **Destino TCP (opcional)** e **Porta TCP**. Deixe-os em branco para verificar só a configuração local. Nenhum dado de aplicativo é enviado.
3. Defina **Tempo limite por consulta ou conexão (segundos)** se a rede for lenta.
4. Deixe **Reparar automaticamente os problemas confirmados** desativado e pressione **Executar diagnóstico**.
5. Leia o relatório. Ele diferencia um endereço ausente, um gateway ausente ou inacessível, problemas de configuração de DNS, configurações de proxy, divergências no perfil do firewall e uma pesquisa ou conexão com falha, e indica a próxima etapa para cada caso.

Uma resposta do gateway não comprova acesso à internet, e uma pesquisa com falha pelo resolvedor configurado não significa que o site está fora do ar.

## Reparar o que o relatório encontrou

1. Execute **Rede** de novo com **Reparar automaticamente os problemas confirmados** ativado. Ele pode iniciar os serviços habilitados Cliente DNS, Cliente DHCP, cliente SMB, Reconhecimento de Local de Rede, Serviço de Lista de Redes e Auxiliar IP, e renovar adaptadores DHCP físicos conectados que não tenham um endereço IPv4 utilizável.
2. A conexão pode cair por um instante durante a renovação. Endereços estáticos e adaptadores virtuais nunca são alterados.
3. Para um único nome em cache incorreto, use a limpeza do cache DNS para aquele host exato em vez de apagar tudo.

Rotas, entradas do Hosts, servidores DNS, regras do firewall, provedores Winsock e perfis de rede são sempre preservados.

## Rede sem fio, autenticação com fio e proxies

- **Rede sem fio** verifica o adaptador e a Configuração Automática de WLAN, e pode iniciar o serviço se ele estiver parado.
- **Autenticação com fio (802.1X)** faz o mesmo com a Configuração Automática com Fio em redes que exigem autenticação na porta.
- **Proxy automático do WinHTTP** mostra as configurações de proxy salvas para o computador e para a conta atual. O KillerBench nunca baixa uma URL de PAC nem altera configurações de proxy durante o diagnóstico.

## Pela linha de comando

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Consulte [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) e [dns-cache](cli.html#cmd-dns-cache).

## Relacionados

- [Seção Rede](network.html)
- [Corrigir problemas de impressão](fix-printing.html)
- [Como as alterações continuam seguras](safety.html)
