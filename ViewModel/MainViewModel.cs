using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelController.Data;
using LabelController.Model;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        private bool _isSameAsIngredients = true;

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
            string prompt = IsSameAsIngredients ? Ingredients : AiPrompt;
            string key = GetApiKey();
            if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrEmpty(key)) return;

            string json = $$"""{"contents":[{"parts":[{"text":"Wygeneruj mi tło do etykiety: {{prompt}}. tło białe, środek pusty, dane tylko w rogach."}]}]}""";
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-image-preview:generateContent?key={key}";

            try {
                var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

                var node = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                string? base64 = node?["candidates"]?[0]?["content"]?["parts"]?[0]?["inlineData"]?["data"]?.ToString();

                if (!string.IsNullOrEmpty(base64)) {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedImages", $"label_{Guid.NewGuid()}.png");
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    await File.WriteAllBytesAsync(path, Convert.FromBase64String(base64));

                    // GeneratedImageBitmap = LoadBitmapImageFromPath(path);
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"Błąd: {ex.Message}");
            }
        }
    }
}


