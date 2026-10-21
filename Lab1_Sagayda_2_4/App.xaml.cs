using System.Diagnostics;

namespace Lab1_Sagayda_2_4
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            window.Stopped += (_, _) =>
                Debug.WriteLine("[App Lifecycle] Stopped: застосунок перейшов у фон.");

            window.Resumed += (_, _) =>
                Debug.WriteLine("[App Lifecycle] Resumed: застосунок повернувся з фону.");

            window.Destroying += (_, _) =>
                Debug.WriteLine("[App Lifecycle] Destroying: вікно застосунку закривається.");

            return window;
        }
    }
}
