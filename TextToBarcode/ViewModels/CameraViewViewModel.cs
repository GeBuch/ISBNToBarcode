using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Plugin.Maui.OCR;

using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TextToBarcode.ViewModels
{
    public partial class CameraViewViewModel(ICameraProvider cameraProvider, IOcrService ocrService) : BaseViewModel
    {
        private const string ISBN_REGEX = @"(ISBN[-]*(1[03])*[ ]*(: ){0,1})*(([0-9Xx][- ]*){13}|([0-9Xx][- ]*){10})";
        readonly ICameraProvider cameraProvider = cameraProvider;
        readonly IOcrService ocrService = ocrService;
        OcrOptions? ocrOptions;

        public IReadOnlyList<CameraInfo> Cameras => cameraProvider.AvailableCameras ?? [];

        public CancellationToken Token => CancellationToken.None;

        public ICollection<CameraFlashMode> FlashModes { get; } = Enum.GetValues<CameraFlashMode>();

        [ObservableProperty]
        public partial CameraFlashMode FlashMode { get; set; }

        [ObservableProperty]
        public partial CameraInfo? SelectedCamera { get; set; }

        [ObservableProperty]
        public partial Size SelectedResolution { get; set; }

        [ObservableProperty]
        public partial float CurrentZoom { get; set; }

        [ObservableProperty]
        public partial string CameraNameText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string ZoomRangeText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string CurrentZoomText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string FlashModeText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string ResolutionText { get; set; } = string.Empty;
        [ObservableProperty]
        public partial string ExtractedText { get; set; } = string.Empty;
        [ObservableProperty]
        public partial bool IsBusy { get; set; } = false;
        [ObservableProperty]
        public partial string[] Languages { get; set; } = Array.Empty<string>();
        [ObservableProperty]
        public partial string SelectedLanguage { get; set; }

        [RelayCommand]
        async Task RefreshCameras(CancellationToken token) => await cameraProvider.RefreshAvailableCameras(token);
        [RelayCommand]
        private async Task CaptureImage(CameraView cameraView)
        {
            await cameraView.CaptureImage(CancellationToken.None);
        }

        [RelayCommand]
        public async Task OnMediaCaptured(MediaCapturedEventArgs e)
        {
            Debug.WriteLine("Media Captured");
            IsBusy = true;
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(50));
            var ms = new MemoryStream();
            e.Media.CopyTo(ms);
            //var a = await ocrService.RecognizeTextAsync(ms.ToArray(), ocrOptions, cancellationTokenSource.Token);
            await ocrService.StartRecognizeTextAsync(ms.ToArray(), ocrOptions, cancellationTokenSource.Token);

        }

        private void OcrComplete(object? sender, OcrCompletedEventArgs e)
        {
            Debug.WriteLine("OCR complete");
            if (e.IsSuccessful)
            {
                var text = e.Result?.MatchedValues.Select(s => SanitizeExtractedText(s)).OrderByDescending(x => x.Length).FirstOrDefault() ?? String.Empty;
                this.ExtractedText = text.Length == 13 ? text : CameraViewViewModel.ISBN10To13(text);
            }
            IsBusy = false;
        }

        private static string ISBN10To13(string text)
        {

            string ISBN10 = string.Concat("978", text.AsSpan(0, 9));
            int isbn10_1 = Convert.ToInt32(ISBN10.Substring(0, 1));
            int isbn10_2 = Convert.ToInt32(Convert.ToInt32(ISBN10.Substring(1, 1)) * 3);
            int isbn10_3 = Convert.ToInt32(ISBN10.Substring(2, 1));
            int isbn10_4 = Convert.ToInt32(Convert.ToInt32(ISBN10.Substring(3, 1)) * 3);
            int isbn10_5 = Convert.ToInt32(ISBN10.Substring(4, 1));
            int isbn10_6 = Convert.ToInt32(Convert.ToInt32(ISBN10.Substring(5, 1)) * 3);
            int isbn10_7 = Convert.ToInt32(ISBN10.Substring(6, 1));
            int isbn10_8 = Convert.ToInt32(Convert.ToInt32(ISBN10.Substring(7, 1)) * 3);
            int isbn10_9 = Convert.ToInt32(ISBN10.Substring(8, 1));
            int isbn10_10 = Convert.ToInt32(Convert.ToInt32(ISBN10.Substring(9, 1)) * 3);
            int isbn10_11 = Convert.ToInt32(ISBN10.Substring(10, 1));
            int isbn10_12 = Convert.ToInt32(Convert.ToInt32(ISBN10.Substring(11, 1)) * 3);
            int k = (isbn10_1 + isbn10_2 + isbn10_3 + isbn10_4 + isbn10_5 + isbn10_6 + isbn10_7 + isbn10_8 + isbn10_9 + isbn10_10 + isbn10_11 + isbn10_12);
            int checkDigit = 10 - ((isbn10_1 + isbn10_2 + isbn10_3 + isbn10_4 + isbn10_5 + isbn10_6 + isbn10_7 + isbn10_8 + isbn10_9 + isbn10_10 + isbn10_11 + isbn10_12) % 10);
            if (checkDigit == 10)
                checkDigit = 0;
            return ISBN10 + checkDigit.ToString();
        }

        private bool OcrValidation(string extractedText)
        {
            Debug.WriteLine("OCR Validating");
            Debug.WriteLine(extractedText);
            extractedText = SanitizeExtractedText(extractedText);
            return (extractedText.Length == 13) || (extractedText.Length == 10);

        }
        private string SanitizeExtractedText(string extractedText) => extractedText.Replace("-", String.Empty)
                                                                                   .Replace("ISBN13", String.Empty)
                                                                                   .Replace("ISBN10", String.Empty)
                                                                                   .Replace("ISBN", String.Empty)
                                                                                   .Replace(":", String.Empty)
                                                                                   .Trim();
        public void Init()
        {
            ocrOptions = new OcrOptions.Builder().SetLanguage(SelectedLanguage)
                                                     .SetTryHard(false)
                                                     .AddPatternConfig(new OcrPatternConfig(ISBN_REGEX, OcrValidation))
                                                     //.SetCustomCallback(OcrComplete)
                                                     .Build();
            ocrService.InitAsync();
            Languages = ocrService.SupportedLanguages.ToArray();
            SelectedLanguage = ocrService.SupportedLanguages.FirstOrDefault() ?? string.Empty;
            ocrService.RecognitionCompleted += OcrComplete;
            this.PropertyChanged += OnPropertyChanged;

        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedLanguage))
            {
                ocrOptions = new OcrOptions.Builder().SetLanguage(SelectedLanguage)
                                                         .SetTryHard(false)
                                                         .AddPatternConfig(new OcrPatternConfig(ISBN_REGEX, OcrValidation))
                                                         //.SetCustomCallback(OcrComplete)
                                                         .Build();
            }
        }
    }
}
