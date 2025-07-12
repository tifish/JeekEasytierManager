using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace JeekEasytierManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        MainViewModel.Instance.SetMainWindow(this);
        DataContext = MainViewModel.Instance;

        Loaded += OnLoaded;
        Closing += OnClosing;
        Activated += OnActivated;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (Program.StartHidden)
        {
            Hide();
        }

        try
        {
            await MainViewModel.Instance.Init();
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Error", "Failed to initialize: " + ex.Message,
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error)
                .ShowWindowDialogAsync(this);
            App.ExitApplication();
        }
    }

    private async void OnActivated(object? sender, EventArgs e)
    {
        if (Settings.AutoRefreshInfo)
        {
            await MainViewModel.Instance.ShowInfo();
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // Cancel the close event and hide the window instead
        e.Cancel = true;
        Hide();
    }

    private void DialogTextBox_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }
}
