# Solução de problemas

## Uma ação informa que está bloqueada

Um resultado bloqueado significa que a ação solicitada não pôde prosseguir. Leia a mensagem e o diário para saber se ela parou antes do envio ou depois de uma etapa anterior. Não presuma que uma operação parcialmente concluída foi revertida. Os motivos de pré-requisito mais comuns estão abaixo.

- **É necessário acesso de administrador.** Feche o KillerBench e inicie-o de novo com **Executar como administrador**.
- **Não pode haver reinicialização pendente.** O Windows tem uma reinicialização pendente. Reinicie o computador e execute a ação de novo.
- **Há uma política gerenciada.** A política de grupo ou o gerenciamento de dispositivos controla a configuração. Altere-a por lá.
- **As evidências mudaram.** Algo mudou entre a primeira verificação e a verificação final. Execute a ferramenta de novo para ler o novo estado.

## Uma solicitação anterior precisa de reconciliação

O KillerBench encontrou um registro pendente de uma solicitação interrompida. Execute a mesma ação de novo. Ela verifica o que aconteceu e informa o resultado real sem repetir a solicitação. Na linha de comando, use o comando `-recover` correspondente, por exemplo `killerbench-cli bitlocker-auto-unlock-recover`.

## Uma verificação mostra Incompletas ou Desconhecido

Não foi possível ler as evidências, muitas vezes porque o KillerBench não estava sendo executado como administrador ou um serviço do Windows estava indisponível. O KillerBench informa isso com honestidade em vez de presumir que o computador está íntegro.

## Onde os arquivos ficam

Diários, registros pendentes e evidências salvas ficam em `%LOCALAPPDATA%\KillerBench`. Cada ação protegida tem a própria pasta. Guarde-os quando precisar de um registro do que mudou.

## Relatar um problema

O KillerBench está em desenvolvimento. Inclua o relatório exportado e o log de atividades ao relatar um problema, e remova tudo o que você não quiser compartilhar.
