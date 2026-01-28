namespace DSI.Dominio.Enums;

/// <summary>
/// Estratégia para resolução de conflitos de chave duplicada
/// </summary>
public enum EstrategiaConflito
{
    /// <summary>
    /// Apenas insere novos registros (erro em duplicatas)
    /// </summary>
    ApenasInserir = 1,

    /// <summary>
    /// Pula registros duplicados silenciosamente
    /// </summary>
    PularSeExistir = 2,

    /// <summary>
    /// Usa UPSERT nativo do banco (ON CONFLICT, MERGE, etc)
    /// </summary>
    UpsertSeSuportado = 3,

    /// <summary>
    /// Tenta UPDATE primeiro, se falhar faz INSERT (mais lento)
    /// </summary>
    DoisPassos = 4
}
