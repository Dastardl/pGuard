using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Core
{
    public static class RectExtensions
    {
        public static Rect Inflate(this Rect startRect, Rect containingRect, double inflation)
        {
            double newWidth = startRect.Width * inflation;
            double newHeight = startRect.Height * inflation;

            double newLeft = Math.Max(containingRect.Left, startRect.Left - (newWidth - startRect.Width) / 2.0d);
            double newTop = Math.Max(containingRect.Top, startRect.Top - (newHeight - startRect.Height) / 2.0d);

            return (new Rect(newLeft, newTop, Math.Min(newWidth, (containingRect.Right - newLeft)), Math.Min(newHeight, (containingRect.Bottom - newTop))));
        }
    }
}
