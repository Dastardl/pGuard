using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml.Controls;

namespace Core
{
    public class CameraPreviewManager
    {
        public MediaCapture MediaCapture
        {
            get
            {
                return (mediaCapture);
            }
        }
        public VideoEncodingProperties VideoProperties
        {
            get
            {
                return (mediaCapture.VideoDeviceController.GetMediaStreamProperties(
                  MediaStreamType.VideoPreview) as VideoEncodingProperties);
            }
        }
        CaptureElement captureElement;
        MediaCapture mediaCapture;

        public CameraPreviewManager(CaptureElement captureElement)
        {
            this.captureElement = captureElement;
        }

        public async Task<VideoEncodingProperties> StartPreviewToCaptureElementAsync(Func<DeviceInformation, bool> deviceFilter)
        {
            var preferredCamera = await GetFilteredCameraOrDefaultAsync(deviceFilter);

            MediaCaptureInitializationSettings initialisationSettings = new MediaCaptureInitializationSettings()
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                VideoDeviceId = preferredCamera.Id
            };
            mediaCapture = new MediaCapture();

            await mediaCapture.InitializeAsync(initialisationSettings);

            captureElement.Source = this.mediaCapture;

            await mediaCapture.StartPreviewAsync();

            return (mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties);
        }

        public async Task StopPreviewAsync()
        {
            await this.mediaCapture.StopPreviewAsync();
            this.captureElement.Source = null;
        }

        async Task<DeviceInformation> GetFilteredCameraOrDefaultAsync(Func<DeviceInformation, bool> deviceFilter)
        {
            var videoCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation selectedCamera = null;
            try
            {
                selectedCamera = videoCaptureDevices.SingleOrDefault(deviceFilter);
            }
            catch { }

            if (selectedCamera == null)
            {
                // we fall back to the first camera that we can find.
                selectedCamera = videoCaptureDevices.FirstOrDefault();
            }
            return (selectedCamera);
        }
    }
}