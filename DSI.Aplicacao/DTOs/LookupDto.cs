using DSI.Dominio.Enums;

namespace DSI.Aplicacao.DTOs;

/// <summary>
/// DTO para criar Lookup
/// </summary>
public class CriarLookupDto
{
    public Guid MapeamentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoLookup Tipo { get; set; }
    
    // Para Lookup local
    public Dictionary<string, string>? ValoresLocais { get; set; }
    
    // Para Lookup em banco
    public Guid? ConexaoBancoId { get; set; }
    public string? TabelaBanco { get; set; }
    public string? ColunaChave { get; set; }
    public string? ColunaValor { get; set; }
    
    public string? ValorPadrao { get; set; }
}

/// <summary>
/// DTO de retorno de Lookup
/// </summary>
public class LookupDto
{
    public Guid Id { get; set; }
    public Guid MapeamentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoLookup Tipo { get; set; }
    public int QuantidadeItens { get; set; }
    public string? ValorPadrao { get; set; }
}

/// <summary>
/// Item de lookup local
/// </summary>
public class ItemLookupDto
{
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}
