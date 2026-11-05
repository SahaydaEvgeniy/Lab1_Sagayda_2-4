namespace Lab1_Sagayda_2_4.Services;

public class ThemeService
{
    private const string ThemePreferenceKey = "app_theme";

    public AppTheme SelectedTheme =>
        Preferences.Default.Get(ThemePreferenceKey, "Light") == "Dark"
            ? AppTheme.Dark
            : AppTheme.Light;

    public void SetTheme(AppTheme theme)
    {
        if (Application.Current is { } application)
            application.UserAppTheme = theme;

        Preferences.Default.Set(ThemePreferenceKey,
            theme == AppTheme.Dark ? "Dark" : "Light");
    }
}
