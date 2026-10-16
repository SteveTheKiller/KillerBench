# Seu primeiro diagnóstico

Comece por [Download e configuração](setup.html) e depois execute uma Verificação completa do sistema. Ela coleta evidências de diagnóstico e salva um diário local; não executa reparos. Uma constatação descreve o escopo inspecionado, não todas as falhas possíveis do computador.

## Executar a verificação

1. Abra o KillerBench. A seção **Diagnósticos** abre com **Verificação completa do sistema** selecionada.
2. Deixe **Reparar automaticamente os problemas confirmados** desativado na primeira execução.
3. Pressione **Executar diagnóstico**. O progresso aparece em **Saída bruta**, à direita, enquanto cada verificação é executada.
4. Ao terminar, o painel de saída muda para o **Relatório legível**.

Comece sem elevação. Algumas evidências do Windows, como os protetores do BitLocker e os serviços protegidos, exigem direitos de administrador. Em uma conta padrão, essas verificações informam que a evidência não está disponível. Se precisar dela, termine a execução atual, guarde o relatório e reabra o KillerBench de propósito com **Executar como administrador**. A elevação não seleciona nem aprova nenhum reparo.

## Ler o relatório

O relatório começa com as contagens de **Para revisar**, **Reparos verificados**, **Incompletas** e **Verificadas**, seguidas de uma próxima etapa recomendada.

- **O que precisa de atenção** lista os problemas que as evidências sustentam, do mais importante para o menos.
- **Informações coletadas** mostra o que foi lido, incluindo evidências incompletas ou indisponíveis.
- Eventos históricos são marcados como histórico. Um erro do mês passado não é tratado como falha atual.

A falta de evidência nunca conta como integridade. Se uma verificação não pôde ser executada, o relatório informa isso.

## Escolher as verificações

Abra **Escolher verificações (avançado)** para selecionar verificações individuais em vez da verificação completa. Cada seção do aplicativo também tem ferramentas específicas, descritas nas páginas de **Seções** deste guia.

## Corrigir o que você encontrou

Quando o relatório recomenda uma correção, ele indica a seção e a ferramenta a abrir. Leia [Como as alterações continuam seguras](safety.html) antes do seu primeiro reparo. A validação nativa ainda está em andamento; um diagnóstico bem-sucedido não comprova que cada reparo oferecido foi testado na sua compilação do Windows. Mantenha backups e revise o destino exato e as consequências antes de qualquer alteração.
