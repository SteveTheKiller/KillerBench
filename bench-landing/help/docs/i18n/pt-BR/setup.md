# Download e configuração

O KillerBench 0.1.0 ainda não foi lançado. Ainda não há pacote nem download público do KillerBench. Esta página descreve os requisitos atuais do pacote para que você se prepare sem baixar uma cópia não oficial.

## Windows e runtime

- O destino de publicação atual é **Windows x64**. O Windows 11 x64 é a referência atual de validação nativa. Não há distribuições nativas para ARM64, x86 ou Windows Server.
- O KillerBench é empacotado de duas formas. O instalador é **dependente de framework** e exige o **.NET Desktop Runtime 10 para Windows x64**. O .NET Runtime básico, o ASP.NET Core Runtime e o .NET Framework não fornecem o WPF. A edição portátil traz o próprio runtime e não precisa de instalação separada. A ferramenta de linha de comando `killerbench-cli.exe` precisa do .NET 10 Runtime para Windows x64. O SDK só é necessário para desenvolvimento, não para o uso normal.
- Use uma versão do Windows compatível com o [.NET 10 no Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows). A compatibilidade do runtime não significa que os reparos do KillerBench foram validados em todas as edições ou compilações compatíveis. A cobertura do Windows 10 continua pendente, e as edições compatíveis dependem dos requisitos atuais de ciclo de vida da Microsoft.
- Obtenha o Desktop Runtime somente na [página de download do .NET 10 da Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), escolhendo **.NET Desktop Runtime**, **Windows**, **x64**. Siga as regras de instalação de software da sua organização.

Se o `dotnet` estiver disponível, `dotnet --list-runtimes` deve listar `Microsoft.WindowsDesktop.App 10.0.x` para a instalação x64. Um runtime instalado só para outra arquitetura não atende ao pacote de instalação x64.

## Quando um pacote for publicado

1. Use o download oficial vinculado a partir deste site. Ainda não há link de download do KillerBench ativo.
2. Leia os requisitos do pacote, a [licença GPLv3](../license.txt), as instruções do Código-fonte correspondente, os avisos de terceiros incluídos e as informações de assinatura e soma de verificação. Só o nome do arquivo não comprova a autenticidade.
3. Execute o instalador para a conta atual, ou para todos os usuários após uma solicitação de elevação. Ele adiciona um atalho no menu Iniciar, um atalho opcional na área de trabalho e uma entrada em Adicionar ou remover programas. A edição portátil é executada sem instalação.
4. Inicie o KillerBench com sua conta normal e siga [Seu primeiro diagnóstico](first-run.html). As falhas de acesso do diagnóstico continuam visíveis; a elevação é uma escolha separada e deliberada.

Não ignore os avisos de segurança do Windows para executar uma cópia não verificada. Mantenha um backup atualizado antes de selecionar qualquer ação que altere o computador.

Ao iniciar, o KillerBench consulta o GitHub em busca de uma versão estável mais recente e pergunta antes de instalá-la. O download precisa corresponder à soma de verificação SHA-256 publicada. Desative a verificação com **Verificar atualizações ao iniciar** em Sobre.

## Limites atuais

A compilação atual tem doze seções, diagnóstico somente leitura, relatórios e superfícies de reparo selecionadas individualmente. A validação nativa de reparos ainda está em andamento. Uma interface que funciona, uma compilação bem-sucedida ou o resultado de um fixture não comprovam o comportamento de um reparo em um computador real. Consulte [Como as alterações continuam seguras](safety.html#development-status).

O console ilustrativo da página inicial é uma saída sintética. As capturas de tela finais do aplicativo e o primeiro pacote público ainda estão pendentes.

## Código-fonte, licença e suporte

O KillerBench é licenciado sob a GPLv3. As instruções de entrega do código-fonte e as permissões de fontes de terceiros continuam pendentes para o primeiro pacote público. Leia [Sobre o KillerBench](about.html) para saber sobre a disponibilidade do código-fonte, os avisos de distribuição, a privacidade das evidências e o suporte. O [guia do Engine](../engine/) é para desenvolvedores com uma cópia autorizada do código-fonte; ele não indica que haja um pacote público do Engine.
