# Como as alterações continuam seguras

O KillerBench trata diagnóstico e alteração como tarefas diferentes. O diagnóstico apenas lê. A verificação completa do sistema nunca executa reparos. Selecione um reparo ou uma ação específica, revise as opções e os pré-requisitos e confirme a alteração proposta.

## Antes de uma alteração

1. **Evidências atuais.** A ferramenta lê o estado atual de novo. Um relatório salvo ou um evento antigo nunca autoriza uma alteração.
2. **Elegibilidade exata.** A alteração só é oferecida quando as evidências correspondem ao problema exato que ela corrige. Qualquer dado incompleto a bloqueia.
3. **Sua confirmação.** Revise a ação selecionada na caixa de confirmação. Algumas ações, como descriptografar uma unidade, exigem uma segunda confirmação. A caixa segue o tema ativo do aplicativo; a aparência não muda os requisitos da ação.
4. **Pré-requisitos.** São verificados os direitos de administrador, o estado de reinicialização e o controle por política ou gerenciamento de dispositivos. Configurações controladas por política não são tocadas.
5. **Um registro pendente.** O KillerBench anota o que vai fazer antes de pedir ao Windows.
6. **Uma verificação final.** Tudo é verificado de novo logo antes da solicitação, então qualquer mudança nesse meio tempo a interrompe.

## Depois de uma alteração

- O KillerBench lê o resultado de volta e só informa **Verificado** quando o Windows mostra o estado esperado.
- Se o Windows retornar sucesso mas o estado não tiver mudado, o resultado informa isso e nada é repetido.
- Cada alteração é salva em um diário em `%LOCALAPPDATA%\KillerBench`.

## Se algo for interrompido

As ações com diário compatíveis mantêm um registro pendente quando uma solicitação pode ter chegado ao Windows e a verificação não foi concluída. Use o caminho de reconciliação documentado da ação, ou o comando `-recover` correspondente, para inspecionar esse registro. A reconciliação não é uma reversão automática. Guarde o diário quando o resultado for incerto e não repita a alteração manualmente. O comportamento em falhas, cancelamentos e inicializações posteriores ainda precisa de validação nativa específica de cada ação.

## O que o KillerBench não fará

- Limpar um TPM, excluir protetores que ele não criou ou exibir senhas de recuperação.
- Alterar uma configuração controlada pela política de grupo ou pelo gerenciamento de dispositivos.
- Executar um reparo só porque um evento histórico ou um relatório importado menciona um problema.

Diagnósticos e análise de relatórios são executados localmente. Uma ação que você selecionar pode contatar um destino externo, como downloads de software ou o backup da chave de recuperação no Active Directory ou no Entra ID. Revise o destino antes de confirmar. Capturas salvas e evidências brutas podem conter a identidade do computador e caminhos de origem, então revise-as antes de compartilhar.

## Status do desenvolvimento

A cobertura de reparos ainda está crescendo. Uma compilação bem-sucedida ou uma verificação simulada não comprova o comportamento nativo de um reparo. O arquivo `VM-VALIDATION.md` do repositório acompanha os cenários em máquinas Windows descartáveis e a validação restante.
