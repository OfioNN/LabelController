using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelController.Data;
using LabelController.Model;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace LabelController.ViewModel {
    partial class MainViewModel : ObservableObject {

        private readonly HttpClient _httpClient = new HttpClient();

        [ObservableProperty]
        private string _productName = string.Empty;

        [ObservableProperty]
        private string _ingredients = string.Empty;

        [ObservableProperty]
        private string _producer = "Lorem ipsum";

        [ObservableProperty]
        private string _description = "Lorem ipsum";

        [ObservableProperty]
        private string _aiPrompt = string.Empty;

        [ObservableProperty]
        private bool _isSameAsIngredients = false;

        public MainViewModel() {
            using (var db = new AppDbContext()) {
                db.Database.EnsureCreated(); 
            }
        }

        [RelayCommand]
        private void Save() {
            if (string.IsNullOrWhiteSpace(ProductName)) {
                MessageBox.Show("Podaj nazwę produktu przed zapisem!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(Ingredients)) {
                MessageBox.Show("Podaj skład produktu przed zapisem!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newLabel = new ProductLabel {
                Name = ProductName,
                Ingredients = Ingredients,
                ImagePath = "",
                Producer = Producer,
                Description = Description,
            };

            using (var db = new AppDbContext()) {
                db.ProductLabels.Add(newLabel);
                db.SaveChanges();
            }

            MessageBox.Show($"Etykieta '{ProductName}' została poprawnie zapisana!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetApiKey() {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            return config["GoogleAi:ApiKey"] ?? string.Empty;
        }


        [RelayCommand]
        private async Task GenerateAiImage() {
            string finalPrompt = IsSameAsIngredients ? Ingredients : AiPrompt;
            if (string.IsNullOrWhiteSpace(finalPrompt)) return;

            string apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey)) {
                MessageBox.Show("Nie znaleziono klucza API w appsettings.json!");
                return;
            }

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/imagen-3:predict?key={apiKey}";

            try {
                // 1. Przygotowanie żądania (Payload)
                var payload = new {
                    instances = new[] {
                new { prompt = finalPrompt }
            },
                    parameters = new {
                        sampleCount = 1,
                        aspectRatio = "1:1" // Możesz zmienić na "4:3" lub "16:9"
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // 2. Wysyłka do Google
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode) {
                    string errorLog = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Błąd API Google ({response.StatusCode}): {errorLog}");
                    return;
                }

                // 3. Odczytanie odpowiedzi
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ImagenResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Predictions != null && result.Predictions.Count > 0) {
                    // Dekodujemy Base64 z powrotem na bajty obrazu
                    byte[] imageBytes = Convert.FromBase64String(result.Predictions[0].BytesBase64);

                    // 4. Zapis lokalny
                    string fileName = $"label_{Guid.NewGuid()}.png";
                    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedImages", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                    await File.WriteAllBytesAsync(fullPath, imageBytes);

                    // 5. Odświeżenie UI
                    //GeneratedImageBitmap = LoadBitmapImageFromPath(fullPath);
                    //_generatedLocalImagePath = fullPath;
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"Błąd połączenia: {ex.Message}");
            }
        }

    }
    public class ImagenResponse {
        public List<ImagenPrediction> Predictions { get; set; } = new();
    }

    public class ImagenPrediction {
        public string BytesBase64 { get; set; } = string.Empty;
        public string MimeType { get; set; } = "image/png";
    }
}
