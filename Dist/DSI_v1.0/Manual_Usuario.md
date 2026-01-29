# Manual do Usuário - DS Importer (DSI)

## Introdução
O DS Importer é uma ferramenta para importar, transformar e carregar dados entre diferentes bancos de dados (ETL).

## Fluxo de Trabalho
O processo principal envolve a criação e execução de **Jobs**.

### 1. Gerenciar Conexões
Antes de criar um Job, cadastre suas conexões de banco de dados:
1. Vá até a aba **Conexões**.
2. Clique em **Nova Conexão**.
3. Selecione o tipo de banco (MySQL, PostgreSQL, SQL Server, etc.).
4. Preencha os dados e clique em **Testar Conexão**.
5. Salve.

### 2. Criar um Job
1. Na aba **Dashboard**, clique em **Novo Job**.
2. Siga o assistente passo-a-passo:
   - **Passo A**: Nomeie o Job.
   - **Passo B**: Selecione Origem e Destino.
   - **Passo C**: Escolha as tabelas que deseja importar.
   - **Passo D**: Revise o mapeamento de colunas.
   - **Passo E**: Configure regras de transformação (ex: Converter maiúsculas, Substituir vazios).
   - **Passo F**: Defina o tamanho do lote e comportamento em caso de erro.
   - **Passo G**: Confirme e salve.

### 3. Executar o Job
1. No Dashboard, localize o Job criado.
2. Clique em **Executar**.
3. Você será redirecionado para a tela de monitoramento.
4. Acompanhe o progresso em tempo real.

## Consultando Histórico
Vá até a aba **Histórico** para ver todas as execuções passadas, verificar status detalhado e logs de erros.

## Troubleshooting
- **Erro de Conexão**: Verifique firewall e credenciais.
- **Banco Bloqueado**: Se usar SQLite, evite abrir o arquivo em outros programas durante a execução.
- **Logs**: Logs técnicos detalhados ficam em `%AppData%\DsImporter\logs`.
