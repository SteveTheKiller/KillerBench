# Criptografar uma unidade com o BitLocker

Use este guia para ativar o BitLocker, fazer backup da chave de recuperação e confirmar que a unidade está protegida. O KillerBench faz backup da senha de recuperação e a verifica antes de iniciar a criptografia, e nunca exibe nem registra uma senha de recuperação.

## Antes de começar

- Execute o KillerBench como administrador.
- A unidade do Windows precisa de um protetor TPM e de um protetor de senha de recuperação. Uma unidade de dados precisa só de uma senha de recuperação.
- Decida para onde vai o backup da chave de recuperação: um arquivo fora da unidade que você vai criptografar e, opcionalmente, o Active Directory ou o Microsoft Entra ID.

## Verificar a unidade

1. Abra **BitLocker** e escolha a unidade em **Volume**.
2. Deixe a operação em **Inspecionar** e pressione **Executar diagnóstico**. O relatório mostra o estado da criptografia, o status da proteção e as identidades dos protetores.
3. Se o TPM estiver presente mas não estiver pronto, prepare-o antes em **Segurança**. O KillerBench nunca limpa um TPM.

## Adicionar os protetores necessários

1. Abra **Mais ações do BitLocker**.
2. Escolha **Adicionar um protetor TPM** ou **Adicionar uma senha de recuperação**, pressione **Visualizar**, marque **Fazer esta alteração na unidade selecionada quando eu pressionar Executar ação** e pressione **Executar ação**.
3. Faça backup da nova senha de recuperação imediatamente. O KillerBench não a exibe.

## Fazer backup da chave de recuperação e criptografar

1. Pressione **Localizar protetores de recuperação** e selecione o protetor de senha de recuperação.
2. Digite um caminho completo para o arquivo de backup, fora da unidade que você vai criptografar.
3. Escolha **Criptografar**, escolha o método de criptografia e marque **Fazer backup da chave de recuperação selecionada e iniciar a criptografia quando eu selecionar Executar diagnóstico**.
4. Pressione **Executar diagnóstico**. O KillerBench grava o backup, verifica-o e inicia a criptografia uma única vez.
5. A criptografia continua em segundo plano. Execute **Inspecionar** mais tarde para confirmar que a unidade aparece totalmente criptografada e com a proteção ativada.

## Backup no Active Directory ou no Entra ID

Em **Mais ações do BitLocker**, escolha o backup no diretório para o protetor selecionado. O KillerBench informa que o Windows aceitou a solicitação, mas não consegue ler a cópia no diretório, então confirme-a no Active Directory ou no centro de administração do Entra.

## Unidades de dados e desbloqueio automático

Depois que uma unidade de dados é criptografada, **Ativar o desbloqueio automático** faz com que ela seja desbloqueada quando o Windows inicia. A unidade do Windows já precisa estar totalmente criptografada e com a proteção ativada.

## Pela linha de comando

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Consulte [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) e [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Cada um tem um comando `-recover` correspondente para uma solicitação interrompida.

## Relacionados

- [Seção BitLocker](bitlocker.html)
- [Seção Segurança](security.html)
- [Como as alterações continuam seguras](safety.html)
