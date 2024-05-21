using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace VogueWorkClock.Resources.Data
{
    public class Credentials
    {
        public required string Url { get; set; }
        public required string Scheme { get; set; }
        public required string Imageupload { get; set; }
    }
    public class Constants
    {
        public static string FirebaseRDUrl = "https://vogueworkclock-default-rtdb.europe-west1.firebasedatabase.app/";
        public static string FirebaseStorageUrl = "gs://vogueworkclock.appspot.com";
    }

    public class SampleList
    {
        public string sDate { get; set; } = "";
        public string sComment { get; set; } = "";
        public string sEmployeeName { get; set; } = "";
        public List<string> sReferences { get; set; } = new List<string>();
    }

    public class NewSampleData
    {
        public struct RecognizePhoto
        {
            public String Name { get; set; }
            public MemoryStream Stream { get; set; }

            public RecognizePhoto()
            {
                Name = "";
                Stream = new MemoryStream();
            }

            public void NewRecognizePhoto(string name, MemoryStream stream)
            {
                Name = name;
                Stream = stream;
            }

            public void DeleteRecognizePhoto()
            {
                Name = string.Empty;
                Stream.Dispose();
            }

            public string GetName() { return Name; }

            public MemoryStream GetStream() { return Stream; }
        }

        public string Season { get; set; } = "";
        public string Year { get; set; } = "";
        public string Type { get; set; } = "";
        public string Style { get; set; } = "";
        public string Description { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Status { get; set; } = "";
        public string Date { get; set; } = "";
        public List<string> References { get; set; } = new List<string>();
        public Color StatusColor { get; set; } = new Color();
        public List<string> StatusText { get; set; } = new List<string>();
        public string SelectedStatus { get; set; } = "";

        public List<SampleList> Sampleslist { get; set; } = new List<SampleList>();
        public List<MemoryStream> Photos { get; set; } = new List<MemoryStream>();

        public RecognizePhoto RPhoto = new RecognizePhoto();
        
        public void Clear()
        {
            //Season = "";
            //Year = "";
            Style = "";
            Description = "";
            EmployeeName = "";
            Status = "";
            Date = "";
            stream_Clear();
            RPhoto.DeleteRecognizePhoto();
        }
        public void stream_Clear()
        {
            foreach (var stream in Photos)
            {
                stream.Dispose();
            }
            Photos.Clear();
        }

    }

    public class Descr
    {
        [JsonPropertyName("description_path")]
        public string DescriptionPath { get; set; } = "";

        [JsonPropertyName("similarity")]
        public double Similarity { get; set; } = 0;
    }

    public class Sampleobj
    {
        public string DescriptionPath { get; set; } = "";
        public double Similarity { get; set; }
        public string Date { get; set; } = "";
        public string TextDescription { get; set; } = "";
        public List<string> ImageUrls { get; set; } = new List<string>();
        public string Status { get; set; } = "";
        public string User { get; set; } = "";
    }

    public class FirebaseService
    {
        private readonly FirebaseClient _firebaseClient;

        public FirebaseService()
        {
            _firebaseClient = new FirebaseClient(Constants.FirebaseRDUrl);
        }

        public async Task<JObject> GetJsonDataAsync()
        {
            var jsonData = await _firebaseClient
                .Child("collection")
                .OnceSingleAsync<JObject>();

            return jsonData;
        }

        public async Task UpdateStatusAsync(NewSampleData sample, string newStatus)
        {
            string path = $"collection/{sample.Year}/{sample.Season}/{sample.Type}/{sample.Style}";
            await _firebaseClient.Child(path).PatchAsync(new { status = newStatus });
        }
    }
}
