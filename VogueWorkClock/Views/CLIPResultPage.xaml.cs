namespace VogueWorkClock.Views;

using System.Threading.Tasks;
using VogueWorkClock.Resources.Data;

public partial class CLIPResultPage : ContentPage
{
    private TaskCompletionSource<Sampleobj> _taskCompletionSource;
    public CLIPResultPage(CLIPResultViewModel viewModel, TaskCompletionSource<Sampleobj> taskCompletionSource)
	{
		InitializeComponent();
        BindingContext = viewModel;
        _taskCompletionSource = taskCompletionSource;
    }

    private async void OnItemButtonClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var selectedDescription = button?.CommandParameter as Sampleobj;
        if (selectedDescription != null)
        {
            _taskCompletionSource.SetResult(selectedDescription);
            // Закрываем модальное окно после выбора
            await Shell.Current.Navigation.PopModalAsync();
        }
    }
}
