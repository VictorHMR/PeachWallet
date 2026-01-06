using Mopups.Services;
using PeachWallet.Models;
using PeachWallet.Utils;

namespace PeachWallet.View.SaldoPages;

public partial class ContaBancariaPopup
{
    private readonly Action<ContaBancariaDTO?, PopupMode> _onSubmit;
    private readonly PopupMode _mode;
    private readonly ContaBancariaDTO? _contaBancaria;

    public ContaBancariaPopup(PopupMode mode, Action<ContaBancariaDTO?, PopupMode> onSubmit, ContaBancariaDTO? contaBancaria = null)
    {
        InitializeComponent();
        _mode = mode;
        _onSubmit = onSubmit;
        _contaBancaria = contaBancaria;

        inputForm.SubmitCommand = new Command(OnSubmitClicked);


        if (_contaBancaria != null)
        {
            txtNomeConta.Text = contaBancaria.Nome;
            txtSaldo.Text = contaBancaria.SaldoAtual.ToString("N2");
        }

        switch (_mode)
        {
            case PopupMode.Create:
                TitleLabel.Text = "Nova Conta Bancária";
                btnSubmit.Text = "Criar";
                break;

            case PopupMode.Update:
                TitleLabel.Text = "Editar Conta Bancária";
                btnSubmit.Text = "Salvar";
                break;
        }
    }

    public void OnCancelClicked(object sender, EventArgs e)
    {
        ContaBancariaDTO dto = _contaBancaria ?? new ContaBancariaDTO();

        _onSubmit?.Invoke(dto, PopupMode.Cancel);
        MopupService.Instance.PopAsync();
    }

    public void OnSubmitClicked()
    {
        ContaBancariaDTO dto = _contaBancaria ?? new ContaBancariaDTO();

        dto.Nome = txtNomeConta.Text;
        dto.SaldoAtual = double.TryParse(txtSaldo.Text, out double value) ? value : 0;

        _onSubmit?.Invoke(dto, _mode);
        MopupService.Instance.PopAsync();
    }


}