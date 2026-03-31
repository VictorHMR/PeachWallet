using PeachWallet.Database;
using PeachWallet.Utils;
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

        dpdTipoDeducaoSaldoDisp.ItemsSource= Enum.GetValues(typeof(TipoDeducaoSaldoDisp))
            .Cast<TipoDeducaoSaldoDisp>()
            .Select(e => new TiposDeducaoSaldoDispPickerItem { Display = e.ToString().Replace("_", " "), Id = e })
            .ToList();

        dpdTipoDeducaoSaldoDisp.SelectedItem = ((List<TiposDeducaoSaldoDispPickerItem>)dpdTipoDeducaoSaldoDisp.ItemsSource)
                .FirstOrDefault(c => c.Id == _viewModel.TipoDeducaoSaldoDisp);

    }
}
