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

        if (BindingContext is StudentViewModel syncViewModel)
        {
            syncViewModel.StopObservingSync();
            syncViewModel.ObserveSync();
        }

        if (BindingContext is StudentViewModel viewModel
            && viewModel.LoadCommand.CanExecute(null))
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is StudentViewModel viewModel)
            viewModel.StopObservingSync();
        System.Diagnostics.Debug.WriteLine("[Student List] OnDisappearing: сторінку списку приховано.");
    }
}
