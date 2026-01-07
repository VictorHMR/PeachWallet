using PeachWallet.Database;
using PeachWallet.ViewModel;

namespace PeachWallet.View;

public partial class ConfigsPage : ContentPage
{
    private ConfigsVM _viewModel;
    public ConfigsPage(LocalDbService connection)
    {
        InitializeComponent();

        _viewModel = new ConfigsVM(connection);
        BindingContext = _viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();

    }
}