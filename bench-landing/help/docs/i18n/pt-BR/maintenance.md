# Manutenção

Manutenção mantém o computador organizado: limpeza de arquivos, perfis de usuário antigos, tarefas agendadas e organização do Windows. Cada ferramenta mostra o que removeria antes de qualquer exclusão.

Guias passo a passo: [Liberar espaço em disco](free-disk-space.html) e [Deixar o Windows mais privado](make-windows-private.html).

## Ferramentas desta seção

Limpe arquivos, perfis e tarefas agendadas e organize o Windows.

### Limpeza

Estima os destinos selecionados de arquivos temporários, caches e evidências de diagnóstico guardadas usando as configurações de idade e de limite de arquivos do painel principal. Windows Temp e Windows SystemTemp são destinos independentes. A Otimização de Entrega é uma opção de cache separada, e o conteúdo removido pode precisar ser baixado de novo. A remoção permanente opcional usa a caixa de limpeza separada, verifica de novo os candidatos guardados dentro do limite selecionado e registra cada resultado. A exclusão não passa pela Lixeira. Os destinos, a idade e o limite de arquivos também podem ser configurados pelo comando cleanup da CLI.

### Perfis

Revisa os perfis do Windows usando o limite de idade selecionado, os registros exatos do ProfileList e, opcionalmente, as dependências de contas de serviços e tarefas agendadas, e retorna uma única conclusão de revisão delimitada. Perfis carregados, do sistema e da conta atual ficam fora da revisão. Os candidatos permanecem apenas em revisão. Nenhum perfil, pasta, chave do registro, conta, serviço, tarefa, credencial, política ou arquivo é alterado, e nenhuma remoção é selecionada. As dependências de aplicativos e a recuperabilidade dos backups continuam sem verificação.

### Tarefas agendadas

Inventaria as tarefas agendadas visíveis, os estados e as entidades indicadas. Nenhuma tarefa é iniciada, registrada ou alterada. Ações, gatilhos e sucesso da execução não são verificados.

### Organizar o Windows

Revisa áreas explícitas de limpeza do Windows como Recall e IA, telemetria, sugestões de pesquisa, conteúdo para consumidores, barra de tarefas e Iniciar, notificações, Edge, energia, hibernação, idiomas do Office e software pré-instalado, para a conta em execução ou para todos os perfis. A revisão é somente leitura. O plano de energia de alto desempenho, os temporizadores de suspensão e de tela e a hibernação podem ser alterados um de cada vez quando essa área é a única selecionada, com evidências atuais e confirmação explícita.
