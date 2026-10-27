# Implantação

Implantação prepara um computador novo. Ela revisa os aplicativos e a configuração que você seleciona e dá nome ao computador. Ela não baixa nem instala nada por conta própria.

Guias passo a passo: [Configurar um computador novo](set-up-new-pc.html).

## Verificar um instalador antes de executá-lo

A linha de comando pode verificar o instalador de um fabricante antes que você o execute:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

O KillerBench só confia em um instalador quando ele tem uma assinatura Authenticode válida com o nome exato da organização do fabricante. Ele nunca executa o instalador.

## Ferramentas desta seção

Prepare um computador novo: aplicativos, nome e configuração.

### Implantação

Revisa os aplicativos de provisionamento e os grupos de configuração selecionados explicitamente. Os registros atuais de desinstalação, de pacotes de aplicativos e do Office Clique para Executar são comparados sem baixar nem executar instaladores. As seleções de configuração descrevem as consequências e encaminham para fluxos de trabalho protegidos separados quando existem. Não ocorre nenhuma ação de download, instalação, conta, atualização, limpeza, política, registro, serviço, perfil ou reinicialização.

### Nome do computador

Compara o nome proposto para o computador com a identidade atual e a função no domínio. Sem renomeação, sem coleta de credenciais e sem reinicialização. A disponibilidade do nome, as permissões e as dependências de aplicativos continuam sem verificação.
