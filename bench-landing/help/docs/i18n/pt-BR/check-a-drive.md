# Verificar e reparar uma unidade

Use este guia quando um volume informa erros, o Windows pede para verificar uma unidade ou arquivos não abrem. O KillerBench verifica primeiro o volume em modo somente leitura e só agenda ou executa um reparo do sistema de arquivos depois que você confirma que tem backup.

## Antes de começar

- Faça backup dos dados importantes do volume. Um reparo do sistema de arquivos pode alterar ou remover arquivos danificados.
- Execute o KillerBench como administrador.

## Verificar o volume

1. Abra **Armazenamento** e escolha **Armazenamento**.
2. Escolha a unidade em **Volume a verificar** e pressione **Executar diagnóstico**.
3. Leia o relatório. Ele mostra a integridade do disco e do volume, o bit sujo, a autorrecuperação online do NTFS, erros de controladora e de dispositivos de disco, contadores de confiabilidade e eventos recentes de armazenamento, como redefinições, novas tentativas e remoções inesperadas.

Uma verificação limpa significa que nenhum reparo é oferecido. Temperatura do disco, desgaste e contadores de E/S não corrigidos são evidências para revisar e não disparam um reparo sozinhos.

## Reparar o sistema de arquivos

1. Pressione **Revisar reparo do sistema de arquivos** e escolha **Reparo pontual** ou **Verificação e reparo offline**.
2. Em um volume de dados, marque **Permitir que um volume que não seja do sistema fique offline** se puder fechar tudo o que o usa. O volume do Windows não pode ficar offline, então o reparo dele é agendado para a próxima reinicialização.
3. Marque **Os dados importantes deste volume têm backup**.
4. Confirme o reparo. O KillerBench verifica de novo o volume exato logo antes de agir e registra antes uma entrada pendente.
5. Para um reparo agendado, reinicie quando for conveniente. O KillerBench verifica o resultado após a reinicialização sem executar o reparo de novo.

## Otimizar ou executar Retrim

A mesma ferramenta pode analisar o volume e executar a otimização recomendada pelo Windows, **Executar Retrim no armazenamento de estado sólido** ou uma desfragmentação de disco rígido. Cada uma é uma ação confirmada separadamente em um único volume exato e íntegro.

## Pela linha de comando

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Consulte [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) e [volume-optimize](cli.html#cmd-volume-optimize).

## Relacionados

- [Seção Armazenamento](storage.html)
- [Liberar espaço em disco](free-disk-space.html)
- [Solução de problemas](troubleshooting.html)
