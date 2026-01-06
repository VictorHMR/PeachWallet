using Mopups.Services;
using PeachWallet.Database;
using PeachWallet.Database.Models;
using PeachWallet.Models;
using PeachWallet.Utils;
using PeachWallet.ViewModel;
using System.Linq;

namespace PeachWallet.View;

public partial class LancamentoPage : ContentPage
{
    private LancamentoVM _viewModel;

    public LancamentoPage(LocalDbService connection)
	{
		InitializeComponent();
        _viewModel = new LancamentoVM(connection);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadDates();
        await CarregaDadosLancamento();
    }


    private async void lsvLancamentos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is LancamentoDTO lancamento)
        {
            await MopupService.Instance.PushAsync(
                    new LancamentoPopup(PopupMode.Update, async (lancamento, mode) =>
                    {
                        if (mode == PopupMode.Update && lancamento != null)
                        {
                            await _viewModel.AtualizarLancamentoAsync(lancamento);
                        }
                        else if (mode == PopupMode.Delete && lancamento != null)
                        {
                            await _viewModel.RemoverLancamentoAsync(lancamento.IdLancamento);
                        }
                        
                        lsvLancamentos.SelectedItem = null;

                    }, lancamento)
            );
        }
    }
    private async void OnAddClicked(object sender, EventArgs e)
    {
        await MopupService.Instance.PushAsync(
                new LancamentoPopup(PopupMode.Create, async (lancamento, mode) =>
                {
                    if (mode == PopupMode.Create && lancamento != null)
                        await _viewModel.CriarLancamentoAsync(lancamento);                        
                })
        );
    }

    private void LoadDates()
    {
        entryStartDate.Date = DateTime.Now.Date;
        entryEndDate.Date = DateTime.Now.Date;
    }


    private async Task CarregaDadosLancamento()
    {
        await _viewModel.ReloadContaAsync();
    }



}
