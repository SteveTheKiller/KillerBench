# Reparo

Reparo reúne as ferramentas que podem corrigir algo. Cada uma faz o diagnóstico primeiro. Um reparo só é oferecido quando evidências atuais mostram o problema exato que ele corrige, e só é executado depois que você ativa **Reparar automaticamente os problemas confirmados** ou confirma um reparo individual. Cada reparo é registrado no diário e verificado em seguida.

Guias passo a passo: [Reparar arquivos do sistema](repair-system-files.html) e [Corrigir o Windows Update](fix-windows-update.html).

## Reparo automático e manual

**Reparar automaticamente os problemas confirmados** permite que uma ferramenta de diagnóstico execute, após a verificação, os reparos que ela comprovar serem necessários. Fica desativado por padrão.

Para um único reparo de manutenção, abra **Avançado: logs e reparos manuais** e escolha **Reparos**. Escolha **Repositório de componentes (DISM)** ou **Arquivos do sistema (SFC)**, leia a visualização, marque **Executar este reparo neste PC** e pressione **Executar reparo**. Consulte [Como as alterações continuam seguras](safety.html) para saber o que acontece antes e depois de qualquer alteração.

## Ferramentas desta seção

Use estas ferramentas depois que um diagnóstico identificar um problema. Os reparos exigem evidências confirmadas e aprovação explícita.

### Serviços essenciais do Windows

Verifica em conjunto os serviços Energia, Plug and Play, Gerenciador de Usuários, Cliente de Política de Grupo, Interface de Serviço de Armazenamento de Rede, Mapeador de Ponto de Extremidade RPC, Chamada de Procedimento Remoto, Iniciador de Processos de Servidor DCOM, Serviço de Infraestrutura de Tarefas em Segundo Plano e CoreMessaging. A recuperação opcional só inicia serviços automáticos habilitados exatos que evidências atuais confirmam estar parados após a janela de inicialização e verificações completas de dependências. Serviços de inicialização sob demanda não são alterados quando estão ociosos. Planos de energia, dispositivos, drivers, contas, credenciais, perfis, políticas, adaptadores, endereços, rotas, DNS, configuração do firewall, registros, pontos de extremidade, registros de tarefas, dados de aplicativos, permissões dos serviços e tipos de inicialização são preservados. Nenhuma atualização de política, varredura de dispositivos, ação de conta, alteração de rede, alteração de energia, alteração de registro ou início de tarefa é solicitado. A verificação cobre só o estado estável dos serviços, não o comportamento completo dos recursos.

### Arquivos do sistema

Inventaria de forma delimitada os estados dos pacotes de manutenção, dos recursos opcionais e das capacidades do Windows, verifica os pré-requisitos, examina os arquivos protegidos do sistema e só escolhe o SFC automaticamente quando a verificação em tempo real confirma corrupção. Estados pendentes de pacotes, recursos ou capacidades pedem revisão, mas não autorizam instalação, remoção, limpeza, reversão, ativação nem desativação.

### Repositório WMI

Verifica a disponibilidade do serviço WMI e depois a consistência do repositório. O reparo automático pode iniciar um serviço parado elegível e recuperar corrupção confirmada, e depois verificar cada resultado. As configurações de serviço desabilitado são preservadas. Registros legíveis são mesclados; conteúdo ilegível pode exigir a reinstalação do provedor. Clientes de gerenciamento podem ser interrompidos. Sem redefinição, exclusão ou reversão automática.

### Notificações do sistema do Windows

Lê o histórico recente do Gerenciador de Controle de Serviços e depois verifica em conjunto o Sistema de Eventos COM+ e o Serviço de Notificação de Eventos do Sistema. A recuperação opcional verifica se o Sistema de Eventos COM+ está pronto antes de iniciar o SENS. As notificações existentes de entrada, rede, energia e COM+ podem ser retomadas com a configuração atual. Catálogos COM+, assinaturas, registros de notificações, consumidores, contas, permissões, configurações de recuperação, política e tipos de inicialização são preservados. Nenhuma assinatura, registro, consumidor, conta, permissão ou política é alterado. A verificação cobre só o estado estável dos serviços, não a entrega completa das notificações.

### Serviço de Política de Diagnóstico

Revisa o histórico operacional delimitado da Política de Diagnóstico, o histórico exato de falhas do serviço e a disponibilidade atual do serviço. Evidências incompletas têm prioridade. Registros históricos não podem autorizar a recuperação do serviço nem uma ação de solução de problemas. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. Os cenários de diagnóstico existentes podem ser retomados. Política, dados de solução de problemas e tipo de inicialização são preservados. Nenhum solucionador de problemas ou correção sugerida é executado. A verificação cobre só o estado estável do serviço, não a detecção de falhas, a conclusão do diagnóstico nem o resultado de um reparo.

### Relatório de Erros do Windows

Revisa a configuração de despejos de memória, os metadados delimitados dos arquivos de despejo, os relatórios recentes de falhas do sistema, o histórico exato de falhas do serviço Relatório de Erros do Windows e a disponibilidade atual do serviço. Relatórios históricos não estabelecem uma falha atual nem uma causa raiz e não podem autorizar uma recuperação. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. Os relatórios existentes na fila podem ser retomados conforme a política atual. Arquivos de relatórios, despejos, consentimento, política de envio e tipo de inicialização são preservados. Nenhum envio ou exclusão é solicitado. A verificação cobre só o estado estável do serviço, não a criação, o envio ou a entrega dos relatórios, a resolução de problemas nem o diagnóstico de falhas.

### Coletor de Eventos do Windows

Revisa o inventário delimitado de assinaturas do Coletor de Eventos do Windows, o histórico exato de falhas do serviço e a disponibilidade atual do serviço. Um inventário vazio pode ser intencional, e a quantidade de assinaturas ou as falhas históricas não podem autorizar uma recuperação. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As assinaturas existentes podem retomar a coleta remota de eventos. Assinaturas, eventos encaminhados, credenciais, configurações de canais, política e tipo de inicialização são preservados. A verificação cobre só o estado estável do serviço, não a acessibilidade das origens, a autenticação, a entrega remota, a atualidade dos eventos nem a integridade das assinaturas.

### Log de Eventos do Windows

Inspeciona quatro canais de diagnóstico quanto a ativação, modo de retenção, tamanho atual, limite configurado, quantidade de registros e saturação, e depois verifica a disponibilidade do serviço Log de Eventos do Windows. O reparo automático pode iniciar um serviço habilitado e parado depois de verificar as dependências e confirmar que está em execução. Uma ação específica confirmada separadamente pode ativar um único canal de diagnóstico exato desabilitado e não gerenciado, preservando todas as outras configurações informadas. Logs, permissões dos canais, retenção, limites de tamanho, tipos de inicialização e configurações gerenciadas são preservados. A entrega bem-sucedida de eventos e a capacidade de arquivamento continuam sem verificação.

### Serviço de Tempo do Windows

Verifica a configuração, o serviço e a sincronização do Tempo do Windows. O reparo automático pode iniciar o W32Time quando o status atual informa o erro exato de serviço parado e as novas verificações de política, modo e dependências são aprovadas, e depois solicitar uma ressincronização e verificar o novo status. Isso pode ajustar o relógio usando as origens já configuradas. Tipo de inicialização, configuração das origens e configurações gerenciadas são preservados.

### Serviço Agendador de Tarefas

Inventaria de forma delimitada o estado das tarefas agendadas, os resultados de execução e a quantidade de execuções perdidas, e combina isso com o histórico exato de falhas do serviço Agendador de Tarefas antes de verificar a disponibilidade atual do serviço. Constatações sobre tarefas e falhas históricas do serviço não podem autorizar o início do serviço. A recuperação opcional só inicia um serviço exato habilitado e parado após novas verificações de dependências. As tarefas habilitadas existentes podem começar com as contas configuradas, incluindo gatilhos perdidos. Nenhuma definição de tarefa, entidade, referência de credencial, política ou tipo de inicialização é alterado, e nenhuma tarefa é iniciada ou excluída diretamente. A verificação cobre só o estado estável do serviço, não o sucesso das ações, a validade das credenciais nem os resultados futuros das tarefas.

### Serviço de Perfil de Usuário

Verifica a disponibilidade do Serviço de Perfil de Usuário. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As solicitações existentes de entrada e de perfil podem ser retomadas. Pastas de perfil, hives do registro, permissões, registros da lista de perfis, credenciais, dados de usuário, política, permissões do serviço e tipo de inicialização são preservados. Nenhuma operação de perfil é solicitada. A verificação cobre só o estado estável do serviço, não a entrada, o carregamento de perfis nem o acesso aos dados de usuário.

### Serviços de backup

Verifica os serviços Cópias de Sombra de Volume, Provedor de Cópia de Sombra de Software da Microsoft, Backup do Windows e Serviço do Mecanismo de Backup em Nível de Bloco, e depois lê o status dos gravadores do VSS, os tipos de provedor registrados, os instantâneos atuais e a alocação do armazenamento de cópias de sombra. Serviços de inicialização sob demanda parados podem ser normais quando estão ociosos. A recuperação opcional só inicia serviços automáticos parados após verificações de dependências e depois confirma que estão em execução. O trabalho de backup agendado ou interrompido existente pode ser retomado. Configurações de inicialização desabilitada e sob demanda, instantâneos, armazenamento de cópias de sombra, registros de provedores, planos de backup, catálogos, agendamentos, destinos, credenciais e retenção são preservados. Nenhum backup é selecionado ou reconfigurado, nenhum serviço é reiniciado e nenhum instantâneo ou catálogo é excluído. O registro dos provedores, o status dos gravadores, a alocação do armazenamento e o estado estável dos serviços não verificam a conclusão do backup, o acesso ao destino nem o sucesso da restauração.
