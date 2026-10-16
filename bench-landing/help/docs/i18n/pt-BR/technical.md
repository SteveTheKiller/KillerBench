# A técnica do KillerBench

Um olhar por dentro de como o KillerBench lê um computador Windows, mantém as evidências honestas e decide quando uma alteração é permitida.

## Pilha tecnológica

O KillerBench é um aplicativo nativo do Windows. O aplicativo de desktop, a linha de comando e o Engine por baixo deles compartilham as mesmas evidências de diagnóstico e os mesmos fluxos de reparo.

| Componente | Detalhe |
| --- | --- |
| Interface | WPF no .NET 10 (`net10.0-windows`), Windows x64, moldura de janela personalizada |
| Engine | `Engine.Core`, `Engine` e `Engine.Windows`, sem nenhuma referência ao WPF ou a controles de desktop |
| Linha de comando | `killerbench-cli`, um host separado sobre o mesmo Engine |
| Provedores do Windows | .NET, APIs do Windows, PowerShell e ferramentas de linha de comando do Windows, cada um executado dentro de limites fixos de tempo, registros e tamanho |
| Relatórios | Saída bruta, Relatório legível, HTML, texto e JSON |
| Dados de referência | Pacotes de conhecimento versionados para códigos de erro, identificadores de hardware, fabricantes e referências de suporte |
| Empacotamento | Instalador: dependente de framework, exige o .NET Desktop Runtime 10 para Windows x64. Portátil: autocontido, sem instalação de runtime |
| Aparência | 13 temas da família e 19 idiomas |

## Instalação, pastas e dados

O KillerBench é distribuído em duas formas. O instalador é o download padrão e permite instalação por usuário ou para todos os usuários. A edição portátil traz o próprio runtime .NET e é executada sem instalação.

O KillerBench é executado com os direitos da conta que o iniciou. Ele não se eleva sozinho. Inicie-o com **Executar como administrador** quando evidências protegidas, como os protetores do BitLocker, ou um reparo exigirem acesso de administrador. A elevação nunca seleciona nem aprova um reparo.

### O que o instalador faz

O instalador verifica e extrai o aplicativo em `%LOCALAPPDATA%\Programs\KillerBench\`, ou em `%ProgramFiles%\KillerBench\` após uma solicitação de elevação para todos os usuários. Ele adiciona um atalho no menu Iniciar, um atalho opcional na área de trabalho e uma entrada em Adicionar ou remover programas com desinstalação normal e silenciosa. A edição portátil verifica e extrai o conteúdo em `%LOCALAPPDATA%\KillerBench\Portable\` para a execução atual e limpa as cópias abandonadas na inicialização seguinte.

### Onde cada coisa fica

| Local | Conteúdo |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Tema, cor de destaque, escala da interface e idioma |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Um diário JSON por execução de fluxo de trabalho: decisões, saída delimitada dos comandos e resultados dos reparos |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Diários de execução de reparos de manutenção como DISM e SFC |
| Pastas de recursos em `%LOCALAPPDATA%\KillerBench\` | Proteções pendentes e resultados de ações protegidas individuais, por exemplo `BitLockerBackup`, `Time`, `Firewall` e `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | O nível de reparo do KillerMCP escolhido em Sobre |
| `HKCU` ou `HKLM\Software\KillerBench` | O caminho de instalação, conforme o escopo da instalação |
| Um arquivo `.kbench` que você escolher | Uma captura salva com as evidências guardadas |

Nada é enviado. Exclua um diário só quando não precisar mais das evidências de um resultado incerto.

## Diagnóstico primeiro

O KillerBench trata diagnóstico e alteração como tarefas diferentes. O diagnóstico apenas lê. Uma alteração só acontece quando você escolhe um reparo ou uma ação específica, revisa as opções e os pré-requisitos e confirma.

A verificação completa do sistema é somente de diagnóstico, mesmo quando um fluxo de trabalho específico tem o reparo automático ativado. Capturas salvas, logs importados e eventos históricos nunca autorizam um reparo.

### Coleta delimitada

Cada comando nativo e cada provedor do Windows é executado dentro de limites fixos de tempo, registros e tamanho. Um provedor registra o código de saída, o tempo limite, o truncamento, a saída padrão e a saída de erro. Um provedor com falha ou truncado nunca pode ser lido como uma resposta limpa e vazia.

### Retenção mínima

Os coletores guardam o que uma constatação precisa. Muitos resultados mantêm nomes, categorias, contagens, estados e carimbos de data e hora, deixando de fora credenciais, linhas de comando, caminhos completos, identidades de contas, pontos de extremidade de rede e cargas úteis de eventos sem relação.

## Evidências que você pode acompanhar

Cada constatação está ligada às evidências que a geraram, e o relatório sempre separa o que foi comprovado do que foi apenas observado.

| Estado | Significado |
| --- | --- |
| Para revisar | As evidências sustentam um problema que merece atenção. |
| Informação | Evidências foram coletadas e são mostradas como contexto. |
| Incompletas ou Desconhecido | Uma verificação não pôde terminar ou a evidência estava indisponível. Nunca conta como integridade. |
| Histórico | Um evento do passado. Explica o histórico, mas não comprova uma falha atual. |
| Verificado | Um reparo ou alteração foi confirmado lendo o resultado de volta. |

### Desconhecido é um resultado

Desconhecido significa que o acesso foi negado, uma origem estava ausente, um provedor falhou, a saída estava malformada, um limite foi excedido ou as evidências discordavam. O KillerBench mostra essa limitação e a próxima etapa útil em vez de escondê-la.

### Histórico continua sendo histórico

Registros de eventos e logs importados descrevem o que foi registrado em um momento passado. Eles podem apontar para um código de erro ou para o próximo escopo de diagnóstico. Eles não comprovam que a falha ainda está ativa.

## Reparos protegidos

Os reparos são operações tipadas com regras de elegibilidade, execução, verificação e recuperação. Não são cadeias de comando tiradas de uma linha de log. Todo reparo compatível segue a mesma sequência:

1. Coletar evidências atuais completas.
2. Corresponder a uma receita compatível.
3. Revisar as opções exigidas e as consequências.
4. Confirmar os pré-requisitos administrativos e de política.
5. Coletar de novo as evidências decisivas logo antes da alteração.
6. Gravar um registro de preparação durável.
7. Executar uma única ação nativa delimitada.
8. Coletar novas evidências de verificação.
9. Registrar um resultado verificado, bloqueado, interrompido ou incerto.

Se as evidências decisivas mudarem antes de a ação ser executada, o KillerBench para sem executá-la.

### Escolhas explícitas

Reparos que dependem de uma opção exigem uma escolha exata sua, como um volume, uma fila de impressão, um destino para a chave de recuperação, um valor de energia, uma versão de pacote ou uma origem de reparo. O KillerBench nunca inventa esses valores.

### Computadores gerenciados

A política de grupo e o gerenciamento de dispositivos podem tornar uma alteração local ineficaz ou inadequada. Os reparos compatíveis verificam quem controla uma configuração e param quando esse controle é gerenciado ou desconhecido. Configurações controladas por política não são tocadas.

### Reparo automático

O reparo automático só deixa as receitas elegíveis prosseguirem. Ele não ignora evidências, não escolhe opções ausentes, não ignora políticas, não tenta de novo uma ação incerta, não reinicia o Windows e não transforma em reparo uma constatação que precisa de revisão.

## Diários e interrupções

Antes de pedir uma alteração ao Windows, o KillerBench anota o que vai fazer. Cada alteração é salva em um diário em `%LOCALAPPDATA%\KillerBench`.

Depois da alteração, o KillerBench lê o resultado de volta e só informa **Verificado** quando o Windows mostra o estado esperado. Se o Windows informar sucesso mas o estado não tiver mudado, o resultado informa isso e nada é repetido.

Um processo pode parar depois que o Windows recebe uma solicitação, mas antes de a verificação terminar. O registro pendente permanece, e uma reconciliação posterior lê o estado atual para descobrir o que aconteceu. A reconciliação não é uma reversão automática e nunca repete a alteração original. Guarde o diário quando o resultado for incerto.

## Constantes e limites

| Especificação | Valor |
| --- | --- |
| Cobertura do diagnóstico | A verificação completa do sistema executa 49 áreas de diagnóstico e verifica 96 receitas de reparo protegidas |
| Leitura de origens de texto | 1.000.000 de caracteres por origem, 2.000 linhas correspondentes exibidas |
| Fontes da sessão | 12 na barra lateral; uma captura comporta até 24 origens e 64 MB |
| Consultas de eventos | Os 200 eventos mais recentes por canal dos últimos 7 dias |
| Verificação do Windows Update | 15 segundos por origem, dois minutos no total |
| Investigação de logs | 10.000 entradas analisadas, 256 códigos distintos, 8 trechos de 320 caracteres por código |
| Arquivo CBS compactado | Arquivo de 256 MiB, membro de log de 512 MiB, extração de 30 segundos |
| CHKDSK somente leitura | Dez minutos; um reparo é limitado a 65 minutos |
| Verificações de DNS e TCP | Dez segundos por padrão, no máximo um minuto |
| Inventário de serviços | Até 1.024 serviços instalados |
| Pacotes de conhecimento | Até 32 abertos ao mesmo tempo |
| Revisão de arquivos grandes | Arquivos de pelo menos 100 MB |
| Monitor de atividade de manutenção | Intervalo de 1 a 60 segundos, de 1 a 120 instantâneos |
| Rascunhos de tarefas agendadas | Limite de execução de 1 a 4.320 minutos |
| Origens de hora personalizadas | Até oito pares NTP |
| Exportação de atividades | Até 150 entradas guardadas |
| Idiomas e temas | 19 idiomas, 13 temas com variações de destaque |

## O que ele não fará

- Limpar um TPM, excluir protetores que ele não criou ou exibir senhas de recuperação.
- Alterar uma configuração controlada pela política de grupo ou pelo gerenciamento de dispositivos.
- Executar um reparo só porque um evento histórico ou um relatório importado menciona um problema.
- Transformar evidências ausentes, parciais ou com falha em um veredito de integridade.

## Relatórios

Cada execução gera um relatório que você pode ler, filtrar e exportar. Um relatório separa:

1. O que foi inspecionado
2. Qual problema foi encontrado
3. Quais evidências sustentam a constatação
4. Qual ação foi proposta ou executada
5. Como o resultado foi verificado
6. O que continua desconhecido ou precisa de reinicialização

**Saída bruta** mantém as evidências técnicas com os números de linha originais, para que você possa rastrear qualquer constatação até a origem. O **Relatório legível** resume a mesma execução em linguagem simples, com a próxima etapa segura em primeiro lugar. Os relatórios HTML usam o sistema de temas do Killer Tools e continuam legíveis sem o aplicativo. O texto prioriza a portabilidade, e o JSON mantém campos estruturados para outro programa.

## Logs salvos e capturas

O KillerBench pode investigar evidências sem executar nada no computador. Solte um log de texto na janela, ou cole o caminho e escolha **Abrir log**. As linhas de CBS, CSI e Setup são analisadas com os carimbos de data e hora, e as constatações apontam para a linha original exata. Arquivos EVTX e XML de eventos podem ser importados do mesmo jeito.

**Verificar o Windows Update** é uma coleta sob demanda e somente leitura dos logs de CBS, DISM, Panther e de reversão do Setup, dos canais de eventos de atualização e manutenção, dos estados dos serviços de atualização, do espaço livre, do tempo de atividade, dos indicadores de reinicialização pendente e da política de atualização selecionada. **Salvar captura** grava as evidências guardadas em um arquivo `.kbench`. **Abrir captura** o reabre sem os arquivos originais e o avalia de novo com as regras instaladas, de modo que as mesmas regras geram as mesmas constatações.

## Modo de demonstração

Inicie `KillerBench.exe --demo` para experimentar **Executar diagnóstico** em uma estação de trabalho fictícia. Cada fluxo de trabalho gera evidências sintéticas e progresso cronometrado. Nenhum provedor do Windows é chamado, nenhum comando de reparo é executado e nenhum diário é gravado. Os botões de ferramentas separados, fora de **Executar diagnóstico**, continuam ativos, então não os use como ações de demonstração.

`KillerBench.exe --demo-screenshot` abre, só para visualização, um relatório de Verificação completa do sistema de um computador fictício fixo. Cliques na área de trabalho, atalhos e o soltar de arquivos são bloqueados.

## Pacotes de conhecimento

Dados de referência grandes e atualizados com frequência ficam em pacotes de conhecimento, e não na lógica de diagnóstico: códigos de erro do Windows e do Setup, identificadores de fabricantes e dispositivos, referências PCI, USB, ACPI e UEFI, dados de fabricantes por OUI de rede, locais de drivers e suporte, e links de suporte selecionados.

Cada pacote identifica o formato, a versão do esquema, a origem, a revisão da origem, a hora de geração e o hash do conteúdo, e o KillerBench os valida antes do uso. Um pacote desconhecido ou inválido continua indisponível em vez de recorrer a um palpite. Uma atualização é baixada para um local temporário, validada e então substitui o pacote ativo em uma única etapa, de modo que uma atualização com falha mantém o pacote anterior no lugar.

Os dados de conhecimento podem explicar um identificador ou sugerir onde procurar em seguida. Eles não podem transformar um código histórico em falha atual, escolher um driver automaticamente nem autorizar um reparo.

## Atualizações e assinatura

Ao iniciar, o KillerBench consulta o GitHub em busca da versão estável mais recente, com limite de quatro segundos, e só oferece uma atualização quando essa versão é mais nova. Você pode desativar a verificação com **Verificar atualizações ao iniciar** em Sobre, ou no aviso de atualização. O download é o pacote que corresponde à cópia em execução, instalador ou portátil, e o SHA-256 dele precisa corresponder ao `SHA256SUMS.txt` da versão antes que algo seja instalado. O aplicativo se fecha e se reabre sozinho para concluir. Não há serviço em segundo plano.

O cartão Sobre mostra a versão, o editor e a impressão digital da assinatura de código, e o SHA-256 do executável em execução, para que você possa conferir a cópia que tem.

## KillerMCP

O KillerBench não inclui nenhum agente de IA. O [KillerMCP](../mcp.html) pode conectar um ao mesmo Engine. As **Opções do KillerMCP** em Sobre decidem o que esse agente pode fazer:

| Configuração | Efeito |
| --- | --- |
| Desativado (padrão) | O agente não pode solicitar nenhum reparo. |
| Permitir reparos seguros | O agente pode solicitar os reparos classificados como seguros. |
| Permitir também reparos disruptivos | Os reparos disruptivos ficam disponíveis, e cada um ainda precisa de aprovação. |

Só uma pessoa diante deste computador pode aumentar o nível. Após um resultado de reparo incerto, o KillerMCP grava um bloqueio, e os reparos do agente continuam bloqueados até que alguém o remova em Sobre.

## Localização

A interface está disponível em 19 idiomas: inglês, bengali, tcheco, alemão, espanhol, francês, húngaro, italiano, japonês, cazaque, norueguês, polonês, português (Brasil), russo, turco, ucraniano, vietnamita, e chinês simplificado e tradicional. A troca de idioma é aplicada na hora, incluindo os nomes das seções e dos fluxos de trabalho, o texto de boas-vindas e um relatório legível aberto. Qualquer texto ausente é exibido em inglês.

Evidências, constatações do Engine, diários e a Saída bruta continuam em inglês, como um log do Windows, para que um relatório seja lido do mesmo jeito em qualquer computador e possa ser pesquisado por texto exato.

## Linha de comando e códigos de saída

O `killerbench-cli` executa o mesmo Engine sem o aplicativo de desktop. O progresso vai para o erro padrão e o JSON para a saída padrão. A maioria dos comandos mostra uma visualização por padrão, e uma alteração exige `--execute`. Os comandos `-recover` correspondentes revisam uma ação interrompida sem repeti-la. Consulte [Executar o KillerBench por scripts](use-the-cli.html) e a [referência de comandos](cli.html).

| Código de saída | Comandos de fluxo de trabalho | Verificação e abertura |
| --- | --- | --- |
| 0 | Nenhum problema não resolvido nos escopos verificados | Cobertura concluída |
| 1 | Erro de inicialização ou do diário | Falha |
| 2 | Argumentos inválidos | Argumentos inválidos |
| 3 | Requer atenção | Coleta parcial |
| 4 | Reinicialização necessária antes da verificação | |
| 130 | Interrompido | Cancelado ou tempo limite geral esgotado |

Nenhum código de saída certifica que um computador está íntegro.

## Arquitetura

O Engine é dividido em três projetos sem interface. `Engine.Core` fornece primitivas e receitas declarativas, `Engine` cuida dos fluxos de trabalho compartilhados e das evidências, e `Engine.Windows` registra os provedores nativos. O aplicativo de desktop, a CLI e os testes são hosts separados.

| Camada | Responsabilidade |
| --- | --- |
| Coletores de capacidades | Leem um estado delimitado do Windows para um escopo exato |
| Analisadores de evidências | Validam forma, identidade, completude e limites |
| Interpretação do diagnóstico | Converte evidências em estados explícitos do fluxo de trabalho |
| Orquestração de fluxos de trabalho | Combina verificações, progresso, receitas e diários |
| Coordenadores de reparo | Verificam de novo a elegibilidade, preparam, alteram, verificam e reconciliam |
| Relatórios | Apresentam problemas, ações, resultados e limitações |
| Pacotes de conhecimento | Resolvem dados de referência versionados sem alterar a lógica |

Os coletores falham de forma fechada quando a identidade é ambígua, a saída está malformada, um limite é excedido ou a autoridade necessária não pode ser inspecionada. Hoje o Engine se destina ao Windows e não é multiplataforma. O [Guia do desenvolvedor do Engine](../engine/index.html) aborda a API, a CLI e a incorporação do Engine em outro host .NET.

## Privacidade e rede

Diagnósticos e análise de logs são executados localmente, sem serviço de IA nem conta do KillerBench. Diários e evidências guardadas ficam nas pastas `%LOCALAPPDATA%\KillerBench` da conta local.

As ações que você seleciona podem contatar o respectivo destino, como o Windows Update, serviços de download de software ou o backup da chave de recuperação no Active Directory ou no Entra ID. A verificação de atualizações ao iniciar contata o GitHub, e as atualizações de conhecimento e o instalador do KillerMCP contatam os serviços configurados. Um cliente de IA que use o [KillerMCP](../mcp.html) pode receber o resultado de referência que solicitou.

Logs brutos, arquivos importados e evidências exportadas podem conter nomes de computadores, contas, caminhos ou texto de origem sensível. Revise-os antes de compartilhar e nunca inclua chaves de recuperação, credenciais ou dados de clientes em um relatório público.

## Status do desenvolvimento

O KillerBench está em desenvolvimento e ainda não há download público. A cobertura de reparos ainda está crescendo, e a validação nativa de reparos ainda está em andamento. Uma compilação bem-sucedida ou uma verificação simulada não comprova o comportamento nativo de um reparo. Máquinas virtuais Windows descartáveis são usadas para validar cada alteração nativa em diferentes versões do Windows e estados de falha. Consulte [Como as alterações continuam seguras](safety.html) para ver o modelo de segurança completo.

## Glossário

Definições em linguagem simples dos termos usados nesta página, em ordem alfabética.

| Termo | O que significa |
| --- | --- |
| BITS | Serviço de Transferência Inteligente em Segundo Plano, o serviço do Windows que baixa atualizações e outros arquivos em segundo plano. |
| Captura | Um arquivo `.kbench` salvo com as evidências guardadas, para poder revisá-las de novo sem o computador original. |
| CBS | Component-Based Servicing, o subsistema do Windows que instala atualizações e recursos. O log dele é o `CBS.log`. |
| Diário | Um registro JSON local gravado antes de uma alteração e atualizado depois, para que uma ação interrompida possa ser revisada mais tarde. |
| DISM | Deployment Image Servicing and Management, a ferramenta do Windows que verifica e repara o repositório de componentes. |
| Elevação | Execução com direitos de administrador após um aviso do Windows. O KillerBench nunca se eleva sozinho. |
| Evidência | O que o KillerBench realmente leu do Windows, guardado com a origem para que uma constatação possa ser rastreada. |
| Falha fechada | Quando as evidências são ambíguas, ausentes ou grandes demais, o resultado é Desconhecido e nenhuma alteração é permitida. |
| Fluxo de trabalho | A execução de uma ferramenta: as verificações que ela coleta, as receitas que pode usar e o relatório que gera. |
| Pacote de conhecimento | Um arquivo versionado e validado de dados de referência, como códigos de erro ou identificadores de hardware. |
| Panther | A pasta em que a Instalação do Windows grava os logs de atualização. |
| Proprietário da política | A política de grupo ou o gerenciamento de dispositivos que controla uma configuração. O KillerBench não toca nas configurações controladas. |
| Proteção pendente | O registro que marca uma alteração como possivelmente enviada ao Windows. Ele bloqueia uma repetição até que a reconciliação leia o resultado. |
| Receita | Um reparo tipado com regras próprias de elegibilidade, execução, verificação e recuperação. |
| Reconciliação | Ler o estado atual após uma interrupção para saber o que aconteceu, sem repetir a alteração. |
| SFC | Verificador de Arquivos do Sistema, a ferramenta do Windows que verifica e restaura os arquivos protegidos do sistema. |
| TPM | Trusted Platform Module, o chip de segurança que o BitLocker usa para proteger a chave da unidade. |
| Verificado | O resultado foi lido de volta do Windows e corresponde ao que a alteração deveria fazer. |
