# Ds Importer (DSI)

> **Sistema ETL visual para importação de dados entre bancos heterogêneos**

## 📋 Sobre o Projeto

O **Ds Importer (DSI)** é um sistema voltado para usuários não-programadores que permite configurar e executar importações de dados entre bancos de dados heterogêneos (MySQL, Firebird, PostgreSQL, SQL Server e outros via ODBC), com mapeamento visual de colunas, regras de tratamento/validação, logs completos, auditoria e reprocessamento.

## 🏗️ Estrutura da Solution

A solution é organizada em **16 projetos** separados por responsabilidade:

### Camada de Domínio
- **DSI.Dominio**: Entidades, enums e modelos de negócio
  - ✅ 9 Enums (TipoBancoDados, ModoConexao, ModoImportacao, PoliticaErro, EstrategiaConflito, TipoRegra, StatusExecucao, AcaoFalhaRegra, TipoLookup)
  - ✅ 8 Entidades (Conexao, Job, TabelaJob, Mapeamento, Regra, Execucao, EstatisticaTabelaExecucao, ErroExecucao)

### Camada de Aplicação
- **DSI.Aplicacao**: Casos de uso e serviços de aplicação
- **DSI.Motor**: Pipeline ETL (Extract, Transform, Load)

### Camada de Conectores
- **DSI.Conectores.Abstracoes**: Interfaces e tipos comuns para conectores
- **DSI.Conectores.MySql**: Conector para MySQL/MariaDB
- **DSI.Conectores.Firebird**: Conector para Firebird
- **DSI.Conectores.PostgreSql**: Conector para PostgreSQL
- **DSI.Conectores.SqlServer**: Conector para SQL Server
- **DSI.Conectores.Odbc**: Conector universal via ODBC

### Infraestrutura
- **DSI.Persistencia**: Camada de persistência SQLite
- **DSI.Seguranca**: Criptografia de credenciais (DPAPI)
- **DSI.Logging**: Sistema de logs dual (amigável + técnico)
- **DSI.Relatorios**: Geração e exportação de relatórios

### Interface e Testes
- **DSI.Desktop**: Interface WPF (Windows Presentation Foundation)
- **DSI.Testes.Unitarios**: Testes unitários (xUnit)
- **DSI.Testes.Integracao**: Testes de integração (xUnit)

## ✅ Status Atual

### Fase 1: Fundação e Infraestrutura - ✅ CONCLUÍDA
- ✅ Solution criada com 16 projetos
- ✅ Todos os projetos compilando com sucesso
- ✅ Modelo de domínio completo em português
- ✅ 9 enums de negócio configurados
- ✅ 8 entidades principais criadas

**Build Status**: ✅ Construir êxito em 4,1s

## 🚀 Tecnologias

- **.NET 9.0**: Framework principal
- **WPF**: Interface desktop
- **SQLite**: Banco de dados interno
- **xUnit**: Framework de testes
- **ODBC**: Suporte universal a bancos de dados

## 📦 Como Compilar

```powershell
# Restaurar dependências e compilar
dotnet build

# Executar testes
dotnet test

# Executar aplicação desktop
dotnet run --project DSI.Desktop
```

## 🎯 Próximos Passos

1. Implementar camada de persistência SQLite
2. Configurar injeção de dependências
3. Implementar framework de conectores
4. Desenvolver motor ETL
5. Criar interface WPF

## 📝 Convenções de Código

- **Idioma**: Todo código, comentários e documentação em português
- **Nomenclatura**: PascalCase para classes, camelCase para variáveis locais
- **Documentação**: XML comments em todos os tipos públicos
- **Testes**: Garantir 100% de funcionamento antes de avançar para próxima fase

## 📄 Licença

Projeto desenvolvido para gerenciamento de importações de dados empresariais.

---

**Versão**: 0.1.0 (MVP em desenvolvimento)  
**Última atualização**: 27/01/2026
