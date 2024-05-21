using Firebase.Database;
using Firebase.Storage;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Storage;
using System.Net;
using VogueWorkClock.Resources.Data;
using static Google.Api.FieldInfo.Types;
using Firebase.Database.Query;

#if ANDROID
using Android.OS;
#endif

namespace VogueWorkClock.ViewModels;
public partial class NewSampleViewModel : ObservableObject
{
    private NewSampleData _SampleData = new NewSampleData();
    private string _uploadedFileNames = string.Empty;

    public string SelectedSeason
    {
        get => _SampleData.Season;
        set
        {
            if (_SampleData.Season != value)
            {
                _SampleData.Season = value;
                OnPropertyChanged(nameof(SelectedSeason));
            }
        }
    }
    public string SelectedYear
    {
        get => _SampleData.Year;
        set
        {
            if (_SampleData.Year != value)
            {
                _SampleData.Year = value;
                OnPropertyChanged(nameof(SelectedYear));
            }
        }
    }
    public string SelectedType
    {
        get => _SampleData.Type;
        set
        {
            if (_SampleData.Type != value)
            {
                _SampleData.Type = value;
                OnPropertyChanged(nameof(SelectedType));
            }
        }
    }
    public string Style
    {
        get => _SampleData.Style;
        set
        {
            if (_SampleData.Style != value)
            {
                _SampleData.Style = value;
                OnPropertyChanged(nameof(Style));
            }
        }
    }
    public string Description
    {
        get => _SampleData.Description;
        set
        {
            if (_SampleData.Description != value)
            {
                _SampleData.Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }
    }
    public string EmployeeName
    {
        get => _SampleData.EmployeeName;
        set
        {
            if (_SampleData.EmployeeName != value)
            {
                _SampleData.EmployeeName = value;
                OnPropertyChanged(nameof(EmployeeName));
            }
        }
    }
    public string UploadedFileNames
    {
        get => _uploadedFileNames;
        set
        {
            if (_uploadedFileNames != value)
            {
                _uploadedFileNames = value; 
                OnPropertyChanged(nameof(UploadedFileNames));
            }
        }
    }

    public async Task OpenFileSystem()
    {
        try
        {
            UploadedFileNames = string.Empty;
            _SampleData.stream_Clear();
            Message = "Загрузка...";
            var options = new PickOptions
            {
                FileTypes = FilePickerFileType.Images
            };

            IEnumerable<FileResult> files = await FilePicker.PickMultipleAsync(options);
            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    using (Stream fileStream = await file.OpenReadAsync())
                    {
                        await fileStream.CopyToAsync(memoryStream);
                    }

                    memoryStream.Position = 0; // Сбрасываем позицию потока
                    _SampleData.Photos.Add(memoryStream);  // Добавляем поток в список фотографий

                    // Обновляем строку с названиями файлов
                    UploadedFileNames += (string.IsNullOrEmpty(UploadedFileNames) ? "" : ", ") + file.FileName;
                }

                // Отображаем сообщение о загруженных файлах
                await Shell.Current.DisplayAlert("Файлы загружены", $"{files.Count()} фото загружено успешно.", "OK");
            }
            
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            Message = "Ошибка при загрузке.";
            await Shell.Current.DisplayAlert("Ошибка", "Ошибка при загрузке файла: " + ex.Message, "OK");
        }

        Message = photomsg;
    }

    public async Task SaveSampleDataAsync()
    {
        Message2 = "Загрузка...";
        FirebaseStorage storage = new FirebaseStorage("vogueworkclock.appspot.com");

        string path = $"collection/{_SampleData.Year}/{_SampleData.Season}/{_SampleData.Type}/{_SampleData.Style}";
        string filename = $"{_SampleData.Year}_{_SampleData.Season}_{_SampleData.Type}_{_SampleData.Style}";

        var photoUrls = new List<string>();
        for (int i = 0; i < _SampleData.Photos.Count; i++)
        {
            var photoUrl = await UploadPhotoAsync(storage, _SampleData.Photos[i], path, filename, i);
            photoUrls.Add(photoUrl);
        }

        var saveData = new
        {
            description = _SampleData.Description,
            user = _SampleData.EmployeeName,
            date = DateTime.Now.ToString("yyyy-MM-dd"),
            status = "wait 1-sample",
            references = photoUrls
        };

        FirebaseClient firebaseClient = new FirebaseClient(Constants.FirebaseRDUrl);
        var databaseReference = firebaseClient.Child(path);

        var existingData = await databaseReference.OnceAsync<object>();
        if (existingData == null || !existingData.Any())
        {
            await databaseReference.PutAsync(saveData);
            await Shell.Current.DisplayAlert("Успешно", $"{_SampleData.Season}, {_SampleData.Year}, {_SampleData.Type}, {_SampleData.Style}, {_SampleData.Description}, {_SampleData.EmployeeName}, {_uploadedFileNames}", "OK");
            ClearData();
        }
        else
        {
            await Shell.Current.DisplayAlert("Предупреждение", "Образец с такими данными уже существует!", "ОК");
        }
        Message2 = savemsg;
    }

    private async Task<string> UploadPhotoAsync(FirebaseStorage storage, MemoryStream photoStream, string path, string filename, int index)
    {
        // Формирование уникального имени файла на основе пути и порядкового номера

        var fileName = $"{path}/{filename}_{index + 1}.jpg"; // Индекс увеличивается на 1 для соответствия порядковому номеру

        var downloadUrl = await storage.Child(fileName).PutAsync(photoStream);

        return downloadUrl;
    }

    public string CheckRequiredFields()
    {
        List<string> missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(_SampleData.Season))
            missingFields.Add("Сезон");

        if (string.IsNullOrWhiteSpace(_SampleData.Year))
            missingFields.Add("Год");

        if (string.IsNullOrWhiteSpace(_SampleData.Type))
            missingFields.Add("Тип");

        if (string.IsNullOrWhiteSpace(_SampleData.Style))
            missingFields.Add("Стиль");

        if (string.IsNullOrWhiteSpace(_SampleData.Description))
            missingFields.Add("Описание");

        if (string.IsNullOrWhiteSpace(_SampleData.EmployeeName))
            missingFields.Add("Имя сотрудника");

        return string.Join(", ", missingFields);
    }

    public void ClearData()
    {
        //SelectedSeason = string.Empty;
        //SelectedYear = string.Empty;
        //SelectedType = string.Empty;
        Style = string.Empty;
        Description = string.Empty;
        EmployeeName = string.Empty;
        UploadedFileNames = string.Empty;
        _SampleData.Clear();
    }

    private static string photomsg = "Загрузить фотографии";
    private static string savemsg = "Сохранить образец";

    [ObservableProperty]
    private string message = photomsg;

    [RelayCommand]
    private void OnUploadPhotos()
	{
        Task task = OpenFileSystem();
    }

    [ObservableProperty]
    private string message2 = savemsg;

    [RelayCommand]
    private void OnSaveData()
    {
        string missingFields = CheckRequiredFields();
        if (!string.IsNullOrEmpty(missingFields))
        {
            Shell.Current.DisplayAlert("Ошибка", $"Необходимо указать следующие поля: {missingFields}", "OK");
        }
        else
        {
            Task task = SaveSampleDataAsync();
        }
    }
}
