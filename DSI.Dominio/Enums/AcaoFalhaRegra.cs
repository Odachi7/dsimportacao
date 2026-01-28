namespace DSI.Dominio.Enums;

/// <summary>
/// Ação a tomar quando uma regra falha
/// </summary>
public enum AcaoFalhaRegra
{
    /// <summary>
    /// Aplica valor default configurado
    /// </summary>
    AplicarDefault = 1,

    /// <summary>
    /// Pula a linha (não importa)
    /// </summary>
    PularLinha = 2,

    /// <summary>
    /// Para a execução do job
    /// </summary>
    PararJob = 3
}
