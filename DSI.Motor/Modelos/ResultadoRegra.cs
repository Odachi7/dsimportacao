using DSI.Dominio.Enums;

namespace DSI.Motor.Modelos;

/// <summary>
/// Resultado da aplicação de uma regra
/// </summary>
public class ResultadoRegra
{
    public bool Sucesso { get; set; }
    public object? ValorTransformado { get; set; }
    public string? MensagemErro { get; set; }
    public TipoRegra TipoRegra { get; set; }
}

/// <summary>
/// Resultado de transformação de uma linha completa
/// </summary>
public class ResultadoTransformacao
{
    public Dictionary<string, object?> ValoresTransformados { get; set; } = new();
    public List<ErroValidacao> Erros { get; set; } = new();
    public bool IsValida => !Erros.Any(e => e.Severidade == Severidade.Erro);
    public bool DevePular { get; set; }
}

/// <summary>
/// Erro de validação
/// </summary>
public class ErroValidacao
{
    public string Coluna { get; set; } = string.Empty;
    public TipoRegra TipoRegra { get; set; }
    public object? ValorOriginal { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public Severidade Severidade { get; set; }
}

public enum Severidade
{
    Aviso,
    Erro
}
