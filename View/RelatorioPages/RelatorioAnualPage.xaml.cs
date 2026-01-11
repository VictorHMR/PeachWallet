using PeachWallet.Database;
using PeachWallet.ViewModel;

namespace PeachWallet.View;

public partial class RelatorioAnualPage : ContentPage
{
    private RelatorioAnualVM _viewModel;

    public RelatorioAnualPage(LocalDbService connection)
	{
		InitializeComponent();
		_viewModel = new RelatorioAnualVM(connection);
		BindingContext = _viewModel;
    }

    protected async override void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.InitializeVMAsync();
    }
}