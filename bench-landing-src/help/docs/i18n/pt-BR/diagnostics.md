# Diagnósticos

Diagnósticos é onde todo atendimento deve começar. Cada ferramenta lê o computador e gera um relatório. Nada aqui altera o Windows. A ferramenta padrão, a verificação completa do sistema, executa um amplo conjunto de verificações somente leitura e classifica o que precisa de atenção.

Guias passo a passo: [Investigar falhas e telas azuis](investigate-crashes.html) e [Reparar arquivos do sistema](repair-system-files.html).

## Ferramentas desta seção

Verifique o computador primeiro e depois leia no relatório o que precisa de atenção e a próxima etapa segura.

### Verificação completa do sistema

Inspeciona 49 áreas de diagnóstico e avalia as receitas de reparo instaladas com base nas evidências atuais. As verificações podem levar vários minutos. A verificação completa do sistema é apenas diagnóstica. Use um fluxo de reparo específico para selecionar e confirmar uma ação compatível depois de revisar os pré-requisitos atuais. Instantâneos históricos, de backup e de transferência continuam somente leitura. O conteúdo dos despejos não é aberto. A inspeção do bit sujo não agenda reparo nem reinicialização. Esta verificação não instala atualizações, não altera drivers, não exclui arquivos e não faz um teste completo de hardware.

### Sinais vitais

Captura a memória disponível, a integridade dos discos informada pelo provedor e o status da proteção do BitLocker, e relaciona os sinais vitais atuais com problemas de dispositivos, histórico de confiabilidade, registros de erros de hardware, evidências de falhas, desligamentos inesperados e resultados do Diagnóstico de Memória do Windows. Indicadores de reinicialização, espaço livre no disco do sistema, elevação e configuração do serviço de atualização continuam visíveis. Senhas de recuperação não são coletadas. Este fluxo de trabalho não executa reparos. O resultado combinado não é um teste completo de hardware, software ou estabilidade.

### Histórico de confiabilidade

Lê os registros do Monitor de Confiabilidade do Windows dos últimos 14 dias junto com os erros recentes de Aplicativo e os eventos de desligamento inesperado, e retorna uma única conclusão histórica delimitada. Registros históricos não estabelecem a integridade atual nem a causa raiz. Nenhum reparo é selecionado e nenhum aplicativo, serviço ou configuração é alterado.

### Desempenho da inicialização

Lê os metadados dos eventos de desempenho de inicialização e desligamento do Windows dos últimos 14 dias, mantendo só a identidade do provedor e campos numéricos de duração delimitados, e os relaciona com os indicadores de reinicialização pendente compatíveis. Caminhos, nomes de processos, contas e o restante da carga útil dos eventos são removidos antes de salvar as evidências. Registros históricos de degradação não identificam um gargalo atual nem autorizam alterações de inicialização, serviços, aplicativos, drivers, tarefas, energia, reinicialização ou política.

### Histórico de erros de hardware

Lê o histórico do Windows Hardware Error Architecture e do diagnóstico de memória dos últimos 14 dias, os problemas atuais de dispositivos Plug and Play, a integridade dos discos físicos e os contadores de confiabilidade do provedor, e retorna uma única conclusão de hardware delimitada. Os registros precisam ser relacionados com a carga útil, os diagnósticos do firmware e os testes de hardware do fabricante. Nenhum componente com defeito nem causa raiz é deduzido, nenhum reparo é selecionado, e nenhum hardware, firmware, driver, disco, pool, volume, arquivo, serviço, energia, reinicialização ou configuração é alterado.

### Histórico de instalação de drivers

Lê o final guardado do histórico de instalação de dispositivos do Windows SetupAPI, relaciona as seções com falha às identidades Plug and Play exatas atuais e retorna uma única conclusão delimitada. Seções históricas do log não comprovam que um dispositivo ainda está presente nem que uma falha é atual. Nenhum driver é instalado, removido, revertido ou atualizado.

### Pontos da Restauração do Sistema

Lista até 256 pontos da Restauração do Sistema disponíveis, revisa a elegibilidade explícita para criação e retorna uma única conclusão de prontidão delimitada. O inventário não comprova que a proteção está ativada em todos os volumes nem que uma restauração vai funcionar. Nenhum ponto de restauração é criado, excluído ou aplicado durante o diagnóstico.

### Ambiente de Recuperação do Windows

Lê a configuração atual do Ambiente de Recuperação do Windows com o REAgentC, relaciona um local habilitado com o disco e a partição exatos atuais e retorna uma única conclusão de prontidão delimitada. O reparo automático opcional pode habilitar uma configuração existente desabilitada e depois verificar o estado informado. A imagem de recuperação não é baixada, movida nem substituída, nenhuma inicialização de recuperação é iniciada e o Windows não é reiniciado. O status habilitado e a presença da partição não comprovam que a imagem de recuperação consegue iniciar ou reparar este PC.

### Ativação do Windows

Lê o status atual da licença do Windows sem coletar chaves do produto completas ou parciais, guarda os metadados anonimizados dos eventos de aviso, erro e críticos da Proteção de Software dos últimos sete dias e retorna uma única conclusão operacional delimitada. A carga útil dos eventos é removida antes de salvar as evidências. Registros históricos continuam como contexto quando a licença atual aparece como Licenciado. Nenhuma ação de ativação, chave do produto, token, licença, conta, serviço ou política é selecionada. O status do provedor e o histórico de eventos não comprovam direito de assinatura, acessibilidade do servidor de ativação, integridade do repositório de tokens, validade futura nem causa raiz.

### Serviços do Windows

Inventaria os nomes dos serviços instalados, os nomes de exibição, os estados, os modos de inicialização e os tipos de ação de recuperação limitados por privacidade, sem coletar contas, caminhos de executáveis, comandos de recuperação ou mensagens de reinicialização. Revisa a coerência dos modos de inicialização, as identidades e os ciclos das dependências diretas, repete os estados pendentes exatos, inspeciona os serviços compatíveis e as dependências dos serviços parados, e só relaciona falhas recentes do Gerenciador de Controle de Serviços quando há um nome de serviço exato ou um nome de exibição único. O uso de grupos de ordem de carregamento é contado sem expandir os membros, e identidades ambíguas ficam sem resolução. Serviços de inicialização sob demanda parados podem ser normais. Nenhum serviço é iniciado, e as configurações de inicialização, dependências e recuperação são preservadas. Use o fluxo de trabalho de Reparo correspondente quando um recurso específico precisar de recuperação.

### Erros de aplicativos

Lê os eventos recentes de erro e críticos de Aplicativo junto com um histórico delimitado do Monitor de Confiabilidade, e retorna uma única conclusão sobre o histórico de estabilidade. Registros históricos não comprovam uma falha atual nem uma causa raiz e só orientam um diagnóstico mais específico. Nenhum aplicativo é parado, reparado, removido ou reconfigurado.

### Dispositivos de vídeo

Inspeciona adaptadores gráficos e monitores em busca de erros de dispositivo informados, IDs de hardware e versões de drivers instaladas. Sem instalação de drivers, redefinição de dispositivos ou alteração de resolução. A qualidade da imagem e as conexões físicas não são testadas.

### Dispositivos de entrada

Inspeciona erros de teclado, mouse e dispositivos HID e o inventário de drivers. Nenhuma tecla é capturada, nenhum dispositivo é redefinido e nenhum driver é alterado. O funcionamento físico da entrada não é testado.

### Problemas de dispositivos

Encontra códigos de erro de configuração Plug and Play diferentes de zero em todas as classes de dispositivos e relaciona as evidências dos dispositivos presentes com o histórico delimitado do SetupAPI sobre instalação de drivers, configuração de dispositivos e Plug and Play. Registros históricos sozinhos não estabelecem uma falha atual. Revise a identidade exata do dispositivo e as evidências de drivers aprovados sem alterar hardware, drivers ou configuração. Dispositivos ausentes e o funcionamento físico não são verificados.
