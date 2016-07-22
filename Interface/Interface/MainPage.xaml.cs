using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Interface
{
    public sealed partial class MainPage : Page
    {
        private BackgroundWorker _worker;
        private CoreDispatcher _dispatcher;
        private MediaCapture mediaCapture;

        private bool _isFinished = false;
        private bool isPreviewing;
        private bool isRecording;

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += App_Loaded;
            Unloaded += App_Unloaded;
        }

        private void App_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            // Init the media capture device. No idea what two webcams will do right now. To be tested :)
            // Also, no webcam right now. Check tomo if this actually works!
            InitMediaCapture();

            // Create worker on new bg thread and let it check the data from our media capture.
            _worker = new BackgroundWorker();
            _worker.DoWork += ReadDataStream;
            _worker.RunWorkerAsync();
        }

        private void App_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Just set a simple flag so our async workers know when we bailed and stop.
            _isFinished = true;
        }



        private async void ReadDataStream(object sender, DoWorkEventArgs e)
        {
            while (!_isFinished)
            {

            }
        }



        private async void InitMediaCapture()
        {
            try
            {
                if (mediaCapture != null)
                {
                    // Cleanup MediaCapture object
                    if (isPreviewing)
                    {
                        await mediaCapture.StopPreviewAsync();
                        isPreviewing = false;
                    }
                    if (isRecording)
                    {
                        await mediaCapture.StopRecordAsync();
                        isRecording = false;
                    }
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

                // "Initializing camera to capture audio and video...";
                // Use default initialization
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                // Set callbacks for failure and recording limit exceeded
                // "Device successfully initialized for video recording!";
                mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);
                mediaCapture.RecordLimitationExceeded += new Windows.Media.Capture.RecordLimitationExceededEventHandler(mediaCapture_RecordLimitExceeded);

                // Start Preview                
                //                previewElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
                // "Camera preview succeeded";

            }
            catch (Exception ex)
            {
                // "Unable to initialize camera for audio/video mode: " + ex.Message;
            }
        }

        private async void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    // "MediaCaptureFailed: " + currentFailure.Message;

                    if (isRecording)
                    {
                        await mediaCapture.StopRecordAsync();
                        // "\n Recording Stopped";
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    // "\nCheck if camera is diconnected. Try re-launching the app";
                }
            });
        }

        public async void mediaCapture_RecordLimitExceeded(Windows.Media.Capture.MediaCapture currentCaptureObject)
        {
            try
            {
                if (isRecording)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        try
                        {
                            // "Stopping Record on exceeding max record duration";
                            await mediaCapture.StopRecordAsync();
                            isRecording = false;
                            if (mediaCapture.MediaCaptureSettings.StreamingCaptureMode == StreamingCaptureMode.Audio)
                            {
                                // "Stopped record on exceeding max record duration: " + audioFile.Path;
                            }
                            else
                            {
                                // "Stopped record on exceeding max record duration: " + recordStorageFile.Path;
                            }
                        }
                        catch (Exception e)
                        {
                            // = e.Message;
                        }
                    });
                }
            }
            catch (Exception e)
            {
                // = e.Message;
            }
        }
    }
}
