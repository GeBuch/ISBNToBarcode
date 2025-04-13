using CommunityToolkit.Maui.Views;

using Plugin.Maui.OCR;

using System.Diagnostics;

using TextToBarcode.ViewModels;

using static Microsoft.Maui.ApplicationModel.Permissions;

namespace TextToBarcode.Pages
{
    public partial class MainPage : BasePage<CameraViewViewModel>
    {
        int pageCount;

        public MainPage(CameraViewViewModel viewModel) : base(viewModel)
        {
            viewModel.Init();
            Debug.WriteLine("Init CameraView");
            InitializeComponent();

            Loaded += (s, e) =>
                {
                    pageCount = Navigation.NavigationStack.Count;
                };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await BindingContext.RefreshCamerasCommand.ExecuteAsync(cancellationTokenSource.Token);
        }


        protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);

            Debug.WriteLine($"< < OnNavigatedFrom {pageCount} {Navigation.NavigationStack.Count}");

            if (Navigation.NavigationStack.Count < pageCount)
            {
                Cleanup();
            }
        }
        void Cleanup()
        {
            Camera.Handler?.DisconnectHandler();
        }

        void OnUnloaded(object? sender, EventArgs e)
        {
            //Cleanup();
        }


    }
}
