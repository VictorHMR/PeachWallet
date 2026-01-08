using CommunityToolkit.Mvvm.Input;
using Mopups.Pages;
using Mopups.Services;
using System.Collections;
using System.Reflection;

namespace PeachWallet.View.Components;

public partial class SelectPromptPopup:PopupPage
{
    private readonly TaskCompletionSource<object?> _tcs = new();

    public readonly string? DisplayMember;
    private bool _closed;

    public SelectPromptPopup(
        IEnumerable itemsSource,
        object? selectedItem = null,
        string? displayMember = null)
    {
        InitializeComponent();

        DisplayMember = displayMember;

        List.ItemsSource = itemsSource;

        if (selectedItem != null)
            List.SelectedItem = selectedItem;
    }

    public Task<object?> Result => _tcs.Task;

    [RelayCommand]
    private async Task SelectItem(object item)
    {
        if (_closed)
            return;

        _closed = true;

        _tcs.TrySetResult(item);

        if (MopupService.Instance.PopupStack.Any())
            await MopupService.Instance.PopAsync();
    }


}
