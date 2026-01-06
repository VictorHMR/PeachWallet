using Mopups.Services;
using PeachWallet.View.Components;

namespace PeachWallet.Utils;

public static class PromptService
{
    public static async Task<string?> ShowTextPromptAsync(
        string title,
        string fieldTitle = "Valor",
        string? initialValue = null,
        Keyboard? keyboard = null,
        string confirmText = "Confirmar")
    {
        var popup = new TextPromptPopup(
            title,
            fieldTitle,
            initialValue,
            keyboard,
            confirmText);

        await MopupService.Instance.PushAsync(popup);
        return await popup.Result;
    }
}
