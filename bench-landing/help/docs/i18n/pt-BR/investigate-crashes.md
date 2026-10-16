# Investigar falhas e telas azuis

Use este guia quando o Windows reinicia inesperadamente, mostra um erro de parada ou um aplicativo continua falhando. O KillerBench reúne em um único relatório as evidências de falhas, desligamentos, erros de hardware e confiabilidade. Ele descreve o que foi registrado e nunca adivinha uma causa raiz.

## Verificar o computador

1. Abra **Diagnósticos** e escolha **Sinais vitais**.
2. Pressione **Executar diagnóstico**. O relatório combina memória, integridade dos discos, problemas de dispositivos, histórico do Monitor de Confiabilidade, registros de erros de hardware, relatórios de falhas, desligamentos inesperados e resultados do Diagnóstico de Memória do Windows.
3. Leia a conclusão e as evidências abaixo dela. Falhas históricas explicam o que aconteceu; elas não comprovam que a falha ainda está presente.

## Restringir a investigação

- **Histórico de erros de hardware** lê os registros do WHEA e do diagnóstico de memória junto com os problemas de dispositivos e a integridade dos discos.
- **Histórico de confiabilidade** agrupa as falhas de aplicativos, os travamentos e os desligamentos inesperados dos últimos 14 dias.
- **Erros de aplicativos** se concentra nos programas que falham.
- **Histórico de instalação de drivers** mostra instalações de drivers com falha associadas a dispositivos presentes agora.
- **Problemas de dispositivos** lista os dispositivos com códigos de erro do Gerenciador de Dispositivos.

## Ler despejos de memória

O KillerBench lista a configuração de despejo e os metadados dos arquivos de despejo e pode analisar a estrutura de minidespejos para obter a exceção e os nomes dos módulos. O aplicativo de desktop nunca abre o conteúdo dos despejos. Com um pacote de conhecimento de verificação de bugs carregado, os códigos de parada são resolvidos para o significado documentado.

Guarde os despejos até terminar a investigação. **Limpeza** pode removê-los, mas isso descarta as evidências.

## Pela linha de comando

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Consulte [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) e [device-code](cli.html#cmd-device-code).

## Relacionados

- [Seção Diagnósticos](diagnostics.html#vitals)
- [Relatórios e evidências](reports.html)
