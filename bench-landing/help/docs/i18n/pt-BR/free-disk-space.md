# Liberar espaço em disco

Use este guia quando uma unidade estiver ficando sem espaço. O KillerBench mostra para onde o espaço está indo, visualiza o que cada destino de limpeza removeria e só exclui depois que você ativa a limpeza permanente.

## Ver quanto espaço resta

1. Abra **Armazenamento** e escolha **Armazenamento**.
2. Pressione **Executar diagnóstico**. O relatório sinaliza cada volume com letra de unidade que tenha menos de 10% de espaço livre e mostra a alocação de cópias de sombra, as cotas padrão e a política do Sensor de Armazenamento.
3. Para despejos de memória e conteúdo antigo de atualizações, execute também **Sinais vitais** em **Diagnósticos** e **Windows Update** em **Atualizações**; o tamanho do cache de atualizações aparece lá.

## Visualizar uma limpeza

1. Abra **Manutenção** e escolha **Limpeza**.
2. Marque os destinos desejados, como **Arquivos temporários do usuário atual**, **Arquivos temporários do Windows**, **Caches de navegador do usuário atual**, **Cache da Otimização de Entrega** ou **Lixeira do usuário atual**. Passe o ponteiro sobre um destino para ver exatamente quais pastas ele abrange.
3. Defina **Idade mínima (dias)** e **Máximo de arquivos por alvo**.
4. Pressione **Executar diagnóstico** com **Executar a limpeza permanente selecionada após novas verificações** desativado. O relatório lista os candidatos e o espaço estimado de cada destino. Nada é excluído.

## Remover os arquivos

1. Ative **Executar a limpeza permanente selecionada após novas verificações** e pressione **Executar diagnóstico**.
2. O KillerBench verifica cada candidato de novo antes de removê-lo, ignora arquivos recentes, protegidos ou em uso, e registra cada resultado no diário.

A limpeza permanente não passa pela Lixeira e não pode ser desfeita. Remover relatórios de erros e despejos de memória descarta evidências de que você pode precisar para investigar falhas, então revise-os primeiro.

## Remover a instalação anterior do Windows

Depois de uma atualização de recursos, **Instalação anterior do Windows** em **Limpeza** pode remover a cópia de reversão pela solicitação compatível de remoção de reversão do Windows. Ela é executada sozinha, exige **Fiz backup de todos os arquivos e configurações de que preciso da instalação anterior** e remove de forma permanente a possibilidade de voltar à versão anterior.

## Pela linha de comando

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Consulte [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) e [previous-windows](cli.html#cmd-previous-windows).

## Relacionados

- [Seção Manutenção](maintenance.html#cleanup)
- [Seção Armazenamento](storage.html)
- [Verificar e reparar uma unidade](check-a-drive.html)
