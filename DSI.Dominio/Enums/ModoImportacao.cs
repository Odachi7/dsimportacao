namespace DSI.Dominio.Enums;

/// <summary>
/// Modo de importação de dados
/// </summary>
public enum ModoImportacao
{
    /// <summary>
    /// Importação completa de todos os dados
    /// </summary>
    Completo = 1,

    /// <summary>
    /// Importação apenas de dados novos/alterados desde última execução
    /// </summary>
    Incremental = 2
}
