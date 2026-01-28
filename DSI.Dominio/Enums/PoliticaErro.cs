namespace DSI.Dominio.Enums;

/// <summary>
/// Política de tratamento de erros durante importação
/// </summary>
public enum PoliticaErro
{
    /// <summary>
    /// Para a execução do job ao encontrar o primeiro erro
    /// </summary>
    PararNoPrimeiroErro = 1,

    /// <summary>
    /// Pula linhas inválidas e continua a importação
    /// </summary>
    PularLinhasInvalidas = 2,

    /// <summary>
    /// Aplica valores default quando possível e continua
    /// </summary>
    AplicarDefaultEContinuar = 3
}
