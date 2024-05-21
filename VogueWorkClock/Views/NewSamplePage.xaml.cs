namespace VogueWorkClock.Views;

public partial class NewSamplePage : ContentPage
{
    public NewSamplePage(NewSampleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
