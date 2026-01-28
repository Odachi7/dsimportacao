namespace DSI.Conectores.Abstracoes.Enums;

/// <summary>
/// Capacidades suportadas por um conector
/// </summary>
[Flags]
public enum CapacidadesConector
{
    /// <summary>
    /// Nenhuma capacidade especial
    /// </summary>
    Nenhuma = 0,

    /// <summary>
    /// Suporta transações
    /// </summary>
    Transacoes = 1 << 0,

    /// <summary>
    /// Suporta UPSERT nativo (ON CONFLICT, MERGE, etc)
    /// </summary>
    UpsertNativo = 1 << 1,

    /// <summary>
    /// Suporta bulk insert otimizado
    /// </summary>
    BulkInsert = 1 << 2,

    /// <summary>
    /// Suporta streaming de dados (cursor)
    /// </summary>
    Streaming = 1 << 3,

    /// <summary>
    /// Suporta descoberta automática de schema
    /// </summary>
    DescobertaSchema = 1 << 4,

    /// <summary>
    /// Suporta consultas parametrizadas
    /// </summary>
    ConsultasParametrizadas = 1 << 5,

    /// <summary>
    /// Suporta múltiplos conjuntos de resultados
    /// </summary>
    MultipleResultSets = 1 << 6
}
