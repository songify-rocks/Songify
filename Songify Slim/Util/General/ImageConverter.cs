using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Songify_Slim.Util.General
{
    public static class ImageConverter
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon ConvertBitmapImageToIcon(BitmapImage bitmapImage)
        {
            // Check if bitmapImage is null
            if (bitmapImage == null)
                throw new ArgumentNullException(nameof(bitmapImage));

            // Convert BitmapImage (BitmapSource) to System.Drawing.Bitmap
            Bitmap bitmap;
            using (MemoryStream outStream = new())
            {
                // Use a BitmapEncoder to save the BitmapImage to the stream
                BitmapEncoder encoder = new PngBitmapEncoder(); // or BmpBitmapEncoder
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(outStream);

                // Create a System.Drawing.Bitmap from the stream
                bitmap = new Bitmap(outStream);
            }

            // Get an HICON from the Bitmap
            IntPtr hIcon = bitmap.GetHicon();

            // Create an Icon from the HICON
            Icon icon = Icon.FromHandle(hIcon);

            // Clone the icon to create a managed copy that doesn't rely on the HICON
            Icon finalIcon = (Icon)icon.Clone();

            // Clean up
            icon.Dispose(); // Dispose the original icon
            bitmap.Dispose(); // Dispose the bitmap
            DestroyIcon(hIcon); // Release the HICON

            return finalIcon;
        }
    }
}