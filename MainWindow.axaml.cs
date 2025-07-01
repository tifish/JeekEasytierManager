using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;

namespace JeekEasytierManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            await MainViewModel.Instance.Init();
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "Failed to initialize: " + ex.Message).ShowAsync();
            Close();
        }
    }
}