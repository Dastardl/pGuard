using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Collections.Generic;
using System.Threading;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.MediaProperties;
using Windows.UI;

namespace Core
{
    public class FacialDrawingHandler
    {
        SynchronizationContext syncContext;
        IReadOnlyList<BitmapBounds> latestFaceLocations;
        Size videoSize;
        CanvasControl drawCanvas;
        Color strokeColour;

        const double INFLATION_FACTOR = 1.5d;

        public FacialDrawingHandler(CanvasControl drawCanvas, VideoEncodingProperties videoEncodingProperties, Color strokeColour)
        {
            this.strokeColour = strokeColour;
            videoSize = new Size(videoEncodingProperties.Width, videoEncodingProperties.Height);
            this.drawCanvas = drawCanvas;
            this.drawCanvas.Draw += OnDraw;
            syncContext = SynchronizationContext.Current;
        }

        void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var faces = latestFaceLocations;

            if (faces != null)
            {
                foreach (var face in faces)
                {
                    var scaledBox = ScaleVideoBitmapBoundsToDrawCanvasRect(face);

                    args.DrawingSession.DrawRectangle(scaledBox, strokeColour);
                }
            }
        }

        Rect ScaleVideoBitmapBoundsToDrawCanvasRect(BitmapBounds bounds)
        {
            Rect rect = new Rect(
              (((float)bounds.X / videoSize.Width) * drawCanvas.ActualWidth),
              (((float)bounds.Y / videoSize.Height) * drawCanvas.ActualHeight),
              (((float)bounds.Width) / videoSize.Width * drawCanvas.ActualWidth),
              (((float)bounds.Height / videoSize.Height) * drawCanvas.ActualHeight)
            );

            rect = rect.Inflate(new Rect(0, 0, drawCanvas.ActualWidth, drawCanvas.ActualHeight), INFLATION_FACTOR);

            return (rect);
        }

        public void SetLatestFrameReceived(IReadOnlyList<BitmapBounds> faceLocations)
        {
            latestFaceLocations = faceLocations;

            syncContext.Post(_ =>
            {
                drawCanvas.Invalidate();
            }, null);
        }


    }
}