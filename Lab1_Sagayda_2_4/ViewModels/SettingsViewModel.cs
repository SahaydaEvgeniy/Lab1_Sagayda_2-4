using System.ComponentModel;
using System.Windows.Input;
using Lab1_Sagayda_2_4.Services;

namespace Lab1_Sagayda_2_4.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ThemeService _themeService;

    public SettingsViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        GoBackCommand = new Command(async () =>
            await Shell.Current.GoToAsync(".."));
    }

    public ICommand GoBackCommand { get; }

    public bool IsDarkTheme
    {
        get => _themeService.SelectedTheme == AppTheme.Dark;
        set
        {
            if (IsDarkTheme == value)
                return;

            _themeService.SetTheme(value ? AppTheme.Dark : AppTheme.Light);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDarkTheme)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThemeDescription)));
        }
    }

    public string ThemeDescription => IsDarkTheme ? "Обрано темну тему" : "Обрано світлу тему";

    public event PropertyChangedEventHandler? PropertyChanged;
}
