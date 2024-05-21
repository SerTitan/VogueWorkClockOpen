namespace VogueWorkClock.Views;

using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using VogueWorkClock.Resources.Data;
using VogueWorkClock.ViewModels;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;

public partial class CalendarPage : ContentPage
{
    private readonly FirebaseService _firebaseClient;
    private readonly CalendarViewModel _viewModel;

    public CalendarPage(CalendarViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
        _firebaseClient = new FirebaseService();
        _viewModel.StatusChanged += OnStatusChanged; // Subscribe to the event
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    private async void OnStatusChanged(object? sender, EventArgs e)
    {
        await LoadData(); // Reload data when status changes
    }

    private async Task LoadData()
    {
        var jsonData = await _firebaseClient.GetJsonDataAsync();
        List<NewSampleData> samples = ProcessJsonData(jsonData);
        _viewModel.Samples.Clear();
        foreach (var sample in samples)
        {
            _viewModel.Samples.Add(sample);
        }
    }

    private List<NewSampleData> ProcessJsonData(JObject jsonData)
    {
        var samples = new List<NewSampleData>();
        for (int i = 18; i < 24; i++)
        {
            var collection = jsonData[$"{i}"]?.Children<JProperty>();

            if (collection != null)
            {
                foreach (var season in collection)
                {
                    foreach (var type in season.Value.Children<JProperty>())
                    {
                        foreach (var style in type.Value.Children<JProperty>())
                        {
                            var sampleData = style.Value;
                            if (style.Name != "heh")
                            {
                                var Status = sampleData["status"]?.ToString();
                                var Date = sampleData["date"]?.ToString();

                                if (Status != null && (Status.StartsWith("wait") || Status.StartsWith("work")))
                                {
                                    var Description = sampleData["description"]?.ToString();
                                    var EmployeeName = sampleData["user"]?.ToString();
                                    var References = sampleData["references"]?.Select(r => r.ToString()).ToList();

                                    var sampleList = new List<SampleList>();
                                    var samplesArray = sampleData["samples"]?.Children().ToList();

                                    Color statusColor = new Color();
                                    List<string> statusText = new List<string> { "Done", "Cancel" };

                                    if (samplesArray != null)
                                    {
                                        string sampDate = string.Empty;
                                        foreach (var sampleItem in samplesArray)
                                        {
                                            if (sampleItem != null && sampleItem.Type == JTokenType.Object)
                                            {
                                                var sDate = sampleItem["date"]?.ToString();
                                                var sComment = sampleItem["comment"]?.ToString();
                                                var sReferences = sampleItem["references"]?.Select(r => r.ToString()).ToList();
                                                var sUser = sampleItem["user"]?.ToString();

                                                if (sDate != null && sComment != null && sUser != null && sReferences != null)
                                                {
                                                    sampleList.Add(new SampleList
                                                    {
                                                        sDate = sDate,
                                                        sComment = sComment,
                                                        sReferences = sReferences,
                                                        sEmployeeName = sUser
                                                    });
                                                }
                                                if (sDate != null) sampDate = sDate;
                                            }
                                        }
                                        if (!string.IsNullOrWhiteSpace(sampDate))
                                        {
                                            DateTime? parsedDate = DateTime.TryParse(sampDate, out var parsed) ? parsed : (DateTime?)null;
                                            statusColor = GetStatusColor(Status, parsedDate);
                                        }
                                    }
                                    else
                                    {
                                        // Если нет вложенных samples, обработаем основную дату и статус
                                        DateTime? parsedDate = DateTime.TryParse(Date, out var parsed) ? parsed : (DateTime?)null;
                                        statusColor = GetStatusColor(Status, parsedDate);
                                    }

                                    // Формируем StatusText
                                    if (Status.StartsWith("work"))
                                    {
                                        var match = System.Text.RegularExpressions.Regex.Match(Status, @"work (\d+)-sample");
                                        if (match.Success && int.TryParse(match.Groups[1].Value, out int currentNumber))
                                        {
                                            statusText.Add($"wait {currentNumber + 1}-sample");
                                        }
                                    }

                                    if (Date != null && Description != null && EmployeeName != null && References != null)
                                    {
                                        samples.Add(new NewSampleData
                                        {
                                            Year = i.ToString(),
                                            Season = season.Name,
                                            Type = type.Name,
                                            Style = style.Name,
                                            Date = Date,
                                            Description = Description,
                                            EmployeeName = EmployeeName,
                                            Status = Status,
                                            StatusColor = statusColor,
                                            References = References,
                                            Sampleslist = sampleList,
                                            StatusText = statusText
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return samples;
    }

    private Color GetStatusColor(string status, DateTime? date)
    {
        if (!date.HasValue)
        {
            return Colors.Transparent;
        }

        var daysElapsed = (DateTime.Now - date.Value).Days;

        if (status.StartsWith("wait"))
        {
            if (daysElapsed < 7)
                return Colors.Green;
            else if (daysElapsed < 14)
                return Colors.Yellow;
            else
                return Colors.Red;
        }
        else if (status.StartsWith("work"))
        {
            if (daysElapsed < 1)
                return Colors.Green;
            else if (daysElapsed < 3)
                return Colors.Yellow;
            else
                return Colors.Red;
        }

        return Colors.Transparent;
    }
}
