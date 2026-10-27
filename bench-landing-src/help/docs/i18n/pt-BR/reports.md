# Relatórios e evidências

Cada execução gera um relatório que você pode ler, filtrar e exportar. O relatório sempre separa o que foi comprovado do que foi apenas observado.

## Estados que você verá

| Estado | Significado |
| --- | --- |
| Para revisar | As evidências sustentam um problema que merece atenção. |
| Informação | Evidências foram coletadas e são mostradas como contexto. |
| Incompletas ou Desconhecido | Uma verificação não pôde terminar ou a evidência estava indisponível. Nunca conta como integridade. |
| Histórico | Um evento do passado. Explica o histórico, mas não comprova uma falha atual. |
| Verificado | Um reparo ou alteração foi confirmado lendo o resultado de volta. |

## A saída bruta e o relatório legível

**Saída bruta** mantém as evidências técnicas com os números de linha originais, para que você possa rastrear qualquer constatação até a origem. O **Relatório legível** resume a mesma execução em linguagem simples, com a próxima etapa segura em primeiro lugar.

## Logs salvos

O KillerBench também pode ler logs salvos do Windows, como os do CBS e do Setup, sem executar nada no computador. As constatações apontam para a linha exata que as gerou. **Carregar exemplo**, em Fontes da sessão, abre um exemplo sintético que você pode explorar com segurança.

## Exportar

**Exportar relatório** salva o relatório com os caminhos de origem, as entradas analisadas e os trechos de evidência, para revisão antes de anexá-lo a um chamado. Os relatórios exportados incluem o nome deste computador, e logs brutos e trechos de evidência também podem conter contas, caminhos ou texto de origem sensível. Os coletores protegidos limitam o que guardam, mas não há garantia de que as evidências importadas estejam anonimizadas. Inspecione e remova as informações sensíveis antes de compartilhar.
