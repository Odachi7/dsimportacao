namespace DSI.Dominio.Enums;

/// <summary>
/// Modo de conexão com o banco de dados
/// </summary>
public enum ModoConexao
{
    /// <summary>
    /// Conexão nativa usando o provider específico do banco
    /// </summary>
    Nativo = 1,

    /// <summary>
    /// Conexão via DSN (Data Source Name) configurado no ODBC
    /// </summary>
    OdbcDsn = 2,

    /// <summary>
    /// Conexão via driver ODBC com parâmetros personalizados
    /// </summary>
    OdbcDriver = 3
}
