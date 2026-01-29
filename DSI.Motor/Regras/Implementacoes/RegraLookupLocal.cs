using DSI.Dominio.Enums;
using DSI.Motor.Modelos;
using DSI.Motor.Regras.Interfaces;
using System.Text.Json;

namespace DSI.Motor.Regras.Implementacoes;

/// <summary>
/// Regra: Lookup Local (De/Para) via JSON
/// Parametros: {"De": "Para", "A": "1", "B": "2"}
/// </summary>
public class RegraLookupLocal : IRegra
{
    public TipoRegra Tipo => TipoRegra.LookupLocal;

    public Task<ResultadoRegra> AplicarAsync(object? valor, string? parametros, IServiceProvider serviceProvider)
    {
        if (valor == null) return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = null, TipoRegra = Tipo });
        var str = valor.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(parametros))
             return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = str, TipoRegra = Tipo });

        try 
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(parametros, options);

            if (dict != null && dict.TryGetValue(str, out var novoValor))
            {
                return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = novoValor, TipoRegra = Tipo });
            }
        }
        catch
        {
            // Falha ao parsear ou buscar
        }

        // Se não achar, retorna original ou erro? MVP: Retorna Original (ou falha se configurado, mas aqui retorna sucesso com original por enqt)
        return Task.FromResult(new ResultadoRegra 
        { 
            Sucesso = true, 
            ValorTransformado = str, // Manteve original
            TipoRegra = Tipo 
        });
    }
}
