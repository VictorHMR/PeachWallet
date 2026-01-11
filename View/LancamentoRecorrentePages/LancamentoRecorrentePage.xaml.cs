using PeachWallet.Database;
using PeachWallet.Services;
using PeachWallet.ViewModel;

namespace PeachWallet.View;

public partial class LancamentoRecorrentePage : ContentPage
{
    private LancamentoRecorrenteVM _viewModel;

    public LancamentoRecorrentePage(LocalDbService connection, LiquidacaoService liquidacaoService)
    {
        InitializeComponent();
        _viewModel = new LancamentoRecorrenteVM(connection, liquidacaoService);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeVMAsync();

    }

}