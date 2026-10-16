# Ajuda do KillerBench

O KillerBench é uma bancada de diagnóstico e reparo do Windows para técnicos. Ele verifica o computador primeiro, explica o que encontrou com as evidências de cada constatação e só altera o Windows quando você escolhe uma correção específica e a confirma.

O KillerBench está em desenvolvimento. Ainda não há download público. Estas páginas descrevem a compilação atual.

## O que ele faz

- Executa diagnósticos somente leitura no Windows Update, em dispositivos, armazenamento, rede, impressão, segurança, aplicativos e perfis, e depois gera um relatório legível.
- Oferece reparos protegidos que só são executados quando evidências atuais comprovam o problema exato, e verifica o resultado em seguida.
- Abrange tarefas de configuração e proteção como BitLocker, configurações de privacidade, organização do Windows e implantação de um computador novo.
- Executa diagnósticos e análise de relatórios localmente, sem IA e sem conta do KillerBench. Algumas ações selecionadas, como downloads e o backup da chave de recuperação no Entra ID, podem contatar serviços externos.
- Usa o tema ativo do aplicativo no console, nos relatórios, nas confirmações e nas caixas de diálogo Abrir, Salvar e Pasta.

## Por onde começar

- [Download e configuração](setup.html)
- [Sobre o KillerBench](about.html)

- [Seu primeiro diagnóstico](first-run.html) apresenta uma verificação completa do sistema.
- [A área de trabalho](workspace.html) explica as seções, os painéis e os atalhos.
- [Como as alterações continuam seguras](safety.html) explica o que o KillerBench verifica antes e depois de qualquer alteração.
- Os [guias passo a passo](fix-windows-update.html) mostram as tarefas comuns.
- As páginas de **Seções** descrevem cada ferramenta, agrupadas como no aplicativo.
- [Linha de comando](cli.html) lista todos os comandos do `killerbench-cli`.
- [Solução de problemas](troubleshooting.html) trata de ações bloqueadas, reinicializações e solicitações interrompidas.

Desenvolvedores que querem incorporar os diagnósticos às próprias ferramentas podem ler o [Guia do desenvolvedor do Engine](../engine/index.html).
