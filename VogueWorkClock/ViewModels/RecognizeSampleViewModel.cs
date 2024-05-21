using Firebase.Database;
using Firebase.Storage;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Storage;
using System.Net;
using VogueWorkClock.Resources.Data;
using Firebase.Database.Query;
using static Google.Api.FieldInfo.Types;

#if ANDROID
using Android.OS;
using Android.Widget;
#endif

namespace VogueWorkClock.ViewModels;
public partial class RecognizeSampleViewModel : ObservableObject
{
    private NewSampleData _data = new NewSampleData();
    private bool _elementsVisible = false;
    private string _uploadedFileNames = string.Empty;

    public string SelectedSeason
    {
        get => _data.Season;
        set
        {
            if (_data.Season != value)
            {
                _data.Season = value;
            }
        }
    }

    public string SelectedYear
    {
        get => _data.Year;
        set
        {
            if (_data.Year != value)
            {
                _data.Year = value;
            }
        }
    }

    public bool ElementsVisible
    {
        get { return _elementsVisible; }
        set
        {
            _elementsVisible = value;
            OnPropertyChanged(nameof(ElementsVisible));
        }
    }

    public string Comment
    {
        get => _data.Description;
        set
        {
            if (_data.Description != value)
            {
                _data.Description = value;
                OnPropertyChanged(nameof(Comment));
            }
        }
    }
    public string EmployeeName
    {
        get => _data.EmployeeName;
        set
        {
            if (_data.EmployeeName != value)
            {
                _data.EmployeeName = value;
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
    private async Task HandleResponse(HttpResponseMessage response)
    {
        string jsonString = await response.Content.ReadAsStringAsync();
        try
        {
            jsonString = Regex.Unescape(jsonString);
            jsonString = jsonString.Trim('"');
            List<Descr>? descriptions = JsonSerializer.Deserialize<List<Descr>>(jsonString);
            if (descriptions != null)
            {
                descriptions.Sort((x, y) => y.Similarity.CompareTo(x.Similarity));

                var viewModel = new CLIPResultViewModel(descriptions, _data);
                var tcs = new TaskCompletionSource<Sampleobj>();

                await Shell.Current.Navigation.PushModalAsync(new CLIPResultPage(viewModel, tcs));

                Sampleobj selectedDescription = await tcs.Task;

                if (selectedDescription != null)
                {
                    await Shell.Current.DisplayAlert("Распознаный пошивочнй образец", $"Тип одежды/стиль: {selectedDescription.DescriptionPath}, Схожесть: {selectedDescription.Similarity}", "OK");

                    var descriptionParts = selectedDescription.DescriptionPath.Split('/');
                    if (descriptionParts.Length == 2)
                    {
                        _data.Type = descriptionParts[0];
                        _data.Style = descriptionParts[1];
                    }
                    _data.Status = selectedDescription.Status;
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Ошибка", "Нет ответа от сервера!", "OK");
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General Error: {ex.Message}");
        }
    }
    private async Task<HttpResponseMessage> SetConnection(MultipartFormDataContent multipartFormDataContent)
    {
        try
        {
            var httpClient = new HttpClient();

            FirebaseClient firebaseClient = new FirebaseClient(Constants.FirebaseRDUrl);

            var credentialsSnapshot = await firebaseClient
            .Child("credentials")
            .OnceSingleAsync<Credentials>();

            var uri = new UriBuilder
            {
                Scheme = credentialsSnapshot.Scheme,
                Host = credentialsSnapshot.Url,
                Path = credentialsSnapshot.Imageupload,
            };

            return await httpClient.PostAsync(uri.Uri, multipartFormDataContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }
    private async Task LoadSample()
    {
        try
        {
            ElementsVisible = false;
            Message = "Загрузка...";

            FileResult? file = await MediaPicker.PickPhotoAsync();
            if (file != null)
            {
                Message = "Обработка...";

                //OnPropertyChanged(Message);

                Stream fileStream = await file.OpenReadAsync();

                //var image = ImageSource.FromStream(() => fileStream);

                var multipartFormDataContent = new MultipartFormDataContent();
                multipartFormDataContent.Add(new StringContent(_data.Season), name: "season");
                multipartFormDataContent.Add(new StringContent(_data.Year), name: "year");
                multipartFormDataContent.Add(new StreamContent(fileStream), name: "file", fileName: file.FileName);

                HttpResponseMessage response = await SetConnection(multipartFormDataContent);
                
                if (response.IsSuccessStatusCode)
                {
                    Task res = HandleResponse(response);

                    MemoryStream memoryStream = new MemoryStream();
                    using (Stream stream = await file.OpenReadAsync())
                    {
                        await stream.CopyToAsync(memoryStream);
                    }

                    _data.RPhoto.DeleteRecognizePhoto();
                    _data.RPhoto.NewRecognizePhoto(file.FileName, memoryStream);

                    UploadedFileNames = file.FileName;
                    ElementsVisible = true;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Ошибка при загрузке файла!", "ОК");
                }
            }
            else
            {
                // Файл не выбран
                await Shell.Current.DisplayAlert("Нет файла", "Файл не выбран!", "OK");
            }

            Message = dwnmsg;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            Message = dwnmsg;
            return;
        }
    }
    private async Task TakePhotoAsync()
    {
        try
        {
            ElementsVisible = false;
            Message2 = "Запуск камеры...";
            FileResult? photo = await MediaPicker.CapturePhotoAsync();

            if (photo != null)
            {
                Message2 = "Сохранение...";

                Stream photoStream = await photo.OpenReadAsync();

                string localFilePath = "";

#if ANDROID
                string? dcim = Android.OS.Environment.DirectoryDcim;
                if (dcim != null)
                {
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
                    localFilePath = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(dcim).AbsolutePath, "CCWorkClock/");
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
                    if (!Directory.Exists(localFilePath))
                    {
                        Directory.CreateDirectory(localFilePath);
                    }
                    localFilePath = Path.Combine(localFilePath, photo.FileName);
                    FileStream localFileStream = File.OpenWrite(localFilePath);

                    await photoStream.CopyToAsync(localFileStream);

                    localFileStream.Close();
                }
                else
                {
                    throw new Exception("Папка для изображений не найдена!");
                }
#elif WINDOWS
                string picturesDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                if (!string.IsNullOrEmpty(picturesDirectory))
                {
                    localFilePath = Path.Combine(picturesDirectory, "CCWorkClock");

                    if (!Directory.Exists(localFilePath))
                    {
                        Directory.CreateDirectory(localFilePath);
                    }

                    localFilePath = Path.Combine(localFilePath, photo.FileName);

                    using (FileStream localFileStream = File.OpenWrite(localFilePath))
                    {
                        await photoStream.CopyToAsync(localFileStream);
                    }
                }
                else
                {
                    throw new Exception("Папка для изображений не найдена!");
                }
#endif
                Message2 = "Обработка...";


                FileStream filestream = File.OpenRead(localFilePath);

                var multipartFormDataContent = new MultipartFormDataContent();
                multipartFormDataContent.Add(new StringContent(_data.Season), name: "season");
                multipartFormDataContent.Add(new StringContent(_data.Year), name: "year");
                multipartFormDataContent.Add(new StreamContent(filestream), name: "file", fileName: photo.FileName);

                HttpResponseMessage response = await SetConnection(multipartFormDataContent);

                if (response.IsSuccessStatusCode)
                {
                    Task res = HandleResponse(response);

                    await Shell.Current.DisplayAlert("Успех", "Файл успешно обработан и сохранен по пути " + localFilePath, "OK");

                    MemoryStream memoryStream = new MemoryStream();
                    using (Stream stream = await photo.OpenReadAsync())
                    {
                        await stream.CopyToAsync(memoryStream);
                    }

                    _data.RPhoto.DeleteRecognizePhoto();
                    _data.RPhoto.NewRecognizePhoto(photo.FileName, memoryStream);

                    UploadedFileNames = photo.FileName;

                    ElementsVisible = true;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Ошибка при загрузке файла!", "ОК");
                }
            }
            else
            {
                throw new Exception("Захват фотографии был отменен или произошла ошибка при работе с камерой.");
            }
            Message2 = photomsg;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            Message2 = photomsg;
            return;
        }
    }

    private async Task LoadRecognitionData()
    {
        try
        {
            Message4 = "Загрузка...";
            FirebaseClient firebaseClient = new FirebaseClient(Constants.FirebaseRDUrl);
            FirebaseStorage storage = new FirebaseStorage("vogueworkclock.appspot.com");

            string path = $"collection/{_data.Year}/{_data.Season}/{_data.Type}/{_data.Style}";

            string statusPattern = @"wait (\d+)-sample";
            Match match = Regex.Match(_data.Status, statusPattern);

            if (match.Success)
            {
                int currentNumber = int.Parse(match.Groups[1].Value);
                await firebaseClient.Child(path).PatchAsync(new { status = $"work {currentNumber}-sample" });

                List<string> photoUrls = new List<string>();
                int photoIndex = 1;
                foreach (var photo in _data.Photos)
                {
                    var stream = new MemoryStream();
                    photo.Position = 0;
                    await photo.CopyToAsync(stream);
                    stream.Position = 0;

                    var fileName = $"{_data.Year}_{_data.Season}_{_data.Type}_{_data.Style}_{currentNumber}-sample_{photoIndex}.jpg";

                    var photoUrl = await storage
                        .Child($"{path}/samples/{currentNumber}")
                        .Child(fileName)
                        .PutAsync(stream);

                    photoUrls.Add(photoUrl);
                    photoIndex++;
                }

                var sample = new
                {
                    date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    comment = _data.Description,
                    user = _data.EmployeeName,
                    references = photoUrls
                };

                await firebaseClient.Child($"{path}/samples/{currentNumber}").PutAsync(sample);

                await Shell.Current.DisplayAlert("Успех", "Данные успешно загружены и сохранены", "OK");
                ClearData();
                ElementsVisible = false;
            }
            else
            {
                if (!_data.Status.StartsWith("wait"))
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Сохранение невозможно, пока образец не получит статус 'wait'", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Со статусом проблема!!!", "OK");
                }
            }

            Message4 = dwnsample;

        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "OK");
        }
    }

    private async Task OpenFileSystem()
    {
        try
        {
            UploadedFileNames = string.Empty;
            UploadedFileNames = _data.RPhoto.GetName();
            _data.stream_Clear();
            _data.Photos.Add(_data.RPhoto.GetStream());
            Message3 = "Загрузка...";
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

                    memoryStream.Position = 0;
                    _data.Photos.Add(memoryStream);

                    UploadedFileNames += (string.IsNullOrEmpty(UploadedFileNames) ? "" : ", ") + file.FileName;
                }

                await Shell.Current.DisplayAlert("Файлы загружены", $"{files.Count()} фото загружено успешно.", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            Message3 = "Ошибка при загрузке.";
            await Shell.Current.DisplayAlert("Ошибка", "Ошибка при загрузке файла: " + ex.Message, "OK");
        }

        Message3 = dwnphoto;
    }
    private string CheckRequiredFields()
    {
        List<string> missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(_data.Season))
            missingFields.Add("Сезон");

        if (string.IsNullOrWhiteSpace(_data.Year))
            missingFields.Add("Год");

        if (string.IsNullOrWhiteSpace(_data.EmployeeName))
            missingFields.Add("Имя сотрудника");

        if (string.IsNullOrWhiteSpace(_data.RPhoto.GetName()))
            missingFields.Add("Распознаваемый образец");

        return string.Join(", ", missingFields);
    }

    private void ClearData()
    {
        //SelectedSeason = string.Empty;
        //SelectedYear = string.Empty;
        Comment = string.Empty;
        EmployeeName = string.Empty;
        UploadedFileNames = string.Empty;
        _data.Clear();
    }

    private static string dwnmsg = "Загрузить образец";
    private static string photomsg = "Сфотографировать образец";
    private static string dwnphoto = "Загрузить фотографии";
    private static string dwnsample = "Обновить данные пошивочного образца";

    [ObservableProperty]
    private string message = dwnmsg;

    [RelayCommand]
    private void OnLoadSample()
	{
        if (_data.Season == "" || _data.Year == "")
        {
            Shell.Current.DisplayAlert("Ошибка", "Необходимо указать год и сезон!", "ОК");
        }
        else
        {
            Task task = LoadSample();
        }
    }

    [ObservableProperty]
    private string message2 = photomsg;

    [RelayCommand]
    private void OnMakeSamplePhoto()
    {
        if (_data.Season == "" || _data.Year == "")
        {
            Shell.Current.DisplayAlert("Ошибка", "Необходимо указать год и сезон!", "ОК");
        }
        else
        {
            Task task = TakePhotoAsync();
        }
    }

    [ObservableProperty]
    private string message3 = dwnphoto;

    [RelayCommand]
    private void OnUploadPhotos()
    {
        Task task = OpenFileSystem();
    }

    [ObservableProperty]
    private string message4 = dwnsample;

    [RelayCommand]
    private void OnUpdateSampleData()
    {
        string missingFields = CheckRequiredFields();
        if (!string.IsNullOrEmpty(missingFields))
        {
            Shell.Current.DisplayAlert("Ошибка", $"Необходимо указать следующие поля: {missingFields}", "OK");
        }
        else
        {
            Task task = LoadRecognitionData();
        }
    }
}
