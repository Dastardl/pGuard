using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;

namespace Core
{
    public class PreviewFrameProcessedEventArgs<T> : EventArgs
    {
        public T Results { get; set; }
        public VideoFrame Frame { get; set; }

        public PreviewFrameProcessedEventArgs() { }

        public PreviewFrameProcessedEventArgs(T processingResults, VideoFrame frame)
        {
            Results = processingResults;
            Frame = frame;
        }
    }
}
