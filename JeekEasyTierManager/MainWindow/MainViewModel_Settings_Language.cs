using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jeek.Avalonia.Localization;

namespace JeekEasyTierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    // "system" follows the OS language; otherwise a two-letter code like "en" or "zh".
    public bool IsLanguageSystem => Settings.Language == "system";
    public bool IsLanguageEnglish => Settings.Language == "en";
    public bool IsLanguageChinese => Settings.Language == "zh";

    [RelayCommand]
    public void SetLanguage(string language)
    {
        Settings.Language = language;
        _ = AppSettings.Save();
        ApplyLanguage(language);
    }

    public void ApplyLanguage(string language)
    {
        if (language == "system")
        {
            var systemLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            Localizer.Language = Localizer.Languages.Contains(systemLanguage)
                ? systemLanguage
                : "en";
        }
        else
        {
            Localizer.Language = string.IsNullOrEmpty(language) ? "en" : language;
        }

        RefreshLanguageSelectionProperties();
    }

    private void RefreshLanguageSelectionProperties()
    {
        OnPropertyChanged(nameof(IsLanguageSystem));
        OnPropertyChanged(nameof(IsLanguageEnglish));
        OnPropertyChanged(nameof(IsLanguageChinese));
    }
}
