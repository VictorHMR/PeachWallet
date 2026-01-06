using Mopups.Services;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Utils;
using PeachWallet.View.SaldoPages;
using PeachWallet.ViewModel;
using UraniumUI.Dialogs.Mopups;

namespace PeachWallet.View;

public partial class SaldoPage : ContentPage
{
    private SaldoVM _viewModel;

    public SaldoPage(LocalDbService connection)
	{
		InitializeComponent();

        _viewModel = new SaldoVM(connection);
        BindingContext = _viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();

    }
}