# Configurar um computador novo

Use este guia ao preparar um PC novo ou recém-redefinido. O KillerBench revisa o computador, os aplicativos que você pretende instalar e o nome que quer dar a ele, e depois aponta as ferramentas protegidas que fazem cada alteração.

## Começar com uma referência

1. Execute **Verificação completa do sistema** em **Diagnósticos** e guarde o relatório como registro do estado inicial do computador.
2. Instale as atualizações pendentes. Consulte [Corrigir o Windows Update](fix-windows-update.html) se alguma falhar.

## Revisar aplicativos e configuração

1. Abra **Implantação** e escolha **Implantação**.
2. Selecione os aplicativos e os grupos de configuração que pretende preparar e pressione **Executar diagnóstico**. A revisão os compara com o que já está instalado. O KillerBench não baixa nem executa instaladores.
3. Antes de executar o instalador de um fabricante, verifique-o pela linha de comando. O KillerBench só confia nele quando tem uma assinatura válida com o nome exato da organização do fabricante.

## Dar nome ao computador

1. Escolha **Nome do computador**, digite o nome e pressione **Executar diagnóstico** para compará-lo com a identidade atual e a função no domínio.
2. Em um computador de grupo de trabalho, marque **Renomear este PC de grupo de trabalho após novas verificações** para renomeá-lo. É preciso reiniciar para concluir.

## Proteger e organizar

- [Criptografe uma unidade com o BitLocker](encrypt-with-bitlocker.html) e faça backup da chave de recuperação.
- [Deixe o Windows mais privado](make-windows-private.html) e revise **Organizar o Windows** em **Manutenção**.
- Crie um ponto de restauração com **Preparar um ponto de restauração** quando tudo estiver pronto.

## Pela linha de comando

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Consulte [workflow](cli.html#cmd-workflow) e [restore-point](cli.html#cmd-restore-point).

## Relacionados

- [Seção Implantação](onboarding.html)
- [Seu primeiro diagnóstico](first-run.html)
