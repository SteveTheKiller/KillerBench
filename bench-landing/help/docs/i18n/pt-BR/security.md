# Segurança

Segurança inspeciona o Microsoft Defender, o firewall, os certificados e os serviços de entrada. Ela também mostra o estado do TPM e da segurança da plataforma, dos quais o BitLocker e o Windows Hello dependem.

Guias passo a passo: [Criptografar uma unidade com o BitLocker](encrypt-with-bitlocker.html) e [Deixar o Windows mais privado](make-windows-private.html).

## Ferramentas desta seção

Inspecione a proteção, o firewall, os certificados e os serviços de entrada.

### Microsoft Defender

Inspeciona o status da proteção e das assinaturas do Defender. O reparo automático só atualiza as assinaturas quando o Defender informa proteção ativa e assinaturas desatualizadas. Usa as origens de assinaturas configuradas. Sem alterações de proteção, exclusões ou políticas; sem verificação de malware.

### Central de Segurança do Windows

Revisa de forma delimitada os registros de antivírus com o estado bruto do provedor, o histórico exato de falhas do serviço e a disponibilidade atual da Central de Segurança do Windows, sem interpretar a integridade da proteção. Um inventário vazio não é um veredito sobre a proteção, e a quantidade de registros, o estado bruto ou as falhas históricas não podem autorizar uma recuperação nem uma alteração de segurança. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. O monitoramento do status de segurança e as notificações existentes podem ser retomados. Configuração do antivírus, registros de provedores, configurações de proteção, exclusões, notificações, política e tipo de inicialização são preservados. Nenhuma verificação, atualização ou alteração de proteção é solicitada. A verificação cobre só o estado estável do serviço, não a presença de antivírus, o estado da proteção, a atualização das assinaturas, a entrega de notificações, a propriedade dos registros nem a política efetiva.

### Serviços de firewall

Lê as configurações de ativação dos perfis Domínio, Privado e Público no ActiveStore e depois verifica os serviços Mecanismo de Filtragem Base e Firewall do Windows Defender. A recuperação opcional só inicia serviços habilitados e parados após novas verificações de dependências. A política de filtragem existente pode entrar em vigor e interromper o acesso de rede ou remoto. Tipos de inicialização, permissões dos serviços, regras do firewall, perfis e configurações de IPsec são preservados. Sem redefinições, alterações de regras ou paradas de serviços. A verificação só estabelece que o serviço está em execução; a proteção efetiva e a conectividade continuam sem verificação.

### Vencimento de certificados

Inspeciona as datas dos certificados e monta cadeias de confiança locais apenas com o cache para os repositórios pessoais do computador e do usuário atual, e retorna uma única conclusão operacional delimitada. Revogação, apresentação no servidor, associações, registro, controle por política, confiança remota e utilidade da chave privada continuam sem verificação. Nenhum certificado, configuração de confiança, registro, associação, chave privada, aplicativo, serviço ou política é alterado.

### Gerenciador de Credenciais

Verifica a disponibilidade do serviço Gerenciador de Credenciais. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. Os aplicativos existentes podem retomar as solicitações de credenciais armazenadas. Conteúdo do cofre, senhas, chaves de acesso, certificados, tokens, registros de credenciais, permissões, política, permissões do serviço e tipo de inicialização são preservados. Nenhum conteúdo ou operação de credenciais é solicitado. A verificação cobre só o estado estável do serviço, não a recuperação de credenciais, a autenticação nem o acesso dos aplicativos.

### Gerenciador de Contas da Web

Revisa o histórico do agente de contas com carga útil anonimizada e a disponibilidade exata do Gerenciador de Contas da Web. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. Os aplicativos existentes podem retomar solicitações de contas e tokens conforme a política atual. Eventos históricos não podem autorizar uma recuperação nem estabelecer o estado atual das contas. Contas, credenciais, tokens, cookies, registros de aplicativos, permissões, estado de entrada, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de conta, credencial, token, cookie, permissão ou entrada. A verificação cobre só o estado estável do serviço, não a autenticação, a emissão de tokens nem o acesso dos aplicativos.

### Assistente de Entrada da Conta da Microsoft

Revisa o histórico do agente de contas com carga útil anonimizada e a disponibilidade exata do Assistente de Entrada da Conta da Microsoft. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As solicitações existentes de entrada com conta da Microsoft podem ser retomadas conforme a política atual. Eventos históricos não podem autorizar uma recuperação nem estabelecer o estado atual das contas. Contas, credenciais, tokens, registro do dispositivo, dados de aplicativos, permissões, estado de entrada, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de conta, credencial, token, registro, permissão ou entrada. A verificação cobre só o estado estável do serviço, não a autenticação, a conectividade da conta nem o acesso dos aplicativos.
