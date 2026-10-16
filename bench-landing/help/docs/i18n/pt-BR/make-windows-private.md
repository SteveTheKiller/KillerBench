# Deixar o Windows mais privado

Use este guia para revisar e reforçar a privacidade de um notebook. O KillerBench lê as configurações atuais, mostra quais já são controladas por política ou pelo gerenciamento de dispositivos e só altera as configurações que você escolher, uma de cada vez, com um desfazer exato.

## Revisar as configurações atuais

1. Abra **Privacidade**.
2. Marque as áreas que deseja revisar. Todas as áreas começam desmarcadas.
3. Pressione **Executar diagnóstico**. O relatório mostra o valor atual para este computador e para a conta que executa o KillerBench, e sinaliza as configurações controladas por política.

## Alterar uma configuração

1. Escolha a configuração em **Configuração de privacidade**, como câmera, microfone, localização, dados de diagnóstico, publicidade, histórico da área de transferência, pesquisa de conteúdo na nuvem ou destaques da pesquisa.
2. Escolha o novo valor e marque **Aplicar quando eu selecionar Executar diagnóstico**.
3. Pressione **Executar diagnóstico**. O KillerBench verifica se há uma política controlando a configuração, registra o valor atual, faz essa única alteração e a lê de volta.

Configurações controladas pela política de grupo ou pelo gerenciamento de dispositivos não são tocadas. Altere-as onde são gerenciadas.

## Desfazer uma alteração

Cada alteração verificada mantém um diário. O desfazer correspondente restaura o valor anterior exato, também após uma verificação de política.

## Organizar o Windows

Para sugestões, conteúdo para consumidores, Recall e recursos de IA, excesso de itens na barra de tarefas e no Iniciar, Edge e software pré-instalado, abra **Manutenção** e escolha **Organizar o Windows**. A revisão é somente leitura e nada é selecionado automaticamente.

## Pela linha de comando

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Consulte [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) e [debloat-review](cli.html#cmd-debloat-review). Uma chave não compatível lista as opções compatíveis.

## Relacionados

- [Seção Privacidade](privacy.html)
- [Configurar um computador novo](set-up-new-pc.html)
