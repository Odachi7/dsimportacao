namespace DSI.Dominio.Enums;

/// <summary>
/// Tipo de lookup (De/Para)
/// </summary>
public enum TipoLookup
{
    /// <summary>
    /// Lista local em memória (pares chave-valor)
    /// </summary>
    ListaLocal = 1,

    /// <summary>
    /// Consulta em tabela de banco de dados
    /// </summary>
    TabelaBancoDados = 2
}
