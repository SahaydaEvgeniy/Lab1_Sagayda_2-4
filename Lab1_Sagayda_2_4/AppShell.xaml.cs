namespace Lab1_Sagayda_2_4
{
    public partial class AppShell : Shell
    {
        public AppShell(MainPage mainPage)
        {
            InitializeComponent();
            MainShellContent.Content = mainPage;
            Routing.RegisterRoute("studentdetail", typeof(Views.StudentDetailPage));
            Routing.RegisterRoute("settings", typeof(Views.SettingsPage));
        }
    }
}
