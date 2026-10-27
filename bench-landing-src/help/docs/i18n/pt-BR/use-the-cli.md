# Executar o KillerBench por scripts

Use a linha de comando quando quiser evidências do KillerBench em um script, uma tarefa de RMM ou um console remoto. O `killerbench-cli` executa o mesmo Engine do aplicativo de desktop e imprime JSON.

## Primeiros passos

1. Abra o PowerShell na pasta do `killerbench-cli.exe`, ou adicione essa pasta ao caminho.
2. Execute `killerbench-cli help` para listar todos os comandos com o respectivo limite de segurança.
3. Execute `killerbench-cli diagnostic-scopes` para ver os 11 grupos de diagnóstico recomendados.

## Executar uma verificação e salvar um relatório

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` é apenas diagnóstico e rejeita `--auto-repair`. Use um fluxo de reparo específico quando for necessária uma ação determinada. `--report` grava o mesmo relatório com tema que o aplicativo exporta. Use `--scope` para verificar só algumas áreas, e `common-check` para obter JSON combinado sem diário de fluxo de trabalho.

## Ler os códigos de saída

| Código | Significado |
| --- | --- |
| 0 | A visualização ou o resultado verificado foi bem-sucedido. |
| 1 | Um erro de inicialização, de entrada ou do diário interrompeu o comando. |
| 2 | Os argumentos não foram reconhecidos. |
| 3 | Bloqueado, com falha ou não verificado. |
| 4 | Um fluxo de trabalho precisa de reinicialização antes de poder verificar. |
| 130 | Você o interrompeu. |

Saída 0 em um comando somente leitura significa que ele foi executado, não que o computador está íntegro. Leia o JSON para ver as constatações.

## Fazer uma alteração

Todo comando que pode alterar o computador mostra uma visualização por padrão. Leia a visualização e execute o mesmo comando com `--execute`. Se uma solicitação for interrompida, execute o comando `-recover` correspondente; ele informa o resultado real e nunca repete a alteração.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Reabrir resultados antigos

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Usar com um assistente de IA

O [KillerMCP](../mcp.html) pode chamar a CLI instalada para consultar códigos de erro do Gerenciador de Dispositivos e do Windows.

## Relacionados

- [Referência da linha de comando](cli.html)
- [Guia do desenvolvedor do Engine](../engine/index.html)
- [Solução de problemas](troubleshooting.html)
