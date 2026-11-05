using Android.App;
using Android.Content.PM;
using Android.Content;
using Android.OS;

namespace Lab1_Sagayda_2_4
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { global::Android.Content.Intent.ActionView },
        Categories = new[] { global::Android.Content.Intent.CategoryDefault, global::Android.Content.Intent.CategoryBrowsable },
        DataScheme = Services.StudentDeepLink.Scheme,
        DataHost = Services.StudentDeepLink.Host)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleDeepLink(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleDeepLink(intent);
        }

        private static void HandleDeepLink(Intent? intent)
        {
            if (intent?.Action == global::Android.Content.Intent.ActionView
                && Uri.TryCreate(intent.DataString, UriKind.Absolute, out var uri)
                && Microsoft.Maui.Controls.Application.Current is App app)
            {
                app.SendOnAppLinkRequestReceived(uri);
            }
        }
    }
}
