# Aplicativos

Aplicativos revisa programas instalados, pacotes da Store, Office e Outlook, extensões de navegador, aplicativos de inicialização e pesquisa. As ferramentas de remoção e redefinição sempre mostram uma revisão primeiro e só alteram o que você selecionar.

Guias passo a passo: [Configurar um computador novo](set-up-new-pc.html).

## Ferramentas desta seção

Revise aplicativos instalados, pacotes da Store, Office e Outlook, extensões de navegador e pesquisa.

### Aplicativos instalados

Lê os registros de desinstalação de aplicativos no escopo selecionado do computador ou da conta em execução e combina esse escopo com evidências delimitadas de erros de Aplicativo, do Monitor de Confiabilidade e de reinicializações. Outras contas e aplicativos portáteis são excluídos. Registros e histórico amplo não são prova exata da integridade nem da propriedade de um aplicativo. Nenhuma ação de instalador, desinstalador, aplicativo, processo, serviço, arquivo, registro, conta, política ou reinicialização é selecionada.

### Pacotes de aplicativos

Inventaria os pacotes Appx/MSIX registrados no escopo de conta selecionado. A conta em execução é o padrão; todos os usuários exige acesso de administrador. Informa identidades exatas, sinalizadores de framework e de não removível, e a quantidade de dependências. Pacotes provisionados, aplicativos Win32, propriedade gerenciada e dados de aplicativos não são avaliados. Nenhum pacote é removido.

### Extensões de navegador

Inventaria de forma delimitada as identidades das extensões do Chrome, Edge, Brave e Firefox da conta em execução. Histórico do navegador, credenciais, configurações das extensões, conteúdo dos manifestos, comandos e valores de política são excluídos. A presença não comprova ativação, confiança, propriedade nem que a remoção é segura. Nenhum navegador, extensão ou política é alterado.

### Aplicativos de inicialização

Inventaria os nomes de registro Run e RunOnce do computador e da conta em execução, os nomes de arquivos da pasta Inicializar, os estados de aprovação de inicialização do Windows e os nomes e estados das tarefas agendadas de inicialização ou entrada, e retorna uma única conclusão sobre o ambiente de inicialização. Linhas de comando, caminhos de destino, carimbos binários de aprovação, ações das tarefas, argumentos, contas e credenciais não são coletados. Um registro não comprova que um item foi executado, causou atraso, é indesejado ou pode ser desativado com segurança. O estado de aprovação não comprova que existe um registro correspondente. Nenhuma alteração de registro, arquivo, aplicativo, tarefa, serviço ou política é selecionada.

### Remoção de aplicativos da Microsoft

Revisa os registros selecionados do OneDrive, Outlook, Office/M365, Project e Teams com opções de escopo de conta e de retenção. Os inventários do computador, da conta em execução e dos pacotes registrados são comparados; a revisão para todos os usuários inclui também os pacotes provisionados para novos usuários. Um pacote compatível da conta atual pode receber um plano de proteção exato e separado no aplicativo de desktop ou na CLI. Remover esse único pacote é uma ação de desktop protegida e separada, com confirmações próprias. A revisão em si não altera nenhum processo, aplicativo, pacote, arquivo, credencial, local de pasta, valor do registro ou dado de usuário.

### Redefinição do Outlook

Revisa o novo Outlook, o Outlook clássico ou ambos, com opções explícitas para caches, configurações de perfil, autenticação, arquivos OST, suplementos e reinstalação do novo Outlook. A revisão da conta em execução verifica locais conhecidos delimitados, registros exatos e se os processos estão fechados. Uma ação separada e confirmada, limitada ao cache, guarda os diretórios de cache exatos com nomes de recuperação únicos e verifica cada movimentação. A revisão para todos os usuários e as outras áreas de redefinição continuam apenas em revisão. Nenhum cache guardado, perfil, arquivo OST, credencial, suplemento, pacote ou configuração é removido.

### Inventário do Foxit

Inventaria os produtos Foxit instalados no computador e os valores do registro do atualizador nas duas exibições. Instalações de usuário, política efetiva e propriedade dos produtos não são verificadas. Nenhum aplicativo ou configuração é alterado.

### Serviço Windows Installer

Verifica as políticas de restrição, reversão, elevação, registro em log e depuração do Installer, os marcadores de operações em andamento, os binários do mecanismo e a visibilidade dos pacotes em cache registrados antes da recuperação do serviço. Relaciona resultados recentes com falha e com sucesso apenas para identidades de produto exatas informadas. Se os pré-requisitos forem atendidos, o reparo automático pode iniciar o serviço habilitado e verificar que está Em execução. Resultados históricos, registro em log, visibilidade do cache e constatações de depuração não autorizam alterações de pacotes ou políticas. Políticas, logs, marcadores de transação, tipos de inicialização e pacotes em cache são preservados. A instalação de um pacote específico não é testada.

### Windows Search

Verifica a disponibilidade do serviço Windows Search, a atividade atual do SystemIndex e o histórico recente de eventos do Search. O reparo automático pode iniciar um serviço habilitado e parado e verificar que está em execução. Uma opção explícita e separada de reconstrução do índice guarda o índice anterior, inicia um índice novo e verifica o estado exato do serviço e do diretório. Locais indexados, política, tipo de inicialização e dados guardados são preservados. Iniciar o serviço ou reconstruir o índice pode retomar a indexação em segundo plano. A completude dos resultados de pesquisa continua sem teste.

### Preparação de Aplicativos

Verifica a disponibilidade do serviço Preparação de Aplicativos. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. A primeira entrada e o provisionamento de aplicativos existentes podem ser retomados conforme a política atual. Pacotes, registros, licenças, perfis de usuário, dados de aplicativos, política e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de pacotes. A verificação cobre só o estado estável do serviço, não a entrada, o provisionamento nem a abertura de aplicativos.

### Serviço de Repositório de Estado

Verifica a disponibilidade do Serviço de Repositório de Estado. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As solicitações existentes de estado do modelo de aplicativos podem ser retomadas. Registros, pacotes, licenças, perfis, dados do navegador e de aplicativos, conteúdo do repositório, política e tipo de inicialização são preservados. Nenhuma redefinição, novo registro ou exclusão é solicitado. A verificação cobre só o estado estável do serviço, não a consistência do repositório nem o funcionamento dos aplicativos.

### Serviço de Implantação de AppX

Verifica a disponibilidade do Serviço de Implantação de AppX. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As solicitações existentes de implantação, registro ou manutenção podem ser retomadas conforme a política atual. Pacotes, registros, licenças, perfis, dados de aplicativos, conteúdo do repositório, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de pacotes. A verificação cobre só o estado estável do serviço, não a implantação, o registro nem a abertura de aplicativos.

### Serviço de Licença de Cliente

Verifica a disponibilidade do Serviço de Licença de Cliente. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As solicitações existentes de licença de aplicativos da Store podem ser retomadas conforme a política atual. Pacotes, registros, licenças, tokens, contas, perfis, dados de aplicativos, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de pacote, licença, token ou conta. A verificação cobre só o estado estável do serviço, não a validação de licenças nem a abertura de aplicativos.

### Serviço de Instalação da Microsoft Store

Verifica o histórico recente de instalações da Microsoft Store e a disponibilidade exata do serviço de instalação da Store. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As solicitações existentes de download e instalação da Store podem ser retomadas conforme a política atual. Pacotes, registros, licenças, contas, dados de aplicativos, estado dos downloads, origens, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de pacote, download, licença, conta, origem ou instalação. A verificação cobre só o estado estável do serviço, não os resultados de downloads, instalações, licenças ou aplicativos.

### Serviço Gerenciador de Licenças do Windows

Verifica a disponibilidade do Serviço Gerenciador de Licenças do Windows. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As verificações existentes de licença de aplicativos podem ser retomadas conforme a política atual. Pacotes, registros, licenças, tokens, contas, dados de aplicativos, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de pacote, licença, token, conta, registro ou direito de uso. A verificação cobre só o estado estável do serviço, não a validação de licenças, os direitos de uso nem a abertura de aplicativos.

### Serviço PushToInstall do Windows

Verifica a disponibilidade do Serviço PushToInstall do Windows. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. Uma solicitação de instalação remota já aprovada pode ser retomada conforme a política atual. Pacotes, registros, licenças, contas, dados de aplicativos, solicitações na fila, origens, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não cria, aprova nem seleciona nenhuma solicitação, pacote, origem, conta ou instalação. A verificação cobre só o estado estável do serviço, não o processamento das solicitações, o download, a instalação nem os resultados dos aplicativos.
