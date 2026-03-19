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
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelController.ViewModel {
    partial class MainViewModel : ObservableObject {

        private readonly HttpClient _httpClient = new HttpClient();

        [ObservableProperty]
        private string _productName = string.Empty;

        [ObservableProperty]
        private string _ingredients = string.Empty;

        [ObservableProperty]
        private ImageSource? _generatedImageBitmap;

        [ObservableProperty]
        private string _producer = "XYZ \n ul.XYZ \nXYZ";

        [ObservableProperty]
        private string _description = "Data minimalnej trwałości jest jednocześnie nr partii. \r\nNajlepiej spożyć przed:..................................... \r\nNależy przechowywać w temperaturze od 0°C do 20°C \r\nw zaciemnionym miejscu. \r\nPo otwarciu opakowania produkt przechowywać \r\nw lodówce, spożyć w ciągu 48 godz.";

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

        [RelayCommand]
        private async Task GenerateAiImage() {
            if (string.IsNullOrWhiteSpace(ProductName)) {
                MessageBox.Show("Podaj nazwę produktu przed zapisem!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string prompt = IsSameAsIngredients ? Ingredients : AiPrompt;
            string key = GetApiKey();
            if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrEmpty(key)) return;

            string improvedPrompt = $"Minimalist vector corner illustration of {prompt}. Placed strictly in the corners. Pure white background. Huge empty negative space in the exact center for text. Flat design, clean lines, simple, no extra details, no clutter.";

            string json = $$"""
                {
                  "contents": [
                    {
                      "parts": [
                        { "text": "{{improvedPrompt}}" }
                      ]
                    }
                  ]
                }
                """;

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-image-preview:generateContent?key={key}";

            try {
                var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

                var node = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                string? base64 = node?["candidates"]?[0]?["content"]?["parts"]?[0]?["inlineData"]?["data"]?.ToString();

                if (!string.IsNullOrEmpty(base64)) {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedImages", $"{ProductName}-{DateTime.Now:yyyyMMddHHmmss}.png");
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    await File.WriteAllBytesAsync(path, Convert.FromBase64String(base64));

                    GeneratedImageBitmap = LoadBitmapImageFromPath(path);
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"Błąd: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task Proba() {
            GeneratedImageBitmap = LoadBitmapImageFromPath(@"X:\VS Code\C#\LabelController\bin\Debug\net8.0-windows\GeneratedImages\Zupa-20260319212929.png");
        }


        private string GetApiKey() {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            return config["GoogleAi:ApiKey"] ?? string.Empty;
        }


        private BitmapImage LoadBitmapImageFromPath(string path) {
            
            var bitmap = new BitmapImage();
            
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();

            bitmap.Freeze();

            return bitmap;
        }
    }
}


