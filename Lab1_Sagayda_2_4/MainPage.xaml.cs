using Lab1_Sagayda_2_4.ViewModels;

namespace Lab1_Sagayda_2_4;

public partial class MainPage : ContentPage
{
    public MainPage(StudentViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}
