using Mopups.Services;

namespace PeachWallet.View.Components;

public partial class TextPromptPopup
{
    private readonly TaskCompletionSource<string?> _tcs;

    public TextPromptPopup(
        string title,
        string fieldTitle = "Valor",
        string? initialValue = null,
        Keyboard? keyboard = null,
        string confirmText = "Confirmar")
    {
        InitializeComponent();

        _tcs = new TaskCompletionSource<string?>();

        TitleLabel.Text = title;
        lblInput.Text = fieldTitle;
        txtInput.Text = initialValue;
        txtInput.Keyboard = keyboard ?? Keyboard.Default;
        btnSubmit.Text = confirmText;

        inputForm.SubmitCommand = new Command(OnSubmit);
    }

    public Task<string?> Result => _tcs.Task;

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await MopupService.Instance.PopAsync();
    }

    private async void OnSubmit()
    {
        _tcs.TrySetResult(txtInput.Text);
        await MopupService.Instance.PopAsync();
    }
}
