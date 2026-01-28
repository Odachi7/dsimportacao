using DSI.Dominio.Enums;

namespace DSI.Aplicacao.DTOs;

/// <summary>
/// DTO para criação de uma conexão
/// </summary>
public class CriarConexaoDto
{
    /// <summary>
    /// Nome da conexão
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do banco de dados
    /// </summary>
    public TipoBancoDados TipoBanco { get; set; }

    /// <summary>
    /// Modo de conexão
    /// </summary>
    public ModoConexao ModoConexao { get; set; }

    /// <summary>
    /// String de conexão (será criptografada)
    /// </summary>
    public string StringConexao { get; set; } = string.Empty;
}

/// <summary>
/// DTO para atualização de uma conexão
/// </summary>
public class AtualizarConexaoDto
{
    /// <summary>
    /// ID da conexão
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nome da conexão
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do banco de dados
    /// </summary>
    public TipoBancoDados TipoBanco { get; set; }

    /// <summary>
    /// Modo de conexão
    /// </summary>
    public ModoConexao ModoConexao { get; set; }

    /// <summary>
    /// String de conexão (será criptografada se alterada)
    /// </summary>
    public string? StringConexao { get; set; }
}

/// <summary>
/// DTO de retorno de conexão (sem dados sensíveis)
/// </summary>
public class ConexaoDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoBancoDados TipoBanco { get; set; }
    public ModoConexao ModoConexao { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public bool TemStringConexao { get; set; }
}
