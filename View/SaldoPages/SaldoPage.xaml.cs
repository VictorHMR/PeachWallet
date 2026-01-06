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
    private readonly LocalDbService _connection;

    public SaldoPage(LocalDbService connection)
	{
		InitializeComponent();

        _connection = connection;
        _viewModel = new SaldoVM(connection);
        BindingContext = _viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();

    }

    private async void AdicionarProjecao_Clicked(object sender, EventArgs e)
    {
        var ano = _viewModel.ProjecaoVM.Projecoes.Any() ? _viewModel.ProjecaoVM.Projecoes.Max(x => x.Ano) + 1 : DateTime.Now.Date.Year;
        var result = await PromptService.ShowTextPromptAsync(
            title: "Nova Projeção",
            fieldTitle: $"Quanto pretende investir mensalmente em {ano}?",
            keyboard: Keyboard.Numeric
        );

        if (string.IsNullOrEmpty(result))
            return;
        ProjecaoDTO projecao = new ProjecaoDTO
        {
            Ano = ano,
            InvestidoMensal = double.Parse(result)
        };

        await _viewModel.ProjecaoVM.CriarProjecaoAsync(projecao);
    }
    private async void EditarProjecao_Clicked(object sender, EventArgs e)
    {
        if (sender is ImageButton btn && btn.CommandParameter is ProjecaoDTO projecao)
        {
            var result = await PromptService.ShowTextPromptAsync(
                title: "Editar Projeção",
                fieldTitle: $"Quanto pretende investir mensalmente em {projecao.Ano}?",
                initialValue: projecao.InvestidoMensal.ToString(),
                keyboard: Keyboard.Numeric
            );

            if (string.IsNullOrEmpty(result))
                return;
            projecao.InvestidoMensal = double.Parse(result);
            await _viewModel.ProjecaoVM.EditarProjecaoAsync(projecao);

        }
    }
}