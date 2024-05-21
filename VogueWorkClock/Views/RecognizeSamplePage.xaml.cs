namespace VogueWorkClock.Views;

public partial class RecognizeSamplePage : ContentPage
{
	public RecognizeSamplePage(RecognizeSampleViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
