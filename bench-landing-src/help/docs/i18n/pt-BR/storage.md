# Armazenamento

Inspeciona discos, volumes, Espaços de Armazenamento e serviços de manutenção de unidades. O diagnóstico não seleciona nenhuma operação de disco; qualquer ação compatível que altere o computador exige um destino exato, pré-requisitos e confirmação próprios.

Guias passo a passo: [Verificar e reparar uma unidade](check-a-drive.html) e [Liberar espaço em disco](free-disk-space.html).

## Ferramentas desta seção

### Armazenamento

Mapeia de forma delimitada as identidades de discos, partições e volumes com letra de unidade, avalia a pressão de 10% de espaço livre em cada volume com letra de unidade, inspeciona a alocação de armazenamento de cópias de sombra do VSS e as cotas padrão de cada volume, resume o progresso atual dos trabalhos do módulo Storage, verifica o volume NTFS selecionado e o estado da autorrecuperação online, e inspeciona os dispositivos de disco e controladora deste PC. Limites de capacidade, configurações de cota e estados de trabalhos são evidências para revisar e não selecionam limpeza, redimensionamento, alteração de cotas nem controle de trabalhos. O mapeamento de letras de unidade é pontual e não cobre todos os caminhos de montagem. As constatações sobre dispositivos não são associadas ao volume selecionado. O volume do Windows é o padrão. Nenhum arquivo, instantâneo, trabalho ou driver é alterado, nenhuma configuração muda, nenhum volume é desmontado e nenhuma reinicialização é agendada.

### Espaços de Armazenamento

Inventaria os pools de Espaços de Armazenamento não primordiais e os discos virtuais, incluindo integridade, estado operacional, capacidade, alocação e resiliência, e retorna uma única conclusão operacional delimitada. O Windows pode iniciar sob demanda o provedor de gerenciamento de Espaços de Armazenamento para responder a esta consulta somente leitura. Pools, discos virtuais, discos físicos, volumes, resiliência, alocação, configurações de manutenção, política e tipo de inicialização são preservados. Nenhuma operação de pool, disco, volume, alocação, anexação, reparo, otimização ou configuração é solicitada. Os resultados do provedor não verificam a acessibilidade do pool, a redundância, a integridade dos dados, a recuperabilidade dos backups nem a E/S dos aplicativos.

### Serviço de Armazenamento

Revisa de forma delimitada os trabalhos atuais do módulo Storage, o histórico exato de falhas do serviço e a disponibilidade atual do Serviço de Armazenamento. Evidências incompletas têm prioridade. O estado dos trabalhos e as falhas históricas não podem autorizar a recuperação do serviço nem alterações de armazenamento. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As notificações de armazenamento existentes e a atividade de armazenamento gerenciada por política podem ser retomadas. Volumes, partições, sistemas de arquivos, pontos de montagem, dispositivos, drivers, configurações do Sensor de Armazenamento, política de manutenção, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de trabalho, volume, partição, ponto de montagem, arquivo, dispositivo, driver ou configuração de armazenamento. A verificação cobre só o estado estável do serviço, não o resultado dos trabalhos nem operações de dispositivo, volume ou política de armazenamento.

### Serviço Otimizar unidades

Verifica a disponibilidade do serviço Otimizar unidades. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. A atividade de otimização já agendada pelo Windows pode ser retomada conforme a política de manutenção atual. Volumes, sistemas de arquivos, dados, agendamentos de otimização, política de manutenção, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhum volume nem analisa, executa trim, desfragmenta, otimiza ou agenda nenhuma unidade. A verificação cobre só o estado estável do serviço, não a análise, a otimização nem a integridade do dispositivo.
