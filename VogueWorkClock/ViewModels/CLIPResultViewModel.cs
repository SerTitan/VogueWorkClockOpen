using Firebase.Database;
using Firebase.Storage;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Storage;
using System.Net;
using VogueWorkClock.Resources.Data;
using Firebase.Database.Query;
using static Google.Api.FieldInfo.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


#if ANDROID
using Android.OS;
using Android.Widget;
#endif

namespace VogueWorkClock.ViewModels;
public partial class CLIPResultViewModel : ObservableObject
{
    public ObservableCollection<Sampleobj> Descriptions { get; set; } = new ObservableCollection<Sampleobj>();

    public string DescriptionPath
    {
        get => "";
        set
        {
            OnPropertyChanged(nameof(DescriptionPath));
        }
    }

    public string Similarity
    {
        get => "";
        set
        {
            OnPropertyChanged(nameof(Similarity));
        }
    }

    public string TextDescription
    {
        get => "";
        set
        {
            OnPropertyChanged(nameof(TextDescription));
        }
    }

    public string Status
    {
        get => "";
        set
        {
            OnPropertyChanged(nameof(Status));
        }
    }

    public string User
    {
        get => "";
        set
        {
            OnPropertyChanged(nameof(User));
        }
    }

    public List<string> ImageUrls
    {
        get => new List<string>();
        set
        {
            OnPropertyChanged(nameof(ImageUrls));
        }
    }

    public CLIPResultViewModel(List<Descr> descriptions, NewSampleData _SampleData)
    {
        LoadData(descriptions, _SampleData);
    }
    private async void LoadData(List<Descr> descriptions, NewSampleData _SampleData)
    {
        FirebaseClient firebaseClient = new FirebaseClient(Constants.FirebaseRDUrl);
        Sampleobj sample = new Sampleobj();

        foreach (var description in descriptions)
        {
            sample = new Sampleobj();
            sample.DescriptionPath = description.DescriptionPath;
            sample.Similarity = description.Similarity;

            string path = $"collection/{_SampleData.Year}/{_SampleData.Season}/{description.DescriptionPath}";
            var json = await firebaseClient.Child(path).OnceSingleAsync<object>();

            if (json != null)
            {
#pragma warning disable CS8604
                var firebaseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json.ToString());
#pragma warning disable CS8601
                if (firebaseData != null)
                {
                    if (firebaseData.TryGetValue("description", out var textDescription))
                    {
                        sample.TextDescription = textDescription.ToString();
                    }

                    if (firebaseData.TryGetValue("status", out var status))
                    {
                        sample.Status = status.ToString();
                    }

                    if (firebaseData.TryGetValue("user", out var user))
                    {
                        sample.User = user.ToString();
                    }

                    if (firebaseData.TryGetValue("date", out var date))
                    { 
                        sample.Date = date.ToString();
                    }

                    if (firebaseData.TryGetValue("references", out var references))
                    {
                        sample.ImageUrls = JsonConvert.DeserializeObject<List<string>>(references.ToString());
                    }

                    if (firebaseData.TryGetValue("samples", out var samplesObj))
                    {
                        if (samplesObj is JArray samplesArray)
                        {
                            foreach (var sampleItem in samplesArray)
                            {
                                if (sampleItem is JObject sampleData && sampleData.TryGetValue("references", out var sampleReferences))
                                {
                                    var sampleImageUrls = sampleReferences.ToObject<List<string>>();
                                    if (sampleImageUrls != null && sample.ImageUrls != null)
                                    {
                                        sample.ImageUrls.AddRange(sampleImageUrls);
                                    }
                                }
                            }
                        }
                    }
                }
#pragma warning restore CS8601
#pragma warning restore CS8604
            }
            Descriptions.Add(sample);

        }
    }
}
