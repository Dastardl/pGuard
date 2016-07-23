using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;

namespace Core
{
    public abstract class PreviewFrameProcessor<T>
    {
        public event EventHandler<PreviewFrameProcessedEventArgs<T>> FrameProcessed;
        PreviewFrameProcessedEventArgs<T> eventArgs;
        MediaCapture mediaCapture;
        Rect videoSize;

        public PreviewFrameProcessor(MediaCapture mediaCapture, VideoEncodingProperties videoEncodingProperties)
        {
            this.mediaCapture = mediaCapture;
            videoSize = new Rect(0, 0, videoEncodingProperties.Width, videoEncodingProperties.Height);
            eventArgs = new PreviewFrameProcessedEventArgs<T>();
        }

        public async Task RunFrameProcessingLoopAsync(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                await InitialiseForProcessingLoopAsync();

                VideoFrame frame = new VideoFrame(BitmapFormat, (int)videoSize.Width, (int)videoSize.Height);

                TimeSpan? lastFrameTime = null;

                try
                {
                    while (true)
                    {
                        token.ThrowIfCancellationRequested();

                        await mediaCapture.GetPreviewFrameAsync(frame);

                        if ((!lastFrameTime.HasValue) || (lastFrameTime != frame.RelativeTime))
                        {
                            T results = await ProcessBitmapAsync(frame.SoftwareBitmap);

                            eventArgs.Frame = frame;
                            eventArgs.Results = results;

                            // This is going to fire on our thread here. Up to the caller to 
                            // 'do the right thing' which is a bit risky really.
                            FireFrameProcessedEvent();
                        }
                        lastFrameTime = frame.RelativeTime;
                    }
                }
                finally
                {
                    frame.Dispose();
                }
            },
            token);
        }
        protected abstract Task InitialiseForProcessingLoopAsync();

        protected abstract Task<T> ProcessBitmapAsync(SoftwareBitmap bitmap);

        protected abstract BitmapPixelFormat BitmapFormat
        {
            get;
        }

        void FireFrameProcessedEvent()
        {
            FrameProcessed?.Invoke(this, eventArgs);
        }
        
    }
}
