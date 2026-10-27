# BitLocker

BitLocker inspeciona a criptografia das unidades, os protetores e as informações de recuperação, e ajuda a fazer backup, proteger ou alterar uma unidade. Ele funciona somente neste computador.

Guias passo a passo: [Criptografar uma unidade com o BitLocker](encrypt-with-bitlocker.html).

## Usar a ferramenta BitLocker

Escolha a letra da unidade e uma **Operação** e pressione **Executar diagnóstico**.

- **Inspecionar** lê o estado da criptografia, o status da proteção e as identidades dos protetores. É a opção padrão e não altera nada.
- **Backup da recuperação** grava a senha de recuperação selecionada em um arquivo novo fora da unidade protegida e a verifica. Pressione antes **Localizar protetores de recuperação**, escolha o protetor e confirme o backup.
- **Criptografar** faz backup da senha de recuperação selecionada, verifica o backup e inicia a criptografia uma única vez com o método escolhido. A unidade do Windows precisa de um protetor TPM e de uma senha de recuperação. Uma unidade de dados precisa só de uma senha de recuperação.
- **Retomar proteção** reativa a proteção em uma unidade totalmente criptografada em que ela estava suspensa.

Cada uma exige a própria caixa de confirmação, e o KillerBench verifica a unidade de novo logo antes de agir.

## Mais ações do BitLocker

Abra **Mais ações do BitLocker** para as alterações que ficam fora de **Executar diagnóstico**. Escolha uma ação, pressione **Visualizar** para ler exatamente o que ela exige, confirme e pressione **Executar ação**.

- **Ativar o desbloqueio automático** faz com que uma unidade de dados criptografada seja desbloqueada quando o Windows inicia. A unidade do Windows precisa estar totalmente criptografada e com a proteção ativada.
- **Adicionar um protetor TPM** ou **Adicionar uma senha de recuperação** cria um protetor novo. O KillerBench nunca exibe nem registra uma senha de recuperação nova, então faça o backup imediatamente.
- **Fazer backup da senha de recuperação no Active Directory** ou no **Microsoft Entra ID** envia o protetor selecionado acima. O KillerBench informa que o Windows aceitou a solicitação. Ele não consegue ler a cópia no diretório, então confirme-a lá.
- **Descriptografar esta unidade** inicia a descriptografia, por exemplo antes de criptografar de novo com um método obrigatório. Exige uma segunda confirmação porque a unidade fica desprotegida depois, e é recusada na unidade do Windows enquanto outras unidades forem desbloqueadas automaticamente por ela.

## TPM e FIPS

Se o TPM estiver presente mas não estiver pronto, a seção Segurança pode prepará-lo sem limpá-lo. O KillerBench nunca limpa um TPM, porque isso destrói as chaves mantidas pelo BitLocker e pelo Windows Hello. A troca da política de algoritmos FIPS está disponível como um reparo explícito e reversível para organizações que precisam de PINs do BitLocker aprovados pelo FIPS.

## Ferramentas desta seção

Inspecione a criptografia, os protetores e as informações de recuperação, e depois faça backup deles ou proteja a unidade.

### BitLocker

Visualiza as opções do BitLocker e inspeciona o volume selecionado. Este fluxo de trabalho não faz criptografia nem backup de recuperação.
