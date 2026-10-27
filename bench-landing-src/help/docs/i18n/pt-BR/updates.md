# Atualizações

Atualizações abrange o Windows Update, os serviços por trás dele e o repositório de componentes. Você pode ver o que está instalado, o que está pendente e por que uma atualização falha, e revisar as atualizações disponíveis antes de escolher quais instalar.

Guias passo a passo: [Corrigir o Windows Update](fix-windows-update.html) e [Reparar arquivos do sistema](repair-system-files.html).

## Ferramentas desta seção

Acompanhe a atividade de manutenção, revise as atualizações disponíveis e repare a cadeia do Windows Update.

### Monitor de atividade de manutenção

Observa de forma delimitada a atividade de manutenção do Windows Update e do Setup em um ou mais instantâneos. As opções controlam a frequência de atualização e se incluem eventos de atualização, histórico de atualizações, a tarefa agendada exata do Windows Update, estados exatos dos serviços, indicadores de reinicialização, transferências em segundo plano da conta atual, atividade da Otimização de Entrega e atividade do Windows Installer limitada por privacidade. IDs de processos, linhas de comando, usuários e caminhos são excluídos. A atividade não comprova progresso, conclusão, travamento nem falha. Nenhuma ação de atualização, instalação, transferência, tarefa, serviço, processo, política, reinicialização ou reparo é solicitada.

### Atualizações disponíveis

Descobre as atualizações de software e drivers selecionadas usando a origem configurada, o Windows Update ou um serviço Microsoft Update já registrado. A disponibilidade da origem é verificada antes da descoberta. A descoberta não instala nada, não aceita licenças, não registra origens e não altera políticas. As atualizações selecionadas podem ser instaladas depois, separadamente, somente após você marcar a caixa de confirmação. É separado do reparo de manutenção.

### Windows Update

Inventaria de forma delimitada os estados dos pacotes de manutenção, dos recursos opcionais e das capacidades do Windows, verifica os indicadores de reinicialização, o espaço livre e os serviços de atualização, observa a Otimização de Entrega sem iniciá-la e depois examina o repositório de componentes e os arquivos protegidos do sistema. Estados pendentes de pacotes, recursos ou capacidades pedem revisão, mas não autorizam nenhuma ação nos componentes. O reparo automático pode iniciar os serviços parados Windows Update, BITS, Serviços de Criptografia, Instalador de Módulos do Windows e Update Orchestrator que estão configurados para iniciar automaticamente. Um serviço de início sob demanda parado é relatado, não iniciado. Iniciar o Update Orchestrator pode retomar a atividade de atualização agendada existente conforme a política atual. Pacotes, recursos opcionais, capacidades, cache da Otimização de Entrega, pares, largura de banda, política, registro da origem, agendamentos e configurações de inicialização são preservados. O DISM ou o SFC exigem evidência em tempo real de corrupção. Este fluxo de trabalho não seleciona nem aprova nenhuma atualização.

### Serviço Windows Update

Verifica o estado exato do Serviço Windows Update, a DLL do serviço registrada, a atividade de atualização atual delimitada e o histórico exato de falhas do serviço. Observações da DLL, atividade de atualização e falhas históricas não podem autorizar o início do serviço nem uma operação de atualização. A recuperação opcional só inicia um serviço automático parado após novas verificações de dependências e do registro do executável. Um serviço de inicialização sob demanda parado continua apenas informativo. A atividade de atualização existente pode ser retomada conforme a política atual. Política de atualização, registro da origem, agendamentos, configurações de reinicialização, pacotes, trabalhos do BITS, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona, aprova nem instala nenhuma atualização. A verificação cobre só o estado estável do serviço, não a acessibilidade da origem, a aplicabilidade, o download, a instalação, a conclusão nem a prontidão para reiniciar.

### Otimização de Entrega

Verifica o estado exato do serviço Otimização de Entrega. Um serviço de inicialização sob demanda parado continua apenas informativo. Só um estado exato Pausado pode oferecer recuperação. A recuperação opcional repete a verificação do estado, envia uma única solicitação de continuação delimitada e exige duas observações coerentes Em execução. Tipo de inicialização, conteúdo do cache, pares, largura de banda, origem das atualizações e política gerenciada são preservados. Atividade de transferência, integridade do cache, progresso das atualizações e conclusão continuam sem verificação.

### Serviço BITS

Verifica o estado exato do Serviço de Transferência Inteligente em Segundo Plano, os trabalhos de transferência da conta atual e o histórico exato e delimitado de falhas do serviço. Falhas históricas e instantâneos de transferência não podem autorizar o início do serviço. A recuperação opcional só inicia o serviço exato quando ele está parado e configurado para iniciar automaticamente, após novas verificações de dependências. Um serviço de início sob demanda parado permanece apenas informativo. Os aplicativos existentes podem retomar transferências em segundo plano na fila. Trabalhos, prioridades, proprietários, destinos, configurações de proxy, política e tipo de inicialização são preservados. A verificação cobre só o estado estável do serviço, não o progresso das transferências, a propriedade dos aplicativos nem a conclusão.

### Transferências em segundo plano (BITS)

Lê os trabalhos de transferência do BITS no escopo de conta selecionado e revisa as identidades exatas dos trabalhos. Os nomes das filas não estabelecem a qual aplicativo pertencem. Nenhuma transferência é retomada, concluída, cancelada, excluída ou reatribuída.

### Serviços de Criptografia

Verifica o estado exato dos Serviços de Criptografia, a DLL do serviço registrada e o histórico exato e delimitado de falhas do serviço. Falhas históricas e observações da DLL não podem autorizar o início do serviço nem a substituição de componentes. A recuperação opcional só inicia o serviço exato quando ele está parado e configurado para iniciar automaticamente, após novas verificações de dependências. Um serviço de início sob demanda parado permanece apenas informativo. As operações existentes de catálogo, certificados e atualizações podem ser retomadas. Certificados, chaves, catálogos, configurações de confiança, política e tipo de inicialização são preservados. A verificação cobre só o estado estável do serviço, não a validação de certificados, assinaturas, catálogos nem da confiança.

### Instalador de Módulos do Windows

Verifica o estado exato do Instalador de Módulos do Windows, as transições delimitadas dos pacotes de manutenção e o histórico exato de falhas do serviço. Falhas históricas e estados dos pacotes não podem autorizar o início do serviço nem uma operação de pacotes. A recuperação opcional só inicia o serviço exato quando ele está parado e configurado para iniciar automaticamente, após novas verificações de dependências. Um serviço de início sob demanda parado permanece apenas informativo. A atividade de manutenção existente pode ser retomada. Pacotes, conteúdo do repositório de componentes, origens, política e tipo de inicialização são preservados. A verificação cobre só o estado estável do serviço, não a integridade do repositório de componentes, a aplicabilidade dos pacotes, a conclusão da manutenção nem a prontidão para reiniciar.

### Serviço Windows Update Medic

Verifica o estado exato do Serviço Windows Update Medic, a DLL do serviço registrada, a atividade de atualização atual delimitada e o histórico exato de falhas do serviço. Observações da DLL, atividade de atualização e falhas históricas não podem autorizar o início do serviço nem uma ação nos componentes. Um serviço de inicialização sob demanda parado pode ser normal. Quando esta ferramenta exata é selecionada, a recuperação opcional só pode iniciar o serviço habilitado e parado após novas verificações de dependências. A correção do Windows pode ser retomada conforme a política atual e pode reparar componentes de atualização ou disparar atividade de atualização. Política de atualização, registro da origem, agendamentos, pacotes, trabalhos do BITS, configurações de reinicialização, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma atualização nem altera nenhuma configuração de atualização. A verificação cobre só o estado estável do serviço, não a correção, a integridade dos componentes, a aplicabilidade, o download, a instalação nem a prontidão para reiniciar.

### Serviço Update Orchestrator

Verifica o estado exato do Serviço Update Orchestrator, a tarefa interna exata do Windows Update, a atividade atual delimitada e o histórico exato de falhas do serviço. Resultados da tarefa, atividade de atualização e falhas históricas não podem autorizar o início do serviço nem uma operação de atualização. A recuperação opcional só inicia um serviço automático parado após novas verificações de dependências e do registro do executável. Um serviço de inicialização sob demanda parado continua apenas informativo. A atividade de atualização agendada existente pode ser retomada conforme a política atual e pode baixar ou instalar atualizações já aprovadas. Política de atualização, registro da origem, agendamentos, configurações de reinicialização, pacotes, trabalhos do BITS, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nem aprova nenhuma atualização. A verificação cobre só o estado estável do serviço, não a aplicabilidade, o download, a instalação, a conclusão nem a prontidão para reiniciar.

### Limpeza do repositório de componentes

Analisa se o Windows recomenda a limpeza do repositório de componentes. A manutenção opcional remove imediatamente os componentes substituídos e analisa de novo. Sem ResetBase nem exclusão manual no WinSxS. Sem desfazer automático; o espaço em disco recuperado não é medido.
