using DSI.Desktop.ViewModels;
using System.Windows;

namespace DSI.Desktop.Views;

public partial class NovaConexaoWindow : Window
{
    private readonly NovaConexaoViewModel _viewModel;

    public NovaConexaoWindow(NovaConexaoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.RequestClose += (result) =>
        {
            DialogResult = result;
            Close();
        };
    }
}
