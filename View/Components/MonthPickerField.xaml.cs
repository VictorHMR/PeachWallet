using Mopups.Services;
using System.Collections;
using System.Windows.Input;

namespace PeachWallet.View.Components;

public partial class MonthPickerField : ContentView
{
    public MonthPickerField()
    {
        InitializeComponent();
    }

    // ======================
    // Bindable Properties
    // ======================

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(MonthPickerField));

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(
            nameof(SelectedItem),
            typeof(object),
            typeof(MonthPickerField),
            null,
            BindingMode.TwoWay,
            propertyChanged: OnSelectedItemChanged);

    public static readonly BindableProperty DisplayMemberProperty =
        BindableProperty.Create(nameof(DisplayMember), typeof(string), typeof(MonthPickerField));

    public static readonly BindableProperty SelectionChangedCommandProperty =
        BindableProperty.Create(nameof(SelectionChangedCommand), typeof(ICommand), typeof(MonthPickerField));

    public static readonly BindableProperty DisplayTextProperty =
        BindableProperty.Create(nameof(DisplayText), typeof(string), typeof(MonthPickerField));

    // ======================
    // Properties
    // ======================

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string DisplayMember
    {
        get => (string)GetValue(DisplayMemberProperty);
        set => SetValue(DisplayMemberProperty, value);
    }

    public ICommand? SelectionChangedCommand
    {
        get => (ICommand?)GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    // ======================
    // Logic
    // ======================

    private async void OnTapped(object sender, EventArgs e)
    {
        if (ItemsSource == null)
            return;

        var popup = new SelectPromptPopup(
            ItemsSource,
            SelectedItem,
            DisplayMember);

        await MopupService.Instance.PushAsync(popup);

        var result = await popup.Result;

        if (result != null)
        {
            SelectedItem = result;

            if (SelectionChangedCommand?.CanExecute(result) == true)
                SelectionChangedCommand.Execute(result);
        }
    }

    private static void OnSelectedItemChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        var control = (MonthPickerField)bindable;

        if (newValue == null)
        {
            control.DisplayText = string.Empty;
            return;
        }

        var prop = newValue.GetType().GetProperty(control.DisplayMember);
        control.DisplayText = prop?.GetValue(newValue)?.ToString() ?? string.Empty;
    }
}
