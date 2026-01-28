using DSI.Dominio.Enums;

namespace DSI.Aplicacao.DTOs;

/// <summary>
/// DTO para criação de um Job
/// </summary>
public class CriarJobDto
{
    public string Nome { get; set; } = string.Empty;
    public Guid ConexaoOrigemId { get; set; }
    public Guid ConexaoDestinoId { get; set; }
    public ModoImportacao Modo { get; set; }
    public int TamanhoLote { get; set; } = 1000;
    public PoliticaErro PoliticaErro { get; set; }
    public EstrategiaConflito EstrategiaConflito { get; set; }
}

/// <summary>
/// DTO para atualização de um Job
/// </summary>
public class AtualizarJobDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid ConexaoOrigemId { get; set; }
    public Guid ConexaoDestinoId { get; set; }
    public ModoImportacao Modo { get; set; }
    public int TamanhoLote { get; set; }
    public PoliticaErro PoliticaErro { get; set; }
    public EstrategiaConflito EstrategiaConflito { get; set; }
}

/// <summary>
/// DTO de retorno de Job
/// </summary>
public class JobDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid ConexaoOrigemId { get; set; }
    public string ConexaoOrigemNome { get; set; } = string.Empty;
    public Guid ConexaoDestinoId { get; set; }
    public string ConexaoDestinoNome { get; set; } = string.Empty;
    public ModoImportacao Modo { get; set; }
    public int TamanhoLote { get; set; }
    public PoliticaErro PoliticaErro { get; set; }
    public EstrategiaConflito EstrategiaConflito { get; set; }
    public int QuantidadeTabelas { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

/// <summary>
/// DTO para configuração de tabela no Job
/// </summary>
public class TabelaJobDto
{
    public Guid? Id { get; set; }
    public string TabelaOrigem { get; set; } = string.Empty;
    public string TabelaDestino { get; set; } = string.Empty;
    public int OrdemExecucao { get; set; }
    public string? ColunaIncremental { get; set; }
    public string? UltimoCheckpoint { get; set; }
}

/// <summary>
/// DTO completo do Job com todas as tabelas e mapeamentos
/// </summary>
public class JobCompletoDto
{
    public JobDto Job { get; set; } = new();
    public List<TabelaJobDto> Tabelas { get; set; } = new();
}
