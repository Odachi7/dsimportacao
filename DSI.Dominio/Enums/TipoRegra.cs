namespace DSI.Dominio.Enums;

/// <summary>
/// Tipos de regras de transformação e validação aplicáveis por coluna
/// </summary>
public enum TipoRegra
{
    // Regras de presença
    /// <summary>
    /// Campo obrigatório (não aceita null/empty)
    /// </summary>
    Obrigatorio = 1,

    /// <summary>
    /// Aplica valor padrão se null
    /// </summary>
    DefaultSeNulo = 2,

    /// <summary>
    /// Aplica valor padrão se vazio
    /// </summary>
    DefaultSeVazio = 3,

    /// <summary>
    /// Retorna null se vazio
    /// </summary>
    NuloSeVazio = 4,

    /// <summary>
    /// Aplica valor constante
    /// </summary>
    ValorConstante = 5,

    // Regras de conversão de tipo
    /// <summary>
    /// Converte para inteiro (Int32)
    /// </summary>
    ConverterParaInt = 10,

    /// <summary>
    /// Converte para inteiro longo (Int64)
    /// </summary>
    ConverterParaLong = 11,

    /// <summary>
    /// Converte para decimal
    /// </summary>
    ConverterParaDecimal = 12,

    /// <summary>
    /// Arredonda valor decimal
    /// </summary>
    Arredondar = 15,

    /// <summary>
    /// Converte para booleano
    /// </summary>
    ConverterParaBool = 13,

    /// <summary>
    /// Converte para string
    /// </summary>
    ConverterParaString = 14,

    // Regras de normalização de texto
    /// <summary>
    /// Remove espaços do início e fim
    /// </summary>
    Trim = 20,

    /// <summary>
    /// Converte para maiúsculas
    /// </summary>
    Maiuscula = 21,

    /// <summary>
    /// Converte para minúsculas
    /// </summary>
    Minuscula = 22,

    /// <summary>
    /// Substitui caracteres
    /// </summary>
    Substituir = 23,

    /// <summary>
    /// Remove máscara (CPF, CNPJ, telefone, etc)
    /// </summary>
    RemoverMascara = 24,

    // Regras de data/hora
    /// <summary>
    /// Converte para data
    /// </summary>
    ConverterParaData = 30,

    /// <summary>
    /// Converte para data/hora
    /// </summary>
    ConverterParaDataHora = 31,

    // Regras de validação
    /// <summary>
    /// Valida valor mínimo
    /// </summary>
    ValorMinimo = 40,

    /// <summary>
    /// Valida valor máximo
    /// </summary>
    ValorMaximo = 41,

    /// <summary>
    /// Valida tamanho máximo de string
    /// </summary>
    TamanhoMaximo = 42,

    /// <summary>
    /// Valida contra expressão regular
    /// </summary>
    ValidarRegex = 43,

    /// <summary>
    /// Valida contra lista de valores permitidos
    /// </summary>
    ValoresPermitidos = 44,

    // Regras de lookup
    /// <summary>
    /// Lookup em lista local (De/Para)
    /// </summary>
    LookupLocal = 50,

    /// <summary>
    /// Lookup em tabela de banco de dados
    /// </summary>
    LookupBancoDados = 51
}
