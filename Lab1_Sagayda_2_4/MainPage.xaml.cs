using Lab1_Sagayda_2_4.ViewModels;

namespace Lab1_Sagayda_2_4;

public partial class MainPage : ContentPage
{
    public MainPage(StudentViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is StudentViewModel viewModel
            && viewModel.LoadCommand.CanExecute(null))
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        System.Diagnostics.Debug.WriteLine("[Student List] OnDisappearing: сторінку списку приховано.");
    }
}
