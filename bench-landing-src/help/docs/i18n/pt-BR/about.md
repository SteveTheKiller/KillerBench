# Sobre o KillerBench

O KillerBench é uma bancada de diagnóstico, reparo e manutenção do Windows criada por Steve the Killer, parte da [família Killer Tools](https://killertools.net). A versão 1.0.0 ainda não foi lançada. O aplicativo de desktop, o Engine e a CLI compartilham as evidências de diagnóstico e os fluxos de reparo compatíveis.

## Disponibilidade e escopo

Ainda não há download público do KillerBench nem pacote do Engine. [Download e configuração](setup.html) descreve o destino atual Windows x64 e os requisitos do Desktop Runtime. A validação nativa de reparos ainda está em andamento; a existência de uma ferramenta não comprova que todas as falhas, interrupções ou configurações do Windows foram testadas.

## Avisos de código-fonte e distribuição

O KillerBench é licenciado sob a **GPLv3**. A licença de distribuição é a [Licença Pública Geral GNU versão 3](../license.txt). A licença descreve as permissões e condições para copiar, modificar e distribuir a obra coberta.

O [repositório público do KillerBench](https://github.com/SteveTheKiller/KillerBench) oferece, por enquanto, informações do projeto. A implementação completa não está disponível publicamente lá. Estes guias para desenvolvedores descrevem uma cópia autorizada do código-fonte; eles não oferecem download público do código-fonte.

Antes de distribuir um binário, quem o publica deve fornecer o Código-fonte correspondente por um método permitido pela [seção 6 da GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html#section6), incluindo o código-fonte e o material de compilação exigidos pela licença. As instruções de entrega do código-fonte do primeiro pacote público e os avisos de terceiros incluídos continuam pendentes. A GPLv3 não concede permissão para redistribuir fontes de terceiros; as permissões atuais das fontes ainda precisam ser verificadas. Revise a licença, as instruções de código-fonte e os avisos do pacote publicado antes de redistribuí-lo.

## Privacidade e serviços externos

Diagnósticos e análise de logs são executados localmente, sem serviço de IA nem conta do KillerBench. Diários e evidências guardadas usam as pastas `%LOCALAPPDATA%\KillerBench` da conta local. Ações selecionadas podem contatar o Windows Update, serviços de download de software, diretórios ou outros destinos indicados. A verificação de atualizações ao iniciar contata o GitHub, e as atualizações de conhecimento e a página do instalador do MCP contatam os serviços configurados. Um cliente de IA que use MCP pode receber o resultado de referência retornado.

Este site guarda as escolhas de tema e cor de destaque no armazenamento local do navegador. Os guias não executam nenhum diagnóstico no seu computador. Logs brutos, arquivos importados e evidências exportadas podem conter nomes de computadores, contas, caminhos ou texto de origem sensível. Revise-os antes de compartilhar; não inclua chaves de recuperação, credenciais nem dados de clientes em um relatório público.

## Suporte

As informações do projeto estão no repositório público. Para suporte, use os contatos publicados no [site do autor](https://thekiller.net); informe a versão do aplicativo, a compilação do Windows, a ferramenta exata, o resultado esperado e o resultado observado. Revise e anonimize as evidências antes de enviá-las e use um canal privado aprovado para informações de clientes. Guarde os diários quando o resultado de um reparo for incerto.

## Guias relacionados

- [Como as alterações continuam seguras](safety.html)
- [Relatórios e evidências](reports.html)
- [Guia do desenvolvedor do Engine](../engine/)
- [KillerMCP](../mcp.html)
