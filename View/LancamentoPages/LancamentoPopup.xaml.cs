using Mopups.Services;
using PeachWallet.Models;
using PeachWallet.Utils;
using System.Threading.Channels;
using System.Windows.Input;
using UraniumUI.Material.Controls;

namespace PeachWallet.View;

public partial class LancamentoPopup
{
    private readonly Action<LancamentoDTO?, PopupMode> _onSubmit;
    private readonly PopupMode _mode;
    private readonly LancamentoDTO? _lancamento;
    private readonly DateTime _dataInicial;
    public LancamentoPopup(PopupMode mode, Action<LancamentoDTO?, PopupMode> onSubmit, LancamentoDTO? lancamento = null, DateTime? dataInicial = null)
    {
        InitializeComponent();
        _mode = mode;
        _onSubmit = onSubmit;
        _lancamento = lancamento;
        _dataInicial = dataInicial ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        dpdTipoLancamento.ItemsSource = Enum.GetValues(typeof(TiposLancamento))
            .Cast<TiposLancamento>()
            .Select(e => new TipoLancamentoPickerItem { Display = e.ToString(), Id = e })
            .ToList();

        dpdTipoLancamento.ItemDisplayBinding = new Binding("Display");

        inputForm.SubmitCommand = new Command(OnSubmitClicked);

        pkDataLancamento.Date = _dataInicial;

        if (_lancamento != null)
        {
            txtDescricao.Text = _lancamento.Descricao;
            txtValor.Text = _lancamento.Valor.ToString("N2");
            dpdTipoLancamento.SelectedItem = ((List<TipoLancamentoPickerItem>)dpdTipoLancamento.ItemsSource)
                .FirstOrDefault(c => c.Id == _lancamento.TipoLancamento);
            pkDataLancamento.Date = _lancamento.DtLancamento;
            swtCred.IsToggled = _lancamento.FlCredito;
        }
    }


    public void OnCancelClicked(object sender, EventArgs e)
    {
        LancamentoDTO dto = _lancamento ?? new LancamentoDTO();

        _onSubmit?.Invoke(dto, PopupMode.Cancel);
        MopupService.Instance.PopAsync();
    }

    public void OnSubmitClicked()
    {
        if (dpdTipoLancamento.SelectedItem is not TipoLancamentoPickerItem)
        {
            DisplayAlert("Atenção", "Escolha uma categoria.", "OK");
            return;
        }
        LancamentoDTO dto = _lancamento ?? new LancamentoDTO();

        if (dpdTipoLancamento.SelectedItem is TipoLancamentoPickerItem selected)
        {
            dto.TipoLancamento = selected.Id;
            dto.Descricao = txtDescricao.Text;
            dto.Valor = double.TryParse(txtValor.Text, out double value) ? value : 0;
            dto.DtLancamento = pkDataLancamento.Date ?? DateTime.Now;
            dto.FlCredito = swtCred.IsToggled;
        }

        _onSubmit?.Invoke(dto, _mode);
        MopupService.Instance.PopAsync();
    }

    private void dpdTipoLanc_SelectedItemChanged(object sender, object e)
    {
        if (dpdTipoLancamento.SelectedItem is not TipoLancamentoPickerItem selected)
            return;
    
        switch (selected.Id)
        {
            case TiposLancamento.Saida:
                swtCred.IsToggled = true;
                swContainer.IsVisible = true;
                Grid.SetColumnSpan(dpdTipoLancamento, 1);
                break;

            default:
                swtCred.IsToggled = false;
                swContainer.IsVisible = false;
                Grid.SetColumnSpan(dpdTipoLancamento, 2);
                break;
        }
    }
}