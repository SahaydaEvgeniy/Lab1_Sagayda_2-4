using System.Diagnostics;

namespace Lab1_Sagayda_2_4
{
    public partial class App : Application
    {
        private readonly AppShell _appShell;
        private readonly Services.DeepLinkService _deepLinkService;
        private readonly Repositories.IStudentRepository _studentRepository;
        private readonly Services.SyncService _syncService;
        private bool _isClosing;

        public App(AppShell appShell, Services.ThemeService themeService,
            Services.DeepLinkService deepLinkService,
            Repositories.IStudentRepository studentRepository, Services.SyncService syncService)
        {
            InitializeComponent();
            UserAppTheme = themeService.SelectedTheme;
            _appShell = appShell;
            _deepLinkService = deepLinkService;
            _studentRepository = studentRepository;
            _syncService = syncService;
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);
            _deepLinkService.ReceiveLink(uri);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(_appShell);
            window.Created += (_, _) => _ = InitializeDatabaseAsync();

            window.Stopped += (_, _) =>
            {
                if (_appShell.CurrentPage?.BindingContext is ViewModels.StudentDetailViewModel viewModel)
                    viewModel.SaveDraft();

                Debug.WriteLine("[App Lifecycle] Stopped: застосунок перейшов у фон.");
            };

            window.Resumed += (_, _) =>
            {
                if (_appShell.CurrentPage?.BindingContext is ViewModels.StudentDetailViewModel viewModel)
                    viewModel.RestoreDraft();

                Debug.WriteLine("[App Lifecycle] Resumed: застосунок повернувся з фону.");
                _ = _syncService.SynchronizeAsync();
            };

            window.Destroying += (_, _) =>
            {
                _isClosing = true;
                _syncService.Stop();
                Debug.WriteLine("[App Lifecycle] Destroying: вікно застосунку закривається.");
            };

            return window;
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _studentRepository.InitializeAsync();
                if (!_isClosing)
                    _syncService.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] Initialization failed: {ex}");
            }
        }
    }
}
