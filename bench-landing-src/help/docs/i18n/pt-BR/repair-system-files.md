# Reparar arquivos do sistema

Use este guia quando componentes do Windows estão corrompidos: recursos não instalam, atualizações informam erros de componentes ou ferramentas do sistema falham. O KillerBench só executa o DISM e o SFC depois que uma verificação em tempo real confirma a corrupção, e depois verifica o resultado.

## Antes de começar

- Execute o KillerBench como administrador.
- Resolva antes qualquer reinicialização pendente.
- Se o computador for gerenciado, verifique se uma política direciona o DISM para uma origem de reparo específica. O KillerBench informa essa política e não a altera.

## Procurar corrupção

1. Abra **Reparo** e escolha **Arquivos do sistema**.
2. Deixe **Reparar automaticamente os problemas confirmados** desativado e pressione **Executar diagnóstico**.
3. Leia o relatório. Ele inventaria os estados dos pacotes de manutenção, dos recursos opcionais e das capacidades, verifica os pré-requisitos e examina os arquivos protegidos do sistema. Um resultado limpo significa que nenhum reparo é oferecido.

## Reparar corrupção confirmada

1. Ative **Reparar automaticamente os problemas confirmados** e pressione **Executar reparo**. O KillerBench escolhe o DISM ou o SFC só quando a verificação em tempo real confirma corrupção.
2. Aguarde a verificação. O relatório só mostra **Verificado** quando a verificação seguinte lê um estado reparado.
3. Se o relatório disser que o repositório não pode ser recuperado ou que a origem de reparo está indisponível, pare por aí. Repetir o reparo não muda o resultado.

Uma execução interrompida do DISM ou do SFC é reconciliada na próxima inicialização sem repetir o comando de manutenção.

## Limpar o repositório de componentes

Escolha **Limpeza do repositório de componentes** em **Atualizações**. Primeiro ela pergunta ao Windows se a limpeza é recomendada. A manutenção opcional remove componentes substituídos e analisa de novo. Ela nunca usa ResetBase e nunca exclui conteúdo do WinSxS manualmente.

## Pela linha de comando

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Consulte [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) e [servicing-logs](cli.html#cmd-servicing-logs). `repair` mostra uma visualização por padrão e só age com `--execute`.

## Relacionados

- [Corrigir o Windows Update](fix-windows-update.html)
- [Seção Reparo](repair.html#system-files)
- [Como as alterações continuam seguras](safety.html)
