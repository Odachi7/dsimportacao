using CommunityToolkit.Mvvm.ComponentModel;
using DSI.Dominio.Enums;

namespace DSI.Desktop.ViewModels;

public partial class RegraViewModel : ObservableObject
{
    [ObservableProperty]
    private TipoRegra _tipo;

    [ObservableProperty]
    private string _parametros = string.Empty;

    public string NomeExibicao => formatarNome(Tipo);

    private string formatarNome(TipoRegra tipo)
    {
        return tipo switch
        {
            TipoRegra.Obrigatorio => "Obrigatório",
            TipoRegra.DefaultSeNulo => "Valor Padrão se Nulo",
            TipoRegra.DefaultSeVazio => "Valor Padrão se Vazio",
            TipoRegra.NuloSeVazio => "Nulo se Vazio",
            TipoRegra.ValorConstante => "Valor Constante",
            TipoRegra.Trim => "Remover Espaços (Trim)",
            TipoRegra.Maiuscula => "Maiúsculas",
            TipoRegra.Minuscula => "Minúsculas",
            TipoRegra.Substituir => "Substituir Texto",
            TipoRegra.TamanhoMaximo => "Tamanho Máximo",
            TipoRegra.ConverterParaInt => "Converter para Inteiro",
            TipoRegra.ConverterParaDecimal => "Converter para Decimal",
            TipoRegra.ConverterParaBool => "Converter para Booleano",
            TipoRegra.ConverterParaData => "Converter para Data",
            TipoRegra.Arredondar => "Arredondar",
            _ => tipo.ToString()
        };
    }
}
