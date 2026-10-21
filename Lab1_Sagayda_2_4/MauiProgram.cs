using Microsoft.Extensions.Logging;
using Lab1_Sagayda_2_4.Services;
using Lab1_Sagayda_2_4.ViewModels;
using Lab1_Sagayda_2_4.Views;

namespace Lab1_Sagayda_2_4
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<IAppStateService, AppStateService>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<StudentViewModel>();
            builder.Services.AddTransient<StudentDetailViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<StudentDetailPage>();
            builder.Services.AddTransient<AppShell>();

            return builder.Build();
        }
    }
}
