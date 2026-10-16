# Dispositivos

Dispositivos abrange impressoras, áudio, câmeras, Bluetooth, entrada e os serviços do Windows dos quais o hardware conectado depende.

Guias passo a passo: [Corrigir problemas de impressão](fix-printing.html) e [Investigar falhas e telas azuis](investigate-crashes.html).

## Ferramentas desta seção

Impressão, áudio, câmeras, Bluetooth, entrada e os serviços por trás do hardware conectado.

### Impressão

Verifica o Spooler de Impressão, as pausas das filas locais, os trabalhos de impressão visíveis, as configurações offline e as condições informadas pelos drivers, e depois inventaria os registros de filas, drivers, portas, pontos de extremidade TCP/IP padrão, processadores de impressão e monitores de impressão. Só relaciona problemas recentes do PrintService quando há uma identidade exata de fila atual. Os pontos de extremidade TCP/IP padrão atribuídos recebem uma única tentativa de conexão delimitada, sem dados de aplicativo, SNMP nem trabalho de impressão. O reparo automático pode iniciar o spooler habilitado, retomar só filas exatas pausadas com evidências atuais de trabalhos aguardando e desativar o modo offline. Pausas intencionais sem trabalhos continuam apenas em revisão. Conexões remotas, cadeias de comunidade SNMP, registros de extensões e conteúdo dos trabalhos são preservados. Um código de condição ou uma conexão bem-sucedida não estabelecem a identidade da impressora, os suprimentos, a mecânica, a aceitação pela fila, o sucesso da renderização nem a impressão física.

### Áudio

Verifica as identidades e os formatos compartilhados da entrada e da saída multimídia padrão, o estado dos dispositivos e drivers de áudio, o Construtor de Ponto de Extremidade de Áudio do Windows e o Áudio do Windows. O reparo automático inicia os serviços habilitados e parados e verifica que estão em execução. Configurações de inicialização, drivers, dispositivos selecionados, modo exclusivo e níveis de volume são preservados. A reprodução e a gravação de som e o roteamento dos aplicativos continuam sem teste.

### Suporte a câmeras

Inspeciona erros de dispositivos de câmera e imagem, IDs de hardware, evidências de drivers instalados e a disponibilidade do Servidor de Quadros da Câmera do Windows. A recuperação opcional só inicia um serviço habilitado e parado após verificações de dependências. Um aplicativo que já esteja solicitando acesso à câmera pode ser retomado com as permissões existentes. Configurações de privacidade da câmera, permissões, configuração dos dispositivos, política, drivers e tipo de inicialização são preservados. Sem redefinição, alteração de permissões ou instalação de drivers. A verificação cobre só a configuração informada do dispositivo e o estado em execução do serviço, não a captura de imagens, o acesso de privacidade nem o funcionamento físico.

### Suporte a Bluetooth

Inspeciona erros de dispositivos Bluetooth, IDs de hardware, evidências de drivers instalados e a disponibilidade do Serviço de Suporte Bluetooth. A recuperação opcional só inicia um serviço habilitado e parado após verificações de dependências; dispositivos emparelhados podem se reconectar. Emparelhamentos, configurações de rádio, configuração dos dispositivos, política e tipo de inicialização são preservados. Sem redefinição, remoção de dispositivos ou instalação de drivers. A verificação cobre só a configuração informada do dispositivo e o estado em execução do serviço, não a descoberta, o emparelhamento, o áudio nem a transferência de dados.

### Suporte a cartões inteligentes

Inspeciona erros de leitores de cartões inteligentes, IDs de hardware, evidências de drivers instalados e a disponibilidade do serviço Cartão Inteligente. A recuperação opcional só inicia um serviço habilitado e parado após verificações de dependências. Os aplicativos podem voltar a detectar cartões inseridos conforme a política existente. Certificados, PINs, credenciais, configuração de leitores e cartões, política, drivers e tipo de inicialização são preservados. Sem inicialização, registro, operação de PIN, redefinição de leitor ou instalação de drivers. A verificação cobre só a configuração informada do leitor e o estado em execução do serviço, não a detecção de cartões, a autenticação nem o acesso dos aplicativos.

### Suporte biométrico

Inspeciona erros de dispositivos biométricos, IDs de hardware, evidências de drivers instalados e a disponibilidade do Serviço de Biometria do Windows. A recuperação opcional só inicia um serviço habilitado e parado após verificações de dependências. As solicitações existentes do Windows Hello e de aplicativos podem ser retomadas conforme a política atual. Modelos biométricos, dados do Windows Hello, credenciais, configuração dos dispositivos, permissões, política, drivers e tipo de inicialização são preservados. Sem registro, exclusão de modelos, redefinição de sensores, alteração de permissões ou instalação de drivers. A verificação cobre só a configuração informada do sensor e o estado em execução do serviço, não a captura, a correspondência, a entrada nem o acesso dos aplicativos.

### Serviço de Dispositivo de Interface Humana

Verifica a disponibilidade do Serviço de Dispositivo de Interface Humana. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As teclas de atalho, os controles de mídia e outros recursos HID existentes podem ser retomados. Dispositivos, drivers, mapeamentos de botões, configurações de acessibilidade, configurações de entrada, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de dispositivo, driver, mapeamento ou configuração de entrada. A verificação cobre só o estado estável do serviço, não o comportamento de botões, controles ou aplicativos.

### Serviço Enumerador de Dispositivos Portáteis

Verifica a disponibilidade do Serviço Enumerador de Dispositivos Portáteis. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. Os aplicativos existentes podem voltar a detectar e acessar dispositivos portáteis conectados. Dispositivos, drivers, arquivos, bibliotecas, configurações de sincronização, permissões, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de dispositivo, arquivo, biblioteca, sincronização ou transferência. A verificação cobre só o estado estável do serviço, não a enumeração, o acesso, a sincronização nem a transferência.

### Aquisição de Imagens do Windows (WIA)

Verifica a disponibilidade do serviço Aquisição de Imagens do Windows. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As solicitações existentes de scanner e de aquisição de imagens estáticas podem ser retomadas com as permissões atuais dos aplicativos. Dispositivos, drivers, dados de imagem, permissões dos aplicativos, configurações de privacidade, configurações de transferência, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de dispositivo, driver, imagem, permissão, transferência ou aquisição. A verificação cobre só o estado estável do serviço, não o funcionamento do scanner, da câmera, da transferência nem dos aplicativos.

### Detecção de Hardware do Shell

Inspeciona evidências Plug and Play delimitadas de controladoras de armazenamento, discos e dispositivos portáteis e as referências de drivers instalados, e depois verifica a disponibilidade do serviço Detecção de Hardware do Shell. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As notificações existentes de Reprodução Automática e de chegada de hardware podem ser retomadas conforme a política atual. Observações de dispositivos e o histórico guardado não podem autorizar uma recuperação. Dispositivos, drivers, volumes, pontos de montagem, configurações de Reprodução Automática, manipuladores, política, permissões do serviço e tipo de inicialização são preservados. Nenhuma operação de dispositivo ou configuração é solicitada. A verificação cobre só o estado estável do serviço, não a entrega de notificações, a prontidão da mídia, o acesso aos volumes nem o funcionamento físico dos dispositivos.

### Serviço de Instalação de Dispositivo

Verifica a disponibilidade do Serviço de Instalação de Dispositivo. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. O trabalho de instalação de dispositivos solicitado pelo Windows pode ser retomado conforme a política atual. Dispositivos, drivers, pacotes, configuração, associações, configurações de classe, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nem instala nada. A verificação cobre só o estado estável do serviço, não a instalação nem o funcionamento dos dispositivos.

### Serviço de Associação de Dispositivo

Verifica o histórico recente de configuração de dispositivos e de Plug and Play, e depois a disponibilidade do Serviço de Associação de Dispositivo. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. O trabalho de associação de dispositivos solicitado pelo Windows pode ser retomado. Dispositivos emparelhados e associados, drivers, credenciais, metadados, permissões, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de dispositivo, emparelhamento, remoção, redefinição ou driver. A verificação cobre só o estado estável do serviço, não a descoberta, o emparelhamento, a associação nem o funcionamento dos dispositivos.

### Serviço de Aprimoramento de Vídeo

Inspeciona as evidências de adaptadores gráficos e monitores, e depois verifica a disponibilidade do Serviço de Aprimoramento de Vídeo. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As solicitações existentes de brilho, cor e aprimoramento podem ser retomadas. Configuração da tela, brilho, perfis de cor, configurações de HDR, dispositivos, drivers, configurações de energia, política, permissões do serviço e tipo de inicialização são preservados. Nenhuma configuração de tela é alterada. A verificação cobre só o estado estável do serviço, não o comportamento do aprimoramento, a qualidade da imagem nem o funcionamento físico da tela.

### Serviço de Gerenciamento de Entrada de Texto

Inspeciona as evidências de teclado, mouse e dispositivos HID, e depois verifica a disponibilidade do Serviço de Gerenciamento de Entrada de Texto. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. As solicitações existentes de teclado virtual, escrita à mão, emoji e entrada de texto avançada podem ser retomadas. Dicionários, dados de escrita à mão, configurações de idioma e teclado, configurações de acessibilidade e privacidade, dispositivos, drivers, política, permissões do serviço e tipo de inicialização são preservados. Nenhum texto digitado, tecla pressionada ou conteúdo escrito à mão é coletado. A verificação cobre só o estado estável do serviço, não a digitação, o reconhecimento nem o funcionamento dos dispositivos.

### Serviço de Cache de Fontes do Windows

Verifica a disponibilidade do Serviço de Cache de Fontes do Windows. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. Os aplicativos existentes podem voltar a usar o cache de fontes atual. Fontes instaladas, arquivos de fontes, dados de aplicativos, arquivos de cache, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não instala nem remove nenhuma fonte e não exclui nem reconstrói o cache. A verificação cobre só o estado estável do serviço, não a renderização de texto nem o comportamento dos aplicativos.

### Host de Provedor da Descoberta de Função

Verifica a disponibilidade do Host de Provedor da Descoberta de Função. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. Os aplicativos existentes podem voltar a descobrir dispositivos de rede pelos provedores instalados. Perfis de rede, compartilhamentos, dispositivos, provedores, regras do firewall, configurações de descoberta, credenciais, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de dispositivo, compartilhamento, provedor, regra de firewall ou configuração de descoberta. A verificação cobre só o estado estável do serviço, não a integridade dos provedores, a descoberta de dispositivos nem a conectividade.

### Publicação de Recurso de Descoberta de Função

Verifica a disponibilidade da Publicação de Recurso de Descoberta de Função. A recuperação opcional só inicia o serviço exato habilitado e parado após novas verificações de dependências. Este PC pode passar a ser detectável pelas configurações existentes de descoberta de rede e do firewall. Perfis de rede, compartilhamentos, recursos publicados, regras do firewall, configurações de descoberta, credenciais, política, permissões do serviço e tipo de inicialização são preservados. O KillerBench não seleciona nenhuma operação de compartilhamento, recurso, regra de firewall ou configuração de descoberta. A verificação cobre só o estado estável do serviço, não a publicação, a visibilidade, o acesso nem a conectividade.
