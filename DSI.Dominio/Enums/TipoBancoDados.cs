namespace DSI.Dominio.Enums;

/// <summary>
/// Tipos de bancos de dados suportados pelo sistema
/// </summary>
public enum TipoBancoDados
{
    /// <summary>
    /// MySQL ou MariaDB
    /// </summary>
    MySql = 1,

    /// <summary>
    /// Firebird
    /// </summary>
    Firebird = 2,

    /// <summary>
    /// PostgreSQL
    /// </summary>
    PostgreSql = 3,

    /// <summary>
    /// Microsoft SQL Server
    /// </summary>
    SqlServer = 4,

    /// <summary>
    /// Conexão via ODBC genérico
    /// </summary>
    Odbc = 5
}
