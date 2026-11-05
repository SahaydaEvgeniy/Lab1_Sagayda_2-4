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
            builder.Services.AddSingleton<Microsoft.Maui.Storage.ISecureStorage>(Microsoft.Maui.Storage.SecureStorage.Default);
            builder.Services.AddSingleton<IStudentCardProtection, StudentCardProtection>();
            builder.Services.AddSingleton<Repositories.IStudentRepository>(services =>
                new Repositories.StudentRepository(
                    Path.Combine(FileSystem.Current.AppDataDirectory, "students.db3"),
                    services.GetRequiredService<IStudentCardProtection>()));
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<DeepLinkService>();
            builder.Services.AddSingleton<Microsoft.Maui.Networking.IConnectivity>(Microsoft.Maui.Networking.Connectivity.Current);
            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri(DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:5080/" : "http://127.0.0.1:5080/"),
                Timeout = TimeSpan.FromSeconds(10)
            });
            builder.Services.AddSingleton<SyncService>();
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
