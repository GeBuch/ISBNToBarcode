using CommunityToolkit.Maui;

using Microsoft.Extensions.Logging;

using Plugin.Maui.OCR;

using TextToBarcode.ViewModels;

namespace TextToBarcode
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitCamera()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("3OF9_NEW.TTF", "BarcodeFont");
                    fonts.AddFont("LibreBarcodeEAN13Text-Regular.ttf", "EAN13Font");
                    fonts.AddFont("EAN-13.ttf", "EAN13");
                })
                .UseOcr();
            builder.Services.AddTransient<CameraViewViewModel>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
