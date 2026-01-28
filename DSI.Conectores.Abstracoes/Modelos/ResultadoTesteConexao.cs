namespace DSI.Conectores.Abstracoes.Modelos;

/// <summary>
/// Resultado de um teste de conexão
/// </summary>
public class ResultadoTesteConexao
{
    /// <summary>
    /// Indica se a conexão foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem descritiva do resultado
    /// </summary>
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>
    /// Versão do servidor de banco de dados
    /// </summary>
    public string? VersaoServidor { get; set; }

    /// <summary>
    /// Nome do banco de dados conectado
    /// </summary>
    public string? NomeBancoDados { get; set; }

    /// <summary>
    /// Tempo de resposta em milissegundos
    /// </summary>
    public long TempoRespostaMs { get; set; }

    /// <summary>
    /// Detalhes do erro (se houver)
    /// </summary>
    public string? DetalhesErro { get; set; }
}
