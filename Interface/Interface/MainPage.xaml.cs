using Core;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Interface
{
    public sealed partial class MainPage : Page
    {
        volatile TaskCompletionSource<SoftwareBitmap> copiedVideoFrameComplete;

        CancellationTokenSource requestStopCancellationToken;
        CameraPreviewManager cameraPreviewManager;
        FacialDrawingHandler drawingHandler;
        FrameProcessor faceDetectionProcessor;

        private bool _isFinished = false;
        private string currentVisualState;

        string CurrentVisualState
        {
            get
            {
                return (currentVisualState);
            }
            set
            {
                if (currentVisualState != value)
                {
                    currentVisualState = value;
                    ChangeStateAsync();
                }
            }
        }

        public MainPage()
        {
            InitializeComponent();

            Loaded += App_Loaded;
            Unloaded += App_Unloaded;
        }


        async Task ChangeStateAsync()
        {
            await Dispatcher.RunAsync(
              Windows.UI.Core.CoreDispatcherPriority.Normal,
              () =>
              {
                  VisualStateManager.GoToState(this, currentVisualState, false);
              }
            );
        }

        private async void App_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            CurrentVisualState = "Playing";

            requestStopCancellationToken = new CancellationTokenSource();
            cameraPreviewManager = new CameraPreviewManager(captureElement);

            var videoProperties = await cameraPreviewManager.StartPreviewToCaptureElementAsync(vcd => vcd.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
            faceDetectionProcessor = new FrameProcessor(cameraPreviewManager.MediaCapture, cameraPreviewManager.VideoProperties);
            drawingHandler = new FacialDrawingHandler(drawCanvas, videoProperties, Colors.White);

            faceDetectionProcessor.FrameProcessed += (s, ev) =>
            {
                // This event will fire on the task thread that the face
                // detection processor is running on. 
                drawingHandler.SetLatestFrameReceived(ev.Results);
                CurrentVisualState = ev.Results.Count > 0 ? "PlayingWithFace" : "Playing";
                CopyBitmapForOxfordIfRequestPending(ev.Frame.SoftwareBitmap);
            };

            try
            {
                await faceDetectionProcessor.RunFrameProcessingLoopAsync(requestStopCancellationToken.Token);
            }
            catch (OperationCanceledException)
            {

            }

            await cameraPreviewManager.StopPreviewAsync();
            requestStopCancellationToken.Dispose();
            CurrentVisualState = "Stopped";
        }

        private void App_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Just set a simple flag so our async workers know when we bailed and stop.
            _isFinished = true;
        }


        void CopyBitmapForOxfordIfRequestPending(SoftwareBitmap bitmap)
        {
            if ((copiedVideoFrameComplete != null) && (!copiedVideoFrameComplete.Task.IsCompleted))
            {
                // We move to RGBA16 because that is a format that we will then be able
                // to use a BitmapEncoder on to move it to PNG and we *cannot* do async
                // work here because it'll break our processing loop.
                var convertedRgba16Bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Rgba16);
                copiedVideoFrameComplete.SetResult(convertedRgba16Bitmap);
            }
        }

        //private async void TakeSnapshot(object sender, object e)
        //{
        //    _timerObject.Stop();

        //    // Create worker on new bg thread and let it check the data from our media capture.
        //    //_worker = new BackgroundWorker();
        //    //_worker.DoWork += ReadDataStream;
        //    //_worker.RunWorkerCompleted += ReadDataStreamComplete;
        //    //_worker.RunWorkerAsync();

        //    try
        //    {
        //        //photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
        //        ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
        //        //await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);

        //        CameraCaptureUI captureUI = new CameraCaptureUI();
        //        captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
        //        captureUI.PhotoSettings.CroppedSizeInPixels = new Size(200, 200);

        //        StorageFile photo2 = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

        //        using (var photoStream = new InMemoryRandomAccessStream())
        //        {

        //            //await mediaCapture.CapturePhotoToStreamAsync(imageProperties, photoStream);
        //            //photoStream.Seek(0);

        //            //BitmapImage bitmap = new BitmapImage();
        //            //await bitmap.SetSourceAsync(photoStream);





        //            var lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
        //            var photo = await lowLagCapture.CaptureAsync();

        //            //convert to displayable format
        //            SoftwareBitmap displayableImage;
        //            using (var frame = photo.Frame)
        //            {
        //                displayableImage = SoftwareBitmap.Convert(photo.Frame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        //            }

        //            var source = new SoftwareBitmapSource();
        //            await source.SetBitmapAsync(displayableImage);

        //            //previewImage.Source = null;
        //            //previewImage.Source = source;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //e.Cancel = true;
        //        //status.Text = ex.Message;
        //    }

        //    _timerObject.Start();
        //}

        //private async void ReadDataStream(object sender, DoWorkEventArgs e)
        //{
        //    if (!_isFinished)
        //    {
        //        try
        //        {
        //            photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
        //            ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
        //            await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);

        //            //status.Text = "Take Photo succeeded: " + photoFile.Path;

        //            IRandomAccessStream photoStream = await photoFile.OpenReadAsync();
        //            BitmapImage bitmap = new BitmapImage();
        //            bitmap.SetSource(photoStream);

        //            e.Result = bitmap;
        //        }
        //        catch (Exception ex)
        //        {
        //            e.Cancel = true;
        //            //status.Text = ex.Message;
        //        }
        //    }
        //}

        //private void ReadDataStreamComplete(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    if (!e.Cancelled)
        //    {
        //        //previewImage.Source = null;

        //        var bitmap = (BitmapImage)e.Result;
        //        if(bitmap != null)
        //        {
        //            //previewImage.Source = bitmap;
        //        }
        //    }
        //}



        //private async void InitMediaCapture()
        //{
        //    try
        //    {
        //        if (mediaCapture != null)
        //        {
        //            // Cleanup MediaCapture object
        //            if (isPreviewing)
        //            {
        //                await mediaCapture.StopPreviewAsync();
        //                //previewImage.Source = null;
        //                isPreviewing = false;
        //            }
        //            if (isRecording)
        //            {
        //                await mediaCapture.StopRecordAsync();
        //                isRecording = false;
        //            }
        //            mediaCapture.Dispose();
        //            mediaCapture = null;
        //        }

        //        // "Initializing camera to capture audio and video...";
        //        // Use default initialization
        //        mediaCapture = new MediaCapture();
        //        await mediaCapture.InitializeAsync();

        //        // Set callbacks for failure and recording limit exceeded
        //        // "Device successfully initialized for video recording!";
        //        mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);
        //        mediaCapture.RecordLimitationExceeded += new Windows.Media.Capture.RecordLimitationExceededEventHandler(mediaCapture_RecordLimitExceeded);

        //        // Start Preview                
        //        //                previewElement.Source = mediaCapture;
        //        await mediaCapture.StartPreviewAsync();
        //        isPreviewing = true;
        //        // "Camera preview succeeded";

        //    }
        //    catch (Exception ex)
        //    {
        //        // "Unable to initialize camera for audio/video mode: " + ex.Message;
        //    }
        //}

        //private async void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        //{
        //    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
        //    {
        //        try
        //        {
        //            // "MediaCaptureFailed: " + currentFailure.Message;

        //            if (isRecording)
        //            {
        //                await mediaCapture.StopRecordAsync();
        //                // "\n Recording Stopped";
        //            }
        //        }
        //        catch (Exception)
        //        {
        //        }
        //        finally
        //        {
        //            // "\nCheck if camera is diconnected. Try re-launching the app";
        //        }
        //    });
        //}

        //public async void mediaCapture_RecordLimitExceeded(Windows.Media.Capture.MediaCapture currentCaptureObject)
        //{
        //    try
        //    {
        //        if (isRecording)
        //        {
        //            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
        //            {
        //                try
        //                {
        //                    // "Stopping Record on exceeding max record duration";
        //                    await mediaCapture.StopRecordAsync();
        //                    isRecording = false;
        //                    if (mediaCapture.MediaCaptureSettings.StreamingCaptureMode == StreamingCaptureMode.Audio)
        //                    {
        //                        // "Stopped record on exceeding max record duration: " + audioFile.Path;
        //                    }
        //                    else
        //                    {
        //                        // "Stopped record on exceeding max record duration: " + recordStorageFile.Path;
        //                    }
        //                }
        //                catch (Exception e)
        //                {
        //                    // = e.Message;
        //                }
        //            });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        // = e.Message;
        //    }
        //}
    }
}
