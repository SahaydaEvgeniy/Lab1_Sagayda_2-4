namespace Lab1_Sagayda_2_4
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("studentdetail", typeof(Views.StudentDetailPage));
        }
    }
}
