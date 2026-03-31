using Mopups.Services;
using PeachWallet.Models;
using PeachWallet.Utils;
using System.Threading.Channels;
using System.Windows.Input;
using UraniumUI.Material.Controls;

namespace PeachWallet.View;

public partial class LancamentoRecorrentePopup
{
    private readonly Action<LancamentoRecorrenteDTO?, PopupMode> _onSubmit;
    private readonly Func<LancamentoRecorrenteDTO, Task>? _onDelete;
    private readonly PopupMode _mode;
    private readonly LancamentoRecorrenteDTO? _lancamento;

    public LancamentoRecorrentePopup(PopupMode mode, Action<LancamentoRecorrenteDTO?, PopupMode> onSubmit, Func<LancamentoRecorrenteDTO, Task>? onDelete = null, LancamentoRecorrenteDTO ? lancamento = null)
    {
        InitializeComponent();
        _mode = mode;
        _onSubmit = onSubmit;
        _onDelete = onDelete;
        _lancamento = lancamento;

        dpdTipoLancamento.ItemsSource = Enum.GetValues(typeof(TiposLancamento))
            .Cast<TiposLancamento>()
            .Select(e => new TipoLancamentoPickerItem { Display = e.ToString(), Id = e })
            .ToList();

        dpdTipoLancamento.ItemDisplayBinding = new Binding("Display");

        inputForm.SubmitCommand = new Command(OnSubmitClicked);

        if (_lancamento != null)
        {
            TitleLabel.Text = "Lançamento Mensal";
            btnSubmit.Text = "Salvar";
            txtDescricao.Text = _lancamento.Descricao;
            txtValor.Text = _lancamento.Valor.ToString("N2");
            dpdTipoLancamento.SelectedItem = ((List<TipoLancamentoPickerItem>)dpdTipoLancamento.ItemsSource)
                .FirstOrDefault(c => c.Id == _lancamento.TipoLancamento);
            pkDataLancamento.Date = _lancamento.DtLancamento;
            swtCred.IsToggled = _lancamento.FlCredito;
            txtQtdMeses.Text = _lancamento.NrMeses?.ToString();
            btnDelete.IsVisible = true;
        }
    }


    public void OnCancelClicked(object sender, EventArgs e)
    {
        LancamentoRecorrenteDTO dto = _lancamento ?? new LancamentoRecorrenteDTO();

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
        LancamentoRecorrenteDTO dto = _lancamento ?? new LancamentoRecorrenteDTO();

        if (dpdTipoLancamento.SelectedItem is TipoLancamentoPickerItem selected)
        {
            dto.TipoLancamento = selected.Id;
            dto.Descricao = txtDescricao.Text;
            dto.Valor = double.TryParse(txtValor.Text, out double value) ? value : 0;
            dto.DtLancamento = pkDataLancamento.Date ?? DateTime.Now;
            dto.FlCredito = swtCred.IsToggled;
            dto.NrMeses = int.TryParse(txtQtdMeses.Text, out int meses) ? meses : null;
        }

        _onSubmit?.Invoke(dto, _mode);
        MopupService.Instance.PopAsync();
    }

    public async void OnDeleteClicked(object sender, EventArgs e)
    {
        bool confirmar = await Application.Current.MainPage.DisplayAlert(
            "Excluir lançamento mensal",
            "Tem certeza que deseja excluir este lançamento? Essa operação irá excluir todos os futuros lançamentos relacionados e este lançamento mensal",
            "Excluir",
            "Cancelar"
        );
        if (!confirmar)
            return;

        await _onDelete.Invoke(_lancamento);
        await MopupService.Instance.PopAsync();
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
