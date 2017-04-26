using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.UI.Popups;
using Windows.UI.Core;
using Windows.Media.MediaProperties;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Microsoft.ProjectOxford.Vision.Contract;
using Windows.Storage;
using System.Collections.ObjectModel;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPCameraCapandOCR
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        MediaCapture _mediaCapture;
        bool _isPreviewing;
        DisplayRequest _displayRequest = new DisplayRequest();

        public MainPage()
        {
            this.InitializeComponent();

            Application.Current.Suspending += Application_Suspending;

            Application.Current.UnhandledException += Current_UnhandledException;

        }

        private void Current_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string msg = Utils.FormatExceptionMessage(e.Exception);
            ShowMessageToUser(msg);
        }

        private async void ShowMessageToUser(string message)
        {
            MessageDialog msgdlg = new MessageDialog(message, "Application Message");

            // Show the MessageDialog     
            IUICommand command = await msgdlg.ShowAsync();



        }

        private async Task StartPreviewAsync()
        {
            try
            {

                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();

                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                ShowMessageToUser("The app was denied access to the camera");
                return;
            }

            try
            {
                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                _mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }

        }

        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                ShowMessageToUser("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !_isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await StartPreviewAsync();
        }
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CleanupCameraAsync();
        }
        private async Task CleanupCameraAsync()
        {
            if (_mediaCapture != null)
            {
                if (_isPreviewing)
                {
                    await _mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    if (_displayRequest != null)
                    {
                        _displayRequest.RequestRelease();
                    }

                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                });
            }

        }

        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupCameraAsync();
                deferral.Complete();
            }
        }

        private async Task<ImageSource> SoftwareBitMapToImageSource(SoftwareBitmap softwareBitmap)
        {
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softwareBitmap);

            // Set the source of the Image control
            return source;
        }

        private async void CapturePic_Click(object sender, RoutedEventArgs e)
        {
            SoftwareBitmap softwareBitmap = await CaptureImage();

            CapturedImage.Source = await SoftwareBitMapToImageSource(softwareBitmap);

        }

        private async Task<SoftwareBitmap> CaptureImage()
        {
            // Prepare and capture photo
            var lowLagCapture = await _mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));

            var capturedPhoto = await lowLagCapture.CaptureAsync();
            var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;

            await lowLagCapture.FinishAsync();

            return softwareBitmap;

        }

        private ObservableCollection<string> GetOCRWords(OcrResults ocrResults)
        {
            ObservableCollection<string> words = new ObservableCollection<string>();


            foreach (var region in ocrResults.Regions)
            {
                foreach (var line in region.Lines)
                {
                    foreach (var word in line.Words)
                    {
                        System.Diagnostics.Debug.WriteLine(word.Text);
                        words.Add(word.Text);
                    }
                }
            }

            return words;

        }

        private async Task<IRandomAccessStream> GetStreamFromSoftwareBitmap(SoftwareBitmap softwareBitmap)
        {
            //https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/imaging#save-a-softwarebitmap-to-a-file-with-bitmapencoder
            //https://msdn.microsoft.com/library/bc062c66-ba64-4d1c-931d-6d88ac2fcf7c

            IStorageItem storageItem = await Windows.Storage.ApplicationData.Current.TemporaryFolder.TryGetItemAsync("test");

            StorageFile storageFile;

            if (storageItem == null)
            {
                storageFile = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync("test");
            }
            else
            {
                storageFile = await Windows.Storage.ApplicationData.Current.TemporaryFolder.GetFileAsync("test");
            }


            using (IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Set the software bitmap
                encoder.SetSoftwareBitmap(softwareBitmap);

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception err)
                {
                    throw err;
                }

            }

            return await storageFile.OpenAsync(FileAccessMode.ReadWrite);
        }

        private async void RecognizeText_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                // capture image
                SoftwareBitmap softwareBitmap = await CaptureImage();

                // display small image of what was captured in the UI
                CapturedImage.Source = await SoftwareBitMapToImageSource(softwareBitmap);

                // Computer vision key
                string connectString = "cd7ebe5a4aba4d2ebb820722c7022289";

                // initialize ComputerVisionHelper
                ComputerVisionHelper.Initialize(connectString);

                using (IRandomAccessStream stream = await GetStreamFromSoftwareBitmap(softwareBitmap))
                {
                    OcrResults ocrResults = await ComputerVisionHelper.SendImageToOCR(stream.AsStreamForRead());

                    // get the OCR results
                    var words = GetOCRWords(ocrResults);

                    // if the list is empty it clear the prevous list or puts int the words it found
                    WordList.ItemsSource = words;

                }
            }
            catch (Exception ex)
            {
                string msg = Utils.FormatExceptionMessage(ex);
                ShowMessageToUser(msg);
            }

        }

    }
}
