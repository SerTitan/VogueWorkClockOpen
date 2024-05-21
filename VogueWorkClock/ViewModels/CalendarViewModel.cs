using Firebase.Database;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using System.Net;
using VogueWorkClock.Resources.Data;
using static Google.Api.FieldInfo.Types;
#if ANDROID 
using Android.OS;
#endif

namespace VogueWorkClock.ViewModels;
public partial class CalendarViewModel : ObservableObject
{
    public ObservableCollection<NewSampleData> Samples { get; set; } = new ObservableCollection<NewSampleData>();
    private NewSampleData _sample = new NewSampleData();

    public event EventHandler? StatusChanged;

    public CalendarViewModel()
    {
        
    }
    public string Season
    {
        get => _sample.Season;
        set
        {
            OnPropertyChanged(nameof(Season));
        }
    }

    public string Year
    {
        get => _sample.Year;
        set
        {
            OnPropertyChanged(nameof(Year));
        }
    }

    public string Type
    {
        get => _sample.Type;
        set
        {
            OnPropertyChanged(nameof(Type));
        }
    }

    public string Style
    {
        get => _sample.Style;
        set
        {
            OnPropertyChanged(nameof(Style));
        }
    }

    public string Description
    {
        get => _sample.Description;
        set
        {
            OnPropertyChanged(nameof(Description));
        }
    }

    public string EmployeeName
    {
        get => _sample.EmployeeName;
        set
        {
            OnPropertyChanged(nameof(EmployeeName));
        }
    }

    public string Status
    {
        get => _sample.Status;
        set
        {
            OnPropertyChanged(nameof(Status));
        }
    }

    public string Date
    {
        get => _sample.Date;
        set
        {
            OnPropertyChanged(nameof(Date));
        }
    }

    public string User
    {
        get => _sample.EmployeeName;
        set
        {
            OnPropertyChanged(nameof(User));
        }
    }

    public List<string> References
    {
        get => _sample.References;
        set
        {
            OnPropertyChanged(nameof(References));
        }
    }

    public List<SampleList> Sampleslist
    {
        get => _sample.Sampleslist;
        set
        {
            OnPropertyChanged(nameof(Sampleslist));
        }
    }
    public string sDate
    {
        get => "";
        set
        {
            OnPropertyChanged(nameof(sDate));
        }
    }

    public string sComment
    {
        get => "";
        set
        {
            OnPropertyChanged(nameof(sComment));
        }
    }

    public List<string> sReferences
    {
        get => new List<string>();
        set
        {
            OnPropertyChanged(nameof(sReferences));
        }
    }

    public string sEmployeeName
    {
        get => "";
        set
        {
            OnPropertyChanged(nameof(sEmployeeName));
        }
    }

    public Color StatusColor
    {
        get => new Color();
        set
        {
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    public string SelectedStatus
    {
        get => _sample.SelectedStatus;
        set
        {
            OnPropertyChanged(nameof(SelectedStatus));
        }
    }

    public List<string> StatusText
    {
        get => _sample.StatusText;
        set
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }

    [RelayCommand]
    private async Task OnChangeStatus(NewSampleData sample)
    {
        if (sample != null && !string.IsNullOrEmpty(sample.SelectedStatus))
        {
            FirebaseService firebaseService = new FirebaseService();
            await firebaseService.UpdateStatusAsync(sample, sample.SelectedStatus);
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [ObservableProperty]
    private bool areReferencesVisible;

    [RelayCommand]
    private void ToggleReferences()
    {
        AreReferencesVisible = !AreReferencesVisible;
    }
}
