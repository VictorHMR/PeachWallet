using Mopups.Services;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Services;
using PeachWallet.Utils;
using PeachWallet.ViewModel;
using System.Linq;

namespace PeachWallet.View;

public partial class LancamentoPage : ContentPage
{
    private LancamentoVM _viewModel;

    public LancamentoPage(LocalDbService connection, LiquidacaoService liquidacaoService)
	{
		InitializeComponent();
        _viewModel = new LancamentoVM(connection, liquidacaoService);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeVMAsync();

    }

}
