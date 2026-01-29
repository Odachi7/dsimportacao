using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DSI.Desktop.Views;

/// <summary>
/// Converter para mostrar/ocultar passos do wizard
/// </summary>
public class PassoVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int passoAtual && parameter is string passoEsperado)
        {
            return passoAtual == int.Parse(passoEsperado) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter para comparação de igualdade
/// </summary>
public class EqualConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue && parameter is string strParam)
        {
            return intValue == int.Parse(strParam) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter para comparação de diferença
/// </summary>
public class NotEqualConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue && parameter is string strParam)
        {
            return intValue != int.Parse(strParam) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class JobWizardWindow : Window
{
    public JobWizardWindow(ViewModels.JobWizardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Assina evento para fechar janela
        viewModel.RequestClose += (s, e) => Close();
    }
}
