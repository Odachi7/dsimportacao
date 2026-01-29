namespace DSI.Aplicacao.DTOs;

/// <summary>
/// DTO para mapeamento de coluna
/// </summary>
public class MapeamentoDto
{
    public Guid? Id { get; set; }
    public string ColunaOrigem { get; set; } = string.Empty;
    public string ColunaDestino { get; set; } = string.Empty;
    public string TipoDestino { get; set; } = string.Empty;
    public bool Ignorada { get; set; }
    public string? ValorConstante { get; set; }
    public List<RegraDto> Regras { get; set; } = new();
}

/// <summary>
/// DTO para configurar mapeamentos de uma tabela
/// </summary>
public class ConfigurarMapeamentosDto
{
    public Guid TabelaJobId { get; set; }
    public List<MapeamentoDto> Mapeamentos { get; set; } = new();
}

/// <summary>
/// Resultado de auto-mapeamento
/// </summary>
public class ResultadoAutoMapeamento
{
    public List<MapeamentoDto> MapeamentosExatos { get; set; } = new();
    public List<SugestaoMapeamento> Sugestoes { get; set; } = new();
    public List<string> ColunasSemMapeamento { get; set; } = new();
}

/// <summary>
/// Sugestão de mapeamento por similaridade
/// </summary>
public class SugestaoMapeamento
{
    public string ColunaOrigem { get; set; } = string.Empty;
    public string ColunaDestinoSugerida { get; set; } = string.Empty;
    public int Similaridade { get; set; } // Percentual 0-100
}
