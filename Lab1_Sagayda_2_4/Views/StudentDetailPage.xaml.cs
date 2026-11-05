using Lab1_Sagayda_2_4.ViewModels;

namespace Lab1_Sagayda_2_4.Views;

public partial class StudentDetailPage : ContentPage
{
    public StudentDetailPage(StudentDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);

        // A popped page ends editing; hiding the app does not cancel the draft.
        if (!Navigation.NavigationStack.Contains(this)
            && BindingContext is StudentDetailViewModel viewModel)
        {
            viewModel.ClearDraft();
        }
    }
}
