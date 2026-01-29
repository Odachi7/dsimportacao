using DSI.Dominio.Enums;
using DSI.Motor.Modelos;

namespace DSI.Motor.Regras.Interfaces;

/// <summary>
/// Interface base para todas as regras de transformação
/// </summary>
public interface IRegra
{
    TipoRegra Tipo { get; }
    Task<ResultadoRegra> AplicarAsync(object? valor, string? parametros, IServiceProvider serviceProvider);
}
