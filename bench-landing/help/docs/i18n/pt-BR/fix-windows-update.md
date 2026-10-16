# Corrigir o Windows Update

Use este guia quando as atualizações falham, travam ou continuam sendo reinstaladas. O KillerBench lê primeiro a cadeia de atualização, explica a falha com as evidências por trás dela e só inicia um serviço ou repara o repositório de componentes quando evidências atuais comprovam esse problema exato.

## Antes de começar

- Execute o KillerBench como administrador. Os serviços de atualização, o repositório de componentes e os arquivos protegidos do sistema exigem elevação para fornecer evidências completas.
- Reinicie primeiro se o Windows já tiver uma reinicialização pendente. Muitos reparos de atualização ficam bloqueados enquanto houver reinicialização pendente.
- Mantenha o computador ligado na tomada durante a manutenção.

## Descobrir por que as atualizações falham

1. Abra **Atualizações** e escolha **Windows Update** na barra lateral de ferramentas.
2. Deixe **Reparar automaticamente os problemas confirmados** desativado na primeira passagem e pressione **Executar diagnóstico**.
3. Leia o **Relatório legível**. Ele lista serviços de atualização parados, reinicializações pendentes, pouco espaço livre, corrupção no repositório de componentes e os códigos de erro exatos que o Windows registrou, com uma próxima etapa recomendada.
4. Se o relatório citar um código de erro, as verificações complementares correspondentes são executadas automaticamente. Você também pode consultar um código com `killerbench-cli update-code 0x80070002`.

## Acompanhar uma atualização que parece travada

Escolha **Monitor de atividade de manutenção** em **Atualizações** e pressione **Executar diagnóstico**. Ele tira instantâneos repetidos da atividade de atualização, do Setup e do Installer, dos totais de transferência e dos indicadores de reinicialização. A atividade é mostrada apenas como evidência; ela nunca afirma sozinha um travamento ou uma falha.

## Reparar o que o relatório encontrou

1. Execute **Windows Update** de novo com **Reparar automaticamente os problemas confirmados** ativado. Ele pode iniciar serviços de atualização habilitados que estejam comprovadamente parados, e só executa o DISM ou o SFC quando a verificação em tempo real confirma corrupção.
2. Para um único serviço, escolha a ferramenta específica dele, como **Serviço BITS**, **Serviços de Criptografia**, **Instalador de Módulos do Windows** ou **Serviço Update Orchestrator**. Cada uma oferece recuperação só para aquele serviço exato.
3. Quando o relatório pedir para reiniciar, reinicie antes de executar qualquer outra coisa. O KillerBench registra os resultados que exigem reinicialização e adia o trabalho seguinte até a reinicialização.

Cada reparo é registrado no diário e lido de volta. Consulte [Como as alterações continuam seguras](safety.html) para ver a sequência completa.

## Instalar uma atualização específica

1. Escolha **Atualizações disponíveis** em **Atualizações** e pressione **Executar diagnóstico** para descobrir as atualizações da origem configurada.
2. Selecione as atualizações desejadas e pressione **Visualizar selecionadas**. A visualização lista exatamente o que seria instalado e qualquer reinicialização necessária.
3. Instale somente depois de ler a visualização. O KillerBench nunca seleciona nem aprova uma atualização por você.

## Pela linha de comando

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Consulte [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) e [update-plan](cli.html#cmd-update-plan). Se uma instalação foi interrompida, execute [update-recover](cli.html#cmd-update-recover); ele verifica o resultado e nunca tenta a atualização de novo.

## Relacionados

- [Seção Atualizações](updates.html)
- [Reparar arquivos do sistema](repair-system-files.html)
- [Solução de problemas](troubleshooting.html)
