using DSI.Dominio.Enums;
using DSI.Motor.Modelos;
using DSI.Motor.Regras.Interfaces;

namespace DSI.Motor.Regras.Implementacoes;

/// <summary>
/// Regra: Campo obrigatório (não aceita null ou empty)
/// </summary>
public class RegraObrigatorio : IRegra
{
    public TipoRegra Tipo => TipoRegra.Obrigatorio;

    public ResultadoRegra Aplicar(object? valor, string? parametros = null)
    {
        if (valor == null || (valor is string str && string.IsNullOrWhiteSpace(str)))
        {
            return new ResultadoRegra
            {
                Sucesso = false,
                ValorTransformado = valor,
                MensagemErro = "Campo obrigatório não pode ser vazio",
                TipoRegra = Tipo
            };
        }

        return new ResultadoRegra { Sucesso = true, ValorTransformado = valor, TipoRegra = Tipo };
    }
}

/// <summary>
/// Regra: Aplicar valor padrão se null
/// </summary>
public class RegraDefaultSeNulo : IRegra
{
    public TipoRegra Tipo => TipoRegra.DefaultSeNulo;

    public ResultadoRegra Aplicar(object? valor, string? parametros = null)
    {
        if (valor == null)
        {
            return new ResultadoRegra
            {
                Sucesso = true,
                ValorTransformado = parametros,
                TipoRegra = Tipo
            };
        }

        return new ResultadoRegra { Sucesso = true, ValorTransformado = valor, TipoRegra = Tipo };
    }
}

/// <summary>
/// Regra: Trim (remove espaços)
/// </summary>
public class RegraTrim : IRegra
{
    public TipoRegra Tipo => TipoRegra.Trim;

    public ResultadoRegra Aplicar(object? valor, string? parametros = null)
    {
        if (valor is string str)
        {
            return new ResultadoRegra
            {
                Sucesso = true,
                ValorTransformado = str.Trim(),
                TipoRegra = Tipo
            };
        }

        return new ResultadoRegra { Sucesso = true, ValorTransformado = valor, TipoRegra = Tipo };
    }
}

/// <summary>
/// Regra: Converter para inteiro
/// </summary>
public class RegraToInt : IRegra
{
    public TipoRegra Tipo => TipoRegra.ConverterParaInt;

    public ResultadoRegra Aplicar(object? valor, string? parametros = null)
    {
        if (valor == null)
            return new ResultadoRegra { Sucesso = true, ValorTransformado = null, TipoRegra = Tipo };

        if (valor is int)
            return new ResultadoRegra { Sucesso = true, ValorTransformado = valor, TipoRegra = Tipo };

        var strValor = valor.ToString();
        if (int.TryParse(strValor, out int resultado))
        {
            return new ResultadoRegra { Sucesso = true, ValorTransformado = resultado, TipoRegra = Tipo };
        }

        return new ResultadoRegra
        {
            Sucesso = false,
            ValorTransformado = valor,
            MensagemErro = $"Não foi possível converter '{valor}' para inteiro",
            TipoRegra = Tipo
        };
    }
}

/// <summary>
/// Regra: Converter para decimal
/// </summary>
public class RegraToDecimal : IRegra
{
    public TipoRegra Tipo => TipoRegra.ConverterParaDecimal;

    public ResultadoRegra Aplicar(object? valor, string? parametros = null)
    {
        if (valor == null)
            return new ResultadoRegra { Sucesso = true, ValorTransformado = null, TipoRegra = Tipo };

        if (valor is decimal)
            return new ResultadoRegra { Sucesso = true, ValorTransformado = valor, TipoRegra = Tipo };

        var strValor = valor.ToString();
        if (decimal.TryParse(strValor, out decimal resultado))
        {
            return new ResultadoRegra { Sucesso = true, ValorTransformado = resultado, TipoRegra = Tipo };
        }

        return new ResultadoRegra
        {
            Sucesso = false,
            ValorTransformado = valor,
            MensagemErro = $"Não foi possível converter '{valor}' para decimal",
            TipoRegra = Tipo
        };
    }
}

/// <summary>
/// Reg ra: Parse de data (múltiplos formatos)
/// </summary>
public class RegraParseData : IRegra
{
    public TipoRegra Tipo => TipoRegra.ConverterParaData;

    private readonly string[] _formatosPadrao = new[]
    {
        "dd/MM/yyyy",
        "yyyy-MM-dd",
        "dd-MM-yyyy",
        "MM/dd/yyyy",
        "yyyy/MM/dd"
    };

    public ResultadoRegra Aplicar(object? valor, string? parametros = null)
    {
        if (valor == null)
            return new ResultadoRegra { Sucesso = true, ValorTransformado = null, TipoRegra = Tipo };

        if (valor is DateTime)
            return new ResultadoRegra { Sucesso = true, ValorTransformado = valor, TipoRegra = Tipo };

        var strValor = valor.ToString();
        
        // Tenta parse com formatos configurados ou padrão
        var formatos = string.IsNullOrWhiteSpace(parametros) 
            ? _formatosPadrao 
            : parametros.Split(';');

        if (DateTime.TryParseExact(strValor, formatos, null, System.Globalization.DateTimeStyles.None, out DateTime resultado))
        {
            return new ResultadoRegra { Sucesso = true, ValorTransformado = resultado, TipoRegra = Tipo };
        }

        return new ResultadoRegra
        {
            Sucesso = false,
            ValorTransformado = valor,
            MensagemErro = $"Não foi possível converter '{valor}' para data",
            TipoRegra = Tipo
        };
    }
}

/// <summary>
/// Regra: Upper case
/// </summary>
public class RegraUpper : IRegra
{
    public TipoRegra Tipo => TipoRegra.Maiuscula;

    public ResultadoRegra Aplicar(object? valor, string? parametros = null)
    {
        if (valor is string str)
        {
            return new ResultadoRegra
            {
                Sucesso = true,
                ValorTransformado = str.ToUpperInvariant(),
                TipoRegra = Tipo
            };
        }

        return new ResultadoRegra { Sucesso = true, ValorTransformado = valor, TipoRegra = Tipo };
    }
}

/// <summary>
/// Regra: Lower case
/// </summary>
public class RegraLower : IRegra
{
    public TipoRegra Tipo => TipoRegra.Minuscula;

    public ResultadoRegra Aplicar(object? valor, string? parametros = null)
    {
        if (valor is string str)
        {
            return new ResultadoRegra
            {
                Sucesso = true,
                ValorTransformado = str.ToLowerInvariant(),
                TipoRegra = Tipo
            };
        }

        return new ResultadoRegra { Sucesso = true, ValorTransformado = valor, TipoRegra = Tipo };
    }
}
