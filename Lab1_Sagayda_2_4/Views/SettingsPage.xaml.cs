using Lab1_Sagayda_2_4.ViewModels;

namespace Lab1_Sagayda_2_4.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
