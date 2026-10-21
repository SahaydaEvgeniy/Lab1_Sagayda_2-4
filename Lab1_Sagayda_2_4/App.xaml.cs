using System.Diagnostics;

namespace Lab1_Sagayda_2_4
{
    public partial class App : Application
    {
        private readonly AppShell _appShell;

        public App(AppShell appShell)
        {
            InitializeComponent();
            _appShell = appShell;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(_appShell);

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
            };

            window.Destroying += (_, _) =>
                Debug.WriteLine("[App Lifecycle] Destroying: вікно застосунку закривається.");

            return window;
        }
    }
}
