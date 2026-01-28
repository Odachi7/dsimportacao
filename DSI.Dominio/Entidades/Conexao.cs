using DSI.Dominio.Enums;

namespace DSI.Dominio.Entidades;

/// <summary>
/// Representa uma conexão com banco de dados (origem ou destino)
/// </summary>
public class Conexao
{
    /// <summary>
    /// Identificador único da conexão
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nome amigável da conexão
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do banco de dados
    /// </summary>
    public TipoBancoDados TipoBanco { get; set; }

    /// <summary>
    /// Modo de conexão (Nativo, ODBC DSN, ODBC Driver)
    /// </summary>
    public ModoConexao ModoConexao { get; set; }

    /// <summary>
    /// String de conexão criptografada (contém senha)
    /// </summary>
    public string StringConexaoCriptografada { get; set; } = string.Empty;

    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime CriadoEm { get; set; }

    /// <summary>
    /// Data da última atualização
    /// </summary>
    public DateTime AtualizadoEm { get; set; }
}
