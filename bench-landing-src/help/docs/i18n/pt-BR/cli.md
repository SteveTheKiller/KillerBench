# Linha de comando

O `killerbench-cli` executa o mesmo Engine do aplicativo de desktop pelo PowerShell ou por um console remoto. Todo comando que poderia alterar o computador mostra uma visualização por padrão e exige uma opção explícita, como `--execute`, antes de agir. Os comandos imprimem as evidências JSON na saída padrão e o progresso no erro padrão.

Guias passo a passo: [Executar o KillerBench por scripts](use-the-cli.html).

O código de saída 0 significa que a visualização ou o resultado verificado foi bem-sucedido; 1, um erro de inicialização, de entrada ou do diário; 2, que os argumentos não foram reconhecidos; 3, bloqueado, com falha ou não verificado; 4, que um fluxo de trabalho precisa de reinicialização antes de poder verificar; e 130, que você o interrompeu.

Execute `killerbench-cli` sem argumentos para imprimir esta lista no seu próprio computador.

## Atualizações e manutenção

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Revisão exata de uma transferência ou observação com dois instantâneos; sem nova tentativa.
- `bits-jobs [--all-users]`: Estados, proprietários e contagens de bytes dos trabalhos do BITS; sem alterações nas transferências.
- `bits-retry JOB_GUID [--execute]`: Visualização por padrão; nova tentativa explícita de um trabalho exato, sem afirmar que o aplicativo concluiu.
- `bits-retry-recover JOB_GUID`: Dois instantâneos somente leitura; arquiva os resultados estáveis sem outra tentativa.
- `component-repair-source`: Política gerenciada da origem de reparo do DISM e conflitos; sem alterações.
- `delivery-optimization`: Estado do serviço Otimização de Entrega somente leitura; evidências JSON.
- `delivery-optimization-activity`: Contagens atuais de trabalhos e totais de bytes limitados por privacidade; sem identidades de transferências nem alterações.
- `delivery-optimization-policy`: Configurações clássicas e de Policy CSP somente leitura; valores sensíveis e identidades de provedores excluídos.
- `feature-service-events`: Lê o histórico exato de falhas dos serviços de hora, pesquisa, WMI, log de eventos e áudio.
- `feature-update-policy`: Política de versão de destino, adiamento, pausa e proteção somente leitura; evidências JSON.
- `pending-file-operations`: Resumo das operações de arquivo pendentes limitado por privacidade, somente leitura; evidências JSON.
- `restart-check`: Origens de reinicialização pendente somente leitura; evidências JSON.
- `servicing-logs`: Análise delimitada do log CBS mais recente, dos arquivos CBS em texto simples e compactados e dos logs do DISM e do Windows Setup; sem reparo.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Instantâneos da atividade de manutenção somente leitura.
- `servicing-service-events`: Históricos exatos de falhas da cadeia de manutenção do Windows Update, somente leitura.
- `servicing-service-states`: Estado atual dos seis serviços da cadeia de manutenção do Windows Update, somente leitura.
- `setup-code DECIMAL_OR_0xHEX`: Referência documentada dos códigos de resultado do Windows Setup; sem inspeção do computador nem reparo.
- `update-applicability`: Evidências de edição, compilação e arquitetura do Windows, somente leitura.
- `update-cache`: Totais delimitados do cache SoftwareDistribution somente leitura; sem nomes de arquivos.
- `update-code DECIMAL_OR_0xHEX`: Referência documentada dos códigos de resultado do Windows Update; sem inspeção do computador nem reparo.
- `update-context`: Evidências da elevação atual e da sessão de desktop somente leitura; sem identificadores de conta.
- `update-engine-files`: Evidências de presença, versão e assinatura do mecanismo nativo do Windows Update, somente leitura.
- `update-events`: Histórico do cliente do Windows Update dos últimos sete dias, somente leitura; JSON com as evidências brutas.
- `update-followup DECIMAL_OR_0xHEX`: Novos diagnósticos somente leitura escolhidos por um código de resultado documentado do Windows Update.
- `update-history`: Os 100 resultados mais recentes de operações do Windows Update, somente leitura; evidências JSON.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Repita `--select`; visualização por padrão.
- `update-policy`: Política gerenciada do Windows Update somente leitura; evidências JSON.
- `update-policy-surfaces`: Evidências da superfície de configuração clássica e de Policy CSP somente leitura; valores e identidades de provedores excluídos.
- `update-recover`: Verifica uma atualização pendente; nunca a instala nem tenta de novo.
- `update-restart-policy`: Prazos de conformidade, períodos de carência, supressão de reinicialização e comportamento das notificações, somente leitura.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: Descoberta e correspondências em JSON; sem instalação.
- `update-service-events`: Histórico exato de falhas do serviço Windows Update, somente leitura.
- `update-service-files`: Evidências exatas de registro e assinatura da DLL do serviço Windows Update, somente leitura.
- `update-services`: Registros dos serviços do Windows Update somente leitura; evidências JSON.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Condicionado a uma nova pesquisa; visualização por padrão; mantém o repositório antigo.
- `update-store-reset-recover`: Reconcilia uma redefinição de repositório preparada sem repeti-la.
- `windows-capabilities`: Estados das capacidades do Windows online somente leitura; resultado JSON.
- `windows-update-task`: Estado exato e histórico de execução da tarefa Scheduled Start, somente leitura; sem execução de tarefa nem operação de atualização.

## BitLocker e TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Visualização por padrão; ativa o desbloqueio automático de um volume de dados criptografado.
- `bitlocker-auto-unlock-recover`: Verifica uma solicitação de desbloqueio automático interrompida sem repeti-la.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Visualização por padrão; somente um arquivo novo de backup de recuperação.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Verifica um backup interrompido sem gravá-lo de novo.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Visualização por padrão; inicia a descriptografia, por exemplo antes de criptografar de novo com um método obrigatório.
- `bitlocker-decrypt-recover`: Verifica uma solicitação de descriptografia interrompida sem repeti-la.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Visualização por padrão; exige protetores TPM e de recuperação existentes.
- `bitlocker-encrypt-recover`: Verifica o estado de conversão pendente sem repetir a solicitação.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Visualização por padrão; faz backup de uma senha de recuperação existente no Active Directory ou no Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Somente visualização das opções.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Visualização por padrão; adiciona um protetor TPM ou de senha de recuperação; a senha nunca é impressa.
- `bitlocker-protector-recover`: Verifica uma solicitação de protetor interrompida sem repeti-la.
- `bitlocker-protectors DRIVE:`: Somente identidades e tipos de protetores; sem senhas de recuperação.
- `bitlocker-resume DRIVE: [--execute]`: Visualização por padrão; somente um volume exato totalmente criptografado e suspenso.
- `bitlocker-resume-recover`: Verifica o estado de proteção pendente sem repetir a solicitação.

## Segurança e privacidade

- `certificate-chains`: Confiança de cadeia local apenas com cache para certificados pessoais, somente leitura; sem revogação nem downloads.
- `defender-overview [--report]`: Visão geral da proteção, do agendamento de verificações e das ameaças agregadas, somente leitura; sem ação.
- `defender-protection [--report]`: Preferências de proteção e superfícies de política reconhecidas, somente leitura; detalhes sensíveis excluídos; sem alterações.
- `defender-scan Quick|Full [--execute] [--report]`: Visualização por padrão; uma única verificação explícita e protegida do Microsoft Defender.
- `defender-scan-history [--report]`: Horários das verificações rápida e completa e status de atraso, somente leitura; detalhes sensíveis excluídos; sem verificação.
- `defender-scan-recover [--report]`: Reconciliação somente leitura sem iniciar outra verificação.
- `defender-threats [--report]`: Histórico agregado de ameaças, sinalizadores ativos atuais e IDs de gravidade; detalhes sensíveis excluídos; sem ação.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Visualização por padrão; somente perfis locais não gerenciados.
- `firewall-enable-recover`: Inspeciona uma alteração pendente sem repeti-la.
- `firewall-events`: Lê o histórico operacional delimitado de avisos, erros e eventos críticos do Firewall do Windows.
- `firewall-logging`: Lê a configuração de log de tráfego dos perfis Domínio, Privado e Público sem abrir os logs.
- `firewall-rules`: Lê contagens de regras efetivas e de origens de política limitadas por privacidade.
- `firewall-service-events`: Lê o histórico exato de falhas do BFE e do Firewall do Windows Defender no Gerenciador de Controle de Serviços.
- `privacy-clipboard-history on|off [--execute]`: Visualização por padrão; valor exato do usuário atual com a política preservada.
- `privacy-clipboard-history-recover`: Verifica de novo uma alteração interrompida sem repeti-la.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Restauração protegida do valor anterior.
- `privacy-plan --select Camera --select ClipboardSync`: Revisa somente as seleções; nenhuma configuração é alterada.
- `privacy-setting KEY on|off [--execute]`: Visualização por padrão; alteração do usuário atual que preserva a política; chaves inválidas listam as opções compatíveis.
- `privacy-setting-recover KEY`: Verifica de novo uma alteração interrompida sem repeti-la.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Restauração protegida do valor exato.
- `security-check`: TPM, Inicialização Segura, proteções de virtualização e política do UAC somente leitura; evidências JSON.

## Serviços

- `service-binaries`: Presença dos binários de serviços locais e status do Authenticode, somente leitura; caminhos excluídos e caminhos não locais não contatados.
- `service-check NAME [NAME ...] | service-check --all`: Estados dos serviços compatíveis e evidências de dependências, somente leitura.
- `service-dependencies`: Revisão da integridade das dependências diretas somente leitura; sem alterações nos serviços.
- `service-dlls`: Presença das DLLs de serviço registradas e status do Authenticode, somente leitura; caminhos excluídos e caminhos não locais não contatados.
- `service-events`: Histórico de falhas dos serviços somente leitura; JSON com as evidências brutas.
- `time-events`: Histórico recente de eventos do Tempo do Windows somente leitura, com evidências JSON.
- `service-logons`: Revisão da classe de conta dos serviços e da interação com a área de trabalho, somente leitura; identidades de contas excluídas.
- `service-path-quoting`: Revisão das aspas nos caminhos dos executáveis de serviços, somente leitura; caminhos completos e argumentos excluídos.
- `service-recovery-policy`: Resumo das ações de recuperação dos serviços instalados, somente leitura; comandos e mensagens de reinicialização excluídos.
- `service-startup-config`: Revisão da coerência dos modos de inicialização somente leitura; sem alterações nos serviços.
- `service-transitions`: Observação repetida dos estados pendentes dos serviços, somente leitura; sem ação nos serviços.
- `service-triggers`: Resumo dos gatilhos dos serviços instalados, somente leitura; cargas úteis dos gatilhos excluídas; sem alterações.

## Armazenamento, arquivos e backup

- `backup-events`: Histórico do VSS e do volsnap; evidências JSON; sem alterações.
- `backup-service-events`: Lê o histórico exato de falhas do VSS, do provedor de cópias de sombra de software, do Backup do Windows e do mecanismo em nível de bloco.
- `backup-summary`: Resumo das operações locais do Backup do Windows; sem alterações nos backups.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Visualização por padrão; a Lixeira é tudo ou nada e a idade não se aplica.
- `disk-cleanup-task`: Estado exato e histórico de execução da tarefa, somente leitura; sem execução de tarefa nem limpeza.
- `disk-diagnostic`: Relaciona as tarefas exatas de diagnóstico de disco ao histórico de eventos; sem ação em tarefas ou discos.
- `disk-diagnostic-tasks`: Histórico exato das tarefas de coleta e resolução, somente leitura; sem ação em tarefas ou discos.
- `disk-quotas`: Cotas padrão e estado de cada volume, somente leitura; identidades de usuários excluídas; sem alterações.
- `file-history-events`: Contagens de avisos e erros do Histórico de Arquivos com projeção de privacidade; sem alterações.
- `file-history-status`: Estado de proteção do Histórico de Arquivos da conta atual; sem alterações.
- `search-index-rebuild [--execute]`: Visualização por padrão; mantém o índice anterior e verifica o novo estado.
- `search-index-rebuild-recover`: Reconciliação somente leitura sem repetir os comandos de reparo.
- `storage-check DRIVE_LETTER`: Verificação fixa do NTFS somente leitura; resultados JSON e progresso em tempo real.
- `storage-events`: Histórico de armazenamento somente leitura; JSON com as evidências brutas.
- `storage-jobs`: Estado e progresso agregados dos trabalhos do módulo Storage, somente leitura; JSON; sem alterações.
- `storage-reliability`: Contadores de temperatura, desgaste e E/S somente leitura; JSON; sem reparo.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Visualização por padrão; a execução exige confirmação de backup; volumes do sistema são agendados para a reinicialização.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Somente visualização; sem reparo nem agendamento.
- `storage-repair-recover`: Verifica um reparo do sistema de arquivos concluído ou agendado sem repeti-lo.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Coleta e vincula as evidências atuais; sem reparo nem agendamento.
- `storage-sense-policy`: Política de limpeza reconhecida do computador, somente leitura; configurações de usuário e identidades de arquivos excluídas.
- `storage-sense-task`: Estado exato e histórico de execução da tarefa, somente leitura; sem execução de tarefa nem limpeza.
- `storage-service-events`: Lê o histórico exato de falhas do Serviço de Armazenamento, do Otimizar unidades, do VSS e do provedor de cópias de sombra de software.
- `volume-capacity DRIVE:`: Espaço disponível somente leitura, com evidências JSON.
- `volume-dirty DRIVE:`: Status do bit sujo somente leitura; sem reparo nem reinicialização.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Análise somente leitura por padrão; exige um volume exato íntegro e mídia compatível.
- `volume-optimize-recover`: Verifica uma operação pendente concluída sem repeti-la.
- `vss-providers`: Nomes, versões e tipos dos provedores do VSS, somente leitura; identificadores excluídos; sem alterações de registro.
- `vss-writers`: Estados informados dos gravadores; interpretação da saída em inglês; sem redefinição.

## Impressão

- `print-events`: Histórico de administração do PrintService dos últimos sete dias, somente leitura; evidências JSON.
- `print-extension-files`: Resumo de presença e assinatura dos binários de processadores e monitores de impressão, somente leitura; caminhos completos excluídos.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Visualização por padrão; somente um trabalho exato em Erro ou Bloqueado.
- `print-job-cancel-recover [--folder PATH]`: Verifica de novo um cancelamento interrompido sem repeti-lo.
- `print-job-list`: Lista os trabalhos visíveis atuais sem alterá-los.
- `print-operational-events`: Estado do canal operacional do PrintService e histórico disponível, somente leitura; evidências JSON.
- `print-policy`: Restrições do Apontar e Imprimir e política de instalação de drivers, somente leitura; evidências JSON.
- `print-rendering-policy`: Política de renderização e de isolamento de drivers somente leitura; evidências JSON.
- `print-spooler-events`: Falhas do Gerenciador de Controle de Serviços ligadas à identidade local do Spooler de Impressão, somente leitura; evidências JSON.
- `printer-check --queue NAME [--queue NAME ...]`: Configurações da fila local selecionada, somente leitura.
- `printer-conditions`: Códigos de condição das filas locais somente leitura; sem verificação física nem alterações.
- `printer-driver-files`: Resumo de presença dos arquivos registrados e de assinatura dos executáveis, somente leitura; caminhos completos excluídos.
- `printer-driver-models`: Resumo da versão 3, versão 4 e compatibilidade com pacotes, somente leitura; evidências JSON.
- `printer-drivers`: Inventário e correspondência dos registros de filas e drivers, somente leitura; evidências JSON.
- `printer-endpoint-check`: Conexões delimitadas aos pontos de extremidade exatos atribuídos de impressoras TCP/IP padrão; sem descoberta, SNMP nem dados de impressão.
- `printer-list`: Descobre as filas locais sem alterar configurações.
- `printer-ports`: Correspondência dos registros de filas e portas, somente leitura; evidências JSON.
- `printer-queue-attributes`: Atributos de spool, retenção e compartilhamento das filas locais, somente leitura.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Visualização por padrão; desativar o modo offline pode liberar os trabalhos em espera.

## Rede

- `dhcp-check`: Elegibilidade de concessões DHCP ausentes ou expiradas, somente leitura; evidências JSON, sem renovação.
- `dhcp-events`: Histórico de eventos do Cliente DHCP para IPv4 e IPv6, somente leitura; resultado JSON.
- `dhcp-leases`: Evidências de carimbo de data e hora, expiração e servidor da concessão, somente leitura; resultado JSON.
- `dhcp-renew [--execute]`: Visualização por padrão; somente concessões DHCP exatas ausentes ou expiradas de adaptadores físicos conectados.
- `dhcp-renew-recover`: Reconciliação somente leitura sem solicitar outra concessão.
- `dns-cache HOST [--execute]`: Visualização por padrão; limpeza protegida do cache local com verificação do host exato.
- `dns-cache-recover`: Verifica de novo uma operação de cache interrompida sem repetir a limpeza.
- `dns-cache-summary`: Status do cache DNS local e contagens por tipo de registro, limitados por privacidade; sem alterações.
- `dns-check HOST [--timeout-seconds 1..60]`: Resolução de nomes do sistema delimitada; resultado JSON.
- `dns-client-config`: Evidências de sufixo, registro, devolução e política, somente leitura; resultado JSON.
- `dns-client-events`: Histórico de avisos e falhas do Cliente DNS limitado por privacidade, somente leitura; resultado JSON.
- `dns-nrpt-policy`: Evidências da política efetiva de namespace DNS, somente leitura; resultado JSON.
- `ip-addresses`: Revisão do estado, da origem e dos conflitos dos endereços IP atuais, somente leitura; evidências JSON.
- `ip-interface-config`: Evidências de configuração das interfaces IP somente leitura; resultado JSON.
- `network-firewall`: Relaciona as atribuições de rede ativas à ativação do perfil de firewall correspondente.
- `network-offload`: Configuração global de descarregamento de rede do Windows somente leitura; resultado JSON.
- `network-profiles`: Categorias atuais dos perfis e classificações de conectividade, somente leitura; nomes de redes excluídos.
- `network-service-events`: Lê o histórico exato de falhas de DHCP, DNS, Estação de Trabalho, NLA, Serviço de Lista de Redes e Auxiliar IP.
- `proxy-check`: Evidências do proxy do WinHTTP salvo e do proxy de Internet da conta atual, somente leitura.
- `route-priority`: Comparação da métrica efetiva da rota padrão, somente leitura; evidências JSON.
- `smb-client-config`: Configuração de assinatura, acesso de convidado e transporte do SMB, somente leitura.
- `smb-connections`: Negociação de protocolo ativa somente leitura; identidades remotas excluídas.
- `smb-server-config`: Configuração de protocolo, assinatura, criptografia e multicanal do SMB, somente leitura.
- `smb-shares`: Inventário delimitado de compartilhamentos SMB somente leitura; caminhos, permissões, sessões e contas excluídos.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Verificação de conexão delimitada; nenhum dado de aplicativo enviado.
- `tcp-port-pressure`: Intervalos TCP dinâmicos e uso agregado de portas locais, somente leitura; pontos de extremidade e processos excluídos.
- `tcp-settings`: Evidências de configuração dos modelos TCP do Windows, somente leitura; resultado JSON.
- `vpn-profiles`: Configuração de perfis de VPN da conta atual somente leitura; credenciais excluídas.
- `winrm-listeners`: Resumo do registro dos ouvintes do WinRM somente leitura; impressões digitais de certificados excluídas.
- `winsock-catalog`: Estrutura do catálogo nativo de protocolos do Winsock, somente leitura; resultado JSON.
- `winsock-namespaces`: Estrutura nativa de provedores de namespace do Winsock, somente leitura; resultado JSON.
- `winsock-provider-files`: Presença e assinaturas dos arquivos de provedores de protocolo, somente leitura; resultado JSON.
- `winsock-readiness`: Prontidão nativa de protocolos, provedores de namespace e arquivos de provedores do Winsock, somente leitura; resultados JSON.

## Aplicativos

- `app-folder-review FOLDER_NAME [--report]`: Revisão de pastas exatas com raiz fixa; sem exclusão.
- `app-package-review FULL_NAME [--all-users]`: Destino exato e pacotes dependentes; sem remoção.
- `app-packages [--all-users]`: Somente inventário de Appx/MSIX registrados; sem remoção.
- `browser-extensions`: Revisão das identidades de extensões do Chrome, Edge, Brave e Firefox; sem remoção.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Somente revisão; sem alterações no sistema.
- `depot-review [application and configuration selections] [--json]`: Somente revisão; sem provisionamento.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Verificação do Authenticode e do signatário do fabricante, somente leitura; nada é executado.
- `foxit-inventory`: Inventário de produtos do computador somente leitura; saída JSON.
- `installed-apps [--current-user]`: Registros de desinstalação e sinalizadores de capacidade; sem remoção.
- `installed-apps-compare BEFORE.json AFTER.json`: Diferenças de registro em relação a um inventário salvo; sem alterações no computador.
- `installed-hotfixes`: Inventário delimitado de hotfixes do Windows instalados, somente leitura; evidências JSON.
- `installer-activity`: Evidências de processos do Installer e marcadores de transação, somente leitura; evidências JSON.
- `installer-check`: Política do Installer e prontidão do mecanismo, somente leitura; evidências JSON.
- `installer-engine-files`: Resumo de presença e Authenticode dos arquivos exigidos pelo Installer, somente leitura; evidências JSON.
- `installer-events`: Histórico de MSI dos últimos sete dias somente leitura; evidências JSON.
- `installer-logging`: Política de log e depuração do Installer, somente leitura; evidências JSON.
- `installer-pending-state`: Correlação entre operações de arquivo pendentes e atividade do Installer, somente leitura; evidências JSON.
- `installer-registration-repair [--execute]`: Visualização por padrão; somente um defeito exato de registro do serviço; nunca cancela o registro do mecanismo.
- `installer-registration-repair-recover`: Verifica de novo um registro interrompido sem repetir os comandos.
- `installer-security-policy`: Política de segurança e de patches do Installer, somente leitura; evidências JSON.
- `installer-service-events`: Falhas do Gerenciador de Controle de Serviços ligadas à identidade local do Windows Installer, somente leitura; evidências JSON.
- `installer-service-registration`: Resumo do registro do serviço Windows Installer, somente leitura; evidências JSON.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Nova visualização da elegibilidade exata; sem provedor de remoção.
- `mace-package-recover`: Reconcilia de novo um registro interrompido de remoção de pacote de aplicativo; nunca repete a remoção.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Somente revisão; sem remoção.
- `msi-cache`: Visibilidade do cache registrado do Windows Installer, somente leitura; evidências JSON.
- `msi-code NONNEGATIVE_DECIMAL`: Referência documentada dos códigos de retorno do Windows Installer; sem inspeção do computador nem reparo.
- `msi-product {PRODUCT-CODE}`: Estado exato do registro de MSI somente leitura; evidências JSON.
- `msi-repair {PRODUCT-CODE} [--execute]`: Visualização por padrão; somente um produto exato danificado ou mal configurado.
- `msi-repair-recover`: Verifica de novo o reparo interrompido de um produto exato sem repeti-lo.
- `msi-sources`: Metadados de origem registrados do Windows Installer limitados por privacidade, somente leitura; evidências JSON.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Visualização por padrão; guarda os caches exatos da conta em execução com nomes de recuperação.
- `orca-cache-reset-recover`: Verifica a retenção de cache interrompida sem repetir uma movimentação.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Somente revisão; sem alterações no Outlook.
- `startup-apps`: Revisão dos registros de inicialização e das tarefas de inicialização ou entrada; sem desativação.
## Sistema

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Consulta local exata de atribuições do Bluetooth SIG; sem solicitação de rede.
- `crash-config`: Modo e caminhos de despejo do Windows salvos; sem alterações.
- `crash-dumps`: Metadados dos arquivos de despejo padrão do Windows; sem análise de conteúdo nem alterações.
- `crash-events`: Relatórios recentes de erros do sistema; sem diagnóstico nem alterações.
- `crash-review [--knowledge PACK_PATH ...]`: Até 32 pacotes; revisão combinada de falhas, desligamentos, estrutura delimitada de minidespejos e, opcionalmente, da referência exata de verificação de bugs; sem alterações.
- `default-printer`: Seleção padrão e configuração de gerenciamento automático, somente leitura; evidências JSON.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Visualização por padrão; somente quando a padrão está ausente ou em conflito.
- `default-printer-set-recover [--folder PATH]`: Verifica de novo uma seleção interrompida sem repeti-la.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Evidências de dispositivos somente leitura com referências exatas opcionais de drivers de até 32 pacotes.
- `device-code DECIMAL_OR_0xHEX`: Referência offline dos códigos de problema do Gerenciador de Dispositivos.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Somente candidatos de referência.
- `event-channel-enable CHANNEL [--execute]`: Visualização por padrão; ativa um único canal de diagnóstico exato não gerenciado e preserva as outras configurações.
- `event-channel-enable-recover`: Reconciliação somente leitura sem repetir o comando de ativação.
- `event-channels`: Estado, retenção, tamanho e saturação de quatro canais de diagnóstico, somente leitura.
- `font-registrations`: Verificação delimitada de registros e presença de arquivos do computador e do usuário atual, somente leitura.
- `memory-diagnostic-results`: Histórico delimitado do Diagnóstico de Memória do Windows somente leitura; evidências JSON.
- `minidump-analyze PATH`: Metadados estruturais delimitados de minidespejos; sem páginas de memória, símbolos ou alterações.
- `name-rename PROPOSED_NAME [--execute]`: Visualização por padrão; somente PCs independentes de grupo de trabalho; nunca reinicia o Windows.
- `name-rename-recover`: Verifica uma renomeação pendente sem repeti-la.
- `name-review PROPOSED_NAME`: Revisão da identidade e do nome proposto, somente leitura.
- `profiles --recover`: Verifica a ausência registrada e guarda as evidências de recuperação; nunca tenta a exclusão de novo.
- `profiles --recover-backup`: Verifica e arquiva um artefato de backup pendente; nunca tenta a cópia de novo.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Visualização primeiro; a cópia nativa protegida exige execução explícita e um destino NTFS local inexistente.
- `profiles --select SID --create-backup-artifact DESTINATION`: Compara um backup já copiado e cria o artefato fixo dele; não copia nem remove nenhum perfil.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Revisão somente leitura; a verificação exige um SID selecionado.
- `task-check`: Inventário de tarefas, constatações classificadas e evidências JSON, somente leitura.
- `task-definition TASK_FOLDER TASK_NAME`: XML exato da tarefa somente leitura; indisponível não significa ausente.
- `task-inventory`: Identidades e estados das tarefas somente leitura; saída JSON.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Revisão histórica da recuperação; nunca autoriza nova tentativa.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Valida e revisa um rascunho de tarefa; sem registro.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Visualização por padrão; definição desabilitada somente de criação com Sistema Local.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Reconciliação somente leitura da definição exata; nunca tenta de novo nem executa uma tarefa.
- `task-xml PLAN.json YYYY-MM-DD`: XML de revisão desabilitado na saída padrão; somente agendamentos de calendário no fuso horário local; sem registro.
- `time-config`: Evidências do modo, dos pares e da política do Tempo do Windows, somente leitura.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Visualização por padrão; o modo personalizado exige pares.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Somente visualização.
- `time-recover --folder PATH`: Verifica e arquiva um registro pendente; nunca tenta as alterações de novo.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Somente leitura; o modo personalizado exige pares.
- `wmi-provider`: Consulta local do provedor do sistema operacional, somente leitura; evidências JSON.

## Conhecimento, validação e Engine

- `knowledge PACK_PATH CONTEXT CODE`: Consulta de referência local; sem execução de reparos.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Ativação local verificada; sem download.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Verifica o pacote local ativo; sem alterações.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Somente alterações de referência; sem instalação de pacotes.
- `knowledge-info PACK_PATH`: Metadados do pacote e contagens de entradas por contexto; sem alterações.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Anexa referências contextuais aos códigos observados.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Seleciona o pacote guardado verificado mais recente; sem download.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Até 50 correspondências de texto de referência; sem execução de reparos.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Resolve uma identidade exata em até 32 pacotes; conflitos nunca são selecionados.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Verifica todos os bancos de dados instalados configurados; sem download nem ativação.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Busca e ativa de forma atômica bancos de dados candidatos assinados nos pontos de extremidade HTTPS configurados.
- `open <file.kbench> [--json]`: Reabre uma captura salva e a avalia de novo com as regras instaladas.
- `scan --output <new-file.kbench> [--json]`: Captura do Windows Update somente leitura salva em um arquivo novo.
- `validation-coverage RECORD.json [...]`: Resume as evidências nativas em VM da versão atual; sem diagnósticos nem reparos.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Plano de cenários em VMs descartáveis somente leitura; sem diagnósticos nem reparos.
- `validation-record RECORD.json`: Valida um registro delimitado de evidências nativas em VM; sem diagnósticos nem reparos.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Relatório local de cobertura em VM da versão atual.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Executa um plano de opções compartilhado validado e somente leitura.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Somente inventário.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Diário de diagnóstico específico; sem reparos.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Somente revisão registrada no diário.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Estado exato opcional do produto incluído no diário.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Diagnóstico opcional do destino selecionado incluído no diário.
- `workflow storage [--volume D:] [--json]`: Verificação do sistema de arquivos somente leitura registrada no diário.
- `workflow-journal FILE.json`: Conteúdo histórico; saída 0 significa leitura bem-sucedida, não integridade.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Mostra ou valida controles independentes de host; sem verificação nem reparo.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Somente relatório histórico; nenhum comando executado.

## Outros comandos

- `<analyze|investigate> <text-log-path> [--json]`: Analisa evidências de texto locais.
- `adapter-bindings`: Resumo das associações dos protocolos principais, somente leitura; evidências JSON.
- `adapter-statistics`: Contadores de bytes, erros e descartes dos adaptadores, somente leitura; evidências JSON.
- `application-events`: Erros recentes de Aplicativo, contagens agrupadas e evidências JSON, somente leitura.
- `applocker-policy`: Modos de coleta efetivos e contagens agregadas de regras, somente leitura; detalhes das regras excluídos; sem alterações.
- `common-check [--scope NAME ...]`: Todas as áreas de diagnóstico por padrão; somente as áreas selecionadas quando especificadas; JSON somente leitura.
- `delete-notifications`: Configuração de notificação de exclusão do NTFS e do ReFS, somente leitura; JSON; sem otimização nem alteração de configuração.
- `diagnostic-scopes [--all]`: Lista 11 grupos de resultados recomendados, ou todas as 49 áreas somente leitura; sem coleta.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Diário de diagnóstico específico; sem reparos.
- `full-scan [--recipes ID[,ID]] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Todas as áreas de diagnóstico; apenas diagnóstico, sem reparos.
- `gateway-check`: Verificações ICMP delimitadas dos próximos saltos padrão exatos configurados; sem varredura de endereços.
- `infrastructure-service-events`: Lê um instantâneo delimitado e exato de falhas dos serviços de conectividade, gerenciamento remoto, notificação e serviços essenciais.
- `investigate-batch FILE [FILE ...]`: Até 12 arquivos; JSON Lines; saída 3 para importações com falha ou parciais.
- `licensing-check`: Estado de ativação do Windows sem chaves do produto, somente leitura; evidências JSON.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Consulta no pacote local pelo prefixo mais longo; sem solicitação de rede.
- `ncsi-policy`: Política de classificação de conectividade do Windows, somente leitura; destinos de sondagem excluídos.
- `neighbor-cache`: Evidências de acessibilidade de vizinhos limitadas por privacidade, somente leitura; resultado JSON.
- `ntfs-self-healing DRIVE:`: Configuração de reparo online do NTFS do volume selecionado, somente leitura; sem reparo nem alteração de configuração.
- `pagefile-check`: Resumo do gerenciamento, da alocação e do uso do arquivo de paginação, somente leitura; caminhos completos excluídos.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Consulta exata no pacote local; sem solicitação de rede nem seleção de driver.
- `physical-disks`: Estado atual de integridade, operacional, de mídia, de barramento e de uso, somente leitura; JSON; sem reparo.
- `platform-service-events`: Lê um instantâneo delimitado e exato de falhas dos serviços de aplicativos, identidade, perfil e suporte a dispositivos.
- `previous-windows [--execute --backup-confirmed]`: Visualização por padrão; remove de forma permanente a capacidade de reversão do Windows pelo DISM.
- `previous-windows-recover`: Reconciliação somente leitura sem repetir a remoção da reversão.
- `provisioned-packages`: Pacotes preparados para novos usuários no Windows local; sem remoção.
- `recipes [WORKFLOW]`: Lista as definições instaladas ou as dependências e os limites de um fluxo de trabalho específico; sem execução.
- `repair <dism|sfc> [--execute]`: Visualização por padrão.
- `reserved-storage`: Estado do armazenamento reservado do Windows online, somente leitura; evidências JSON.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Visualização por padrão; cria um ponto após novas verificações.
- `restore-point-recover`: Verifica uma solicitação pendente sem criar outro ponto.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Somente visualização.
- `shadow-copies`: Estados e atributos atuais dos instantâneos do VSS, somente leitura; identidades excluídas; sem alterações nos instantâneos.
- `shadow-storage`: Alocação e pressão da área diferencial do VSS, somente leitura; JSON; sem redimensionamento nem ação nos instantâneos.
- `uac-policy`: Ativação do UAC e política do aviso de elevação, somente leitura; sem dados de conta ou credenciais.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Consulta local exata no registro de UEFI; sem solicitação de rede nem seleção de driver.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Consulta exata no pacote local; sem solicitação de rede nem seleção de driver.
- `vitals-check`: Instantâneos do Windows, do hardware, da conta, dos recursos, da bateria e dos discos físicos, somente leitura; evidências JSON.
- `win32-code DECIMAL_OR_0xHEX`: Consulta offline de mensagens no Windows instalado.
- `workflows`: Lista as chaves e os títulos dos fluxos de trabalho instalados; sem verificação nem reparo.
