using DSI.Dominio.Enums;

namespace DSI.Aplicacao.DTOs;

/// <summary>
/// DTO para regra de transformação
/// </summary>
public class RegraDto
{
    public Guid? Id { get; set; }
    public TipoRegra TipoRegra { get; set; }
    public int Ordem { get; set; }
    public string? Parametros { get; set; }
}
