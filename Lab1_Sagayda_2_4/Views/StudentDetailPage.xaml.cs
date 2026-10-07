using Lab1_Sagayda_2_4.ViewModels;

namespace Lab1_Sagayda_2_4.Views;

public partial class StudentDetailPage : ContentPage
{
    public StudentDetailPage()
    {
        InitializeComponent();
        BindingContext = new StudentDetailViewModel();
    }
}
