# Corrigir problemas de impressão

Use este guia quando trabalhos ficam parados na fila, uma impressora aparece offline ou a impressora padrão está errada. O KillerBench lê primeiro o spooler, as filas, os trabalhos, os drivers e as portas, e depois permite corrigir uma fila ou um trabalho exato.

## Verificar as impressoras

1. Abra **Dispositivos** e escolha **Impressão**.
2. Pressione **Localizar impressoras locais** e escolha a fila com problema. Para verificar todas as filas, escolha **Usar verificações gerais de impressão**.
3. Deixe **Reparar automaticamente os problemas confirmados** desativado e pressione **Executar diagnóstico**.
4. Leia o relatório. Ele mostra o estado do Spooler de Impressão, as filas pausadas, o modo offline, os trabalhos visíveis, as condições dos drivers, os registros de portas, os erros recentes do PrintService e, para impressoras de rede, uma tentativa de conexão com o endereço atribuído à impressora.

Uma conexão bem-sucedida ou um código de condição limpo não comprovam que a impressora tem papel, toner ou um mecanismo funcionando.

## Reparar a fila

1. Execute **Impressão** de novo com **Reparar automaticamente os problemas confirmados** ativado. Ele pode iniciar o spooler habilitado, retomar uma fila exata pausada que tenha trabalhos aguardando e desativar o modo offline.
2. Desativar o modo offline pode liberar trabalhos em espera, então confirme que eles ainda devem ser impressos.
3. Uma fila pausada de propósito e sem nada aguardando continua pausada.

## Remover um trabalho travado

1. Pressione **Localizar trabalhos de impressão bloqueados**.
2. Selecione o trabalho que mostra Erro ou Bloqueado e marque **Cancelar somente este trabalho selecionado**.
3. Pressione **Executar diagnóstico**. Só esse trabalho é cancelado, e o resultado é lido de volta.

## Corrigir a impressora padrão

Escolha a fila que deve ser a padrão, marque **Reparar uma impressora padrão ausente ou conflitante com esta fila** e pressione **Executar diagnóstico**. Isso só age quando a padrão está ausente ou em conflito.

## Pela linha de comando

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Consulte [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) e [default-printer-set](cli.html#cmd-default-printer-set).

## Relacionados

- [Seção Dispositivos](devices.html#printing)
- [Corrigir problemas de rede e internet](fix-network.html)
