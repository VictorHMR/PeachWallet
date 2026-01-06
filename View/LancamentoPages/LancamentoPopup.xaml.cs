using Mopups.Services;
using PeachWallet.Models;
using PeachWallet.Utils;
using System.Threading.Channels;
using System.Windows.Input;

namespace PeachWallet.View;

public partial class LancamentoPopup
{
    private readonly Action<LancamentoDTO?, PopupMode> _onSubmit;
    private readonly PopupMode _mode;
    private readonly LancamentoDTO? _lancamento;

    public LancamentoPopup(PopupMode mode, Action<LancamentoDTO?, PopupMode> onSubmit, LancamentoDTO? lancamento = null)
    {
        InitializeComponent();
        _mode = mode;
        _onSubmit = onSubmit;
        _lancamento = lancamento;
        dpdTipoLancamento.ItemsSource = Enum.GetValues(typeof(TiposLancamento))
            .Cast<TiposLancamento>()
            .Select(e => new TipoLancamentoPickerItem { Display = e.ToString(), Id = e })
            .ToList();
        dpdTipoLancamento.ItemDisplayBinding = new Binding("Display");

        inputForm.SubmitCommand = new Command(OnSubmitClicked);


        if (_lancamento != null)
        {
            txtDescricao.Text = _lancamento.Descricao;
            txtValor.Text = _lancamento.Valor.ToString("N2");
            dpdTipoLancamento.SelectedItem = ((List<TipoLancamentoPickerItem>)dpdTipoLancamento.ItemsSource)
                .FirstOrDefault(c => c.Id == _lancamento.TipoLancamento);
        }

        switch (_mode)
        {
            case PopupMode.Create:
                TitleLabel.Text = "Novo Lançamento";
                btnSubmit.Text = "Criar";
                btnDelete.IsVisible = false;
                break;

            case PopupMode.Update:
                TitleLabel.Text = "Editar Lançamento";
                btnSubmit.Text = "Salvar";
                btnDelete.IsVisible = true;
                break;

            case PopupMode.Delete:
                TitleLabel.Text = "Excluir Lançamento";
                btnSubmit.IsVisible = false;
                btnDelete.IsVisible = true;
                txtDescricao.IsEnabled = false;
                txtValor.IsEnabled = false;
                dpdTipoLancamento.IsEnabled = false;
                break;
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

        if (_mode != PopupMode.Delete)
        {

            if (dpdTipoLancamento.SelectedItem is TipoLancamentoPickerItem selected)
            {
                dto.TipoLancamento = selected.Id;
                dto.Descricao = txtDescricao.Text;
                dto.Valor = double.TryParse(txtValor.Text, out double value) ? value : 0;
            }
        }

        _onSubmit?.Invoke(dto, _mode);
        MopupService.Instance.PopAsync();
    }

    public async void OnDeleteClicked(object sender, EventArgs e)
    {
        bool confirmar = await Application.Current.MainPage.DisplayAlert(
            "Excluir lançamento",
            "Tem certeza que deseja excluir este lançamento?",
            "Excluir",
            "Cancelar"
        );

        if (!confirmar)
            return;

        _onSubmit?.Invoke(_lancamento, PopupMode.Delete);
        await MopupService.Instance.PopAsync();
    }

}


public class TipoLancamentoPickerItem
{
    public TiposLancamento Id { get; set; }
    public string Display { get; set; }
}