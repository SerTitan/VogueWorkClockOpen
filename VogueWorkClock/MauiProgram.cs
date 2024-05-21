namespace VogueWorkClock;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

        builder.Services.AddSingleton<CalendarViewModel>();

        builder.Services.AddSingleton<CalendarPage>();

        builder.Services.AddSingleton<NewSampleViewModel>();

        builder.Services.AddSingleton<NewSamplePage>();

        builder.Services.AddSingleton<RecognizeSampleViewModel>();

		builder.Services.AddSingleton<RecognizeSamplePage>();

        builder.Services.AddSingleton<CLIPResultPage>();

        builder.Services.AddSingleton<CLIPResultViewModel>();

        return builder.Build();
	}
}
