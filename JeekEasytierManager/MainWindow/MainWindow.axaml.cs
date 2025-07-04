using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;

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
            await MessageBoxManager.GetMessageBoxStandard("Error", "Failed to initialize: " + ex.Message).ShowAsync();
            App.ExitApplication();
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
