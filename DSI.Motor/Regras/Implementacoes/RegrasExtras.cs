using DSI.Dominio.Enums;
using DSI.Motor.Modelos;
using DSI.Motor.Regras.Interfaces;
using System.Globalization;

namespace DSI.Motor.Regras.Implementacoes;

/// <summary>
/// Regra: Retorna null se string for vazia
/// </summary>
public class RegraNuloSeVazio : IRegra
{
    public TipoRegra Tipo => TipoRegra.NuloSeVazio;

    public Task<ResultadoRegra> AplicarAsync(object? valor, string? parametros, IServiceProvider serviceProvider)
    {
        if (valor == null || (valor is string str && string.IsNullOrWhiteSpace(str)))
        {
            return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = null, TipoRegra = Tipo });
        }
        return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = valor, TipoRegra = Tipo });
    }
}

/// <summary>
/// Regra: Aplica valor constante (ignora valor original)
/// </summary>
public class RegraValorConstante : IRegra
{
    public TipoRegra Tipo => TipoRegra.ValorConstante;

    public Task<ResultadoRegra> AplicarAsync(object? valor, string? parametros, IServiceProvider serviceProvider)
    {
        return Task.FromResult(new ResultadoRegra 
        { 
            Sucesso = true, 
            ValorTransformado = parametros, 
            TipoRegra = Tipo 
        });
    }
}

/// <summary>
/// Regra: Arredondar decimal
/// </summary>
public class RegraArredondar : IRegra
{
    public TipoRegra Tipo => TipoRegra.Arredondar;

    public Task<ResultadoRegra> AplicarAsync(object? valor, string? parametros, IServiceProvider serviceProvider)
    {
        if (valor == null) return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = null, TipoRegra = Tipo });

        int casas = 2;
        if (!string.IsNullOrEmpty(parametros) && int.TryParse(parametros, out int p))
            casas = p;

        decimal decimalVal = 0;
        if (valor is decimal d) decimalVal = d;
        else if (valor is double db) decimalVal = (decimal)db;
        else if (valor is float f) decimalVal = (decimal)f;
        else if (decimal.TryParse(valor.ToString(), out decimal parsed)) decimalVal = parsed;
        else
        {
             return Task.FromResult(new ResultadoRegra
             {
                 Sucesso = false,
                 ValorTransformado = valor,
                 MensagemErro = "Valor não é numérico para arredondamento",
                 TipoRegra = Tipo
             });
        }

        return Task.FromResult(new ResultadoRegra 
        { 
            Sucesso = true, 
            ValorTransformado = Math.Round(decimalVal, casas), 
            TipoRegra = Tipo 
        });
    }
}

/// <summary>
/// Regra: Converter para Booleano (S/N, 1/0, True/False)
/// </summary>
public class RegraToBool : IRegra
{
    public TipoRegra Tipo => TipoRegra.ConverterParaBool;

    public Task<ResultadoRegra> AplicarAsync(object? valor, string? parametros, IServiceProvider serviceProvider)
    {
        if (valor == null) return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = null, TipoRegra = Tipo });

        if (valor is bool b) return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = b, TipoRegra = Tipo });

        var str = valor.ToString()?.Trim().ToUpperInvariant();
        bool result = false;

        if (str == "S" || str == "SIM" || str == "Y" || str == "YES" || str == "1" || str == "TRUE")
            result = true;
        else if (str == "N" || str == "NAO" || str == "NO" || str == "0" || str == "FALSE")
            result = false;
        else
        {
            return Task.FromResult(new ResultadoRegra
            {
                Sucesso = false,
                ValorTransformado = valor,
                MensagemErro = $"Valor '{valor}' não reconhecido como booleano",
                TipoRegra = Tipo
            });
        }

        return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = result, TipoRegra = Tipo });
    }
}

/// <summary>
/// Regra: Tamanho Máximo (Trunca)
/// </summary>
public class RegraTamanhoMaximo : IRegra
{
    public TipoRegra Tipo => TipoRegra.TamanhoMaximo;

    public Task<ResultadoRegra> AplicarAsync(object? valor, string? parametros, IServiceProvider serviceProvider)
    {
        if (valor == null) return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = null, TipoRegra = Tipo });

        var str = valor.ToString();
        if (string.IsNullOrEmpty(str)) return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = str, TipoRegra = Tipo });

        int max = 255;
        if (!string.IsNullOrEmpty(parametros) && int.TryParse(parametros, out int p))
            max = p;

        if (str.Length > max)
        {
            // MVP: Trunca
            return Task.FromResult(new ResultadoRegra 
            { 
               Sucesso = true, 
               ValorTransformado = str.Substring(0, max), 
               TipoRegra = Tipo 
            });
        }

        return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = str, TipoRegra = Tipo });
    }
}

/// <summary>
/// Regra: Substituir Texto (Old|New)
/// </summary>
public class RegraSubstituir : IRegra
{
    public TipoRegra Tipo => TipoRegra.Substituir;

    public Task<ResultadoRegra> AplicarAsync(object? valor, string? parametros, IServiceProvider serviceProvider)
    {
        if (valor == null) return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = null, TipoRegra = Tipo });
        
        var str = valor.ToString() ?? "";
        if (string.IsNullOrEmpty(parametros) || !parametros.Contains('|'))
             return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = str, TipoRegra = Tipo });

        var parts = parametros.Split('|');
        if (parts.Length >= 2)
        {
            var oldVal = parts[0];
            var newVal = parts[1];
            return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = str.Replace(oldVal, newVal), TipoRegra = Tipo });
        }

        return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = str, TipoRegra = Tipo });
    }
}

/// <summary>
/// Regra: Default se Vazio
/// </summary>
public class RegraDefaultSeVazio : IRegra
{
    public TipoRegra Tipo => TipoRegra.DefaultSeVazio;

    public Task<ResultadoRegra> AplicarAsync(object? valor, string? parametros, IServiceProvider serviceProvider)
    {
        if (valor == null || (valor is string str && string.IsNullOrEmpty(str)))
        {
            return Task.FromResult(new ResultadoRegra 
            { 
                Sucesso = true, 
                ValorTransformado = parametros, 
                TipoRegra = Tipo 
            });
        }
        return Task.FromResult(new ResultadoRegra { Sucesso = true, ValorTransformado = valor, TipoRegra = Tipo });
    }
}
