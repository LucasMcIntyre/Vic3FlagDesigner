using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Vic3FlagDesigner
{
    public static class ImageColorProcessor
    {
        public static BitmapImage ApplyColorReplacement(BitmapImage originalImage, Color color1, Color color2, Color color3, Color selectedColor1, Color selectedColor2, Color selectedColor3)
        {
            if (originalImage == null) return originalImage;

            try
            {
                var convertedBitmap = new FormatConvertedBitmap(originalImage, PixelFormats.Bgra32, null, 0);
                WriteableBitmap modifiedBitmap = new WriteableBitmap(convertedBitmap);
                int stride = modifiedBitmap.PixelWidth * 4;
                byte[] pixelData = new byte[modifiedBitmap.PixelHeight * stride];
                modifiedBitmap.CopyPixels(pixelData, stride, 0);

                ReplaceColorsInPixelData(pixelData, modifiedBitmap.PixelWidth, modifiedBitmap.PixelHeight, stride, color1, color2, color3, selectedColor1, selectedColor2, selectedColor3);
                modifiedBitmap.WritePixels(new Int32Rect(0, 0, modifiedBitmap.PixelWidth, modifiedBitmap.PixelHeight), pixelData, stride, 0);

                return BitmapFromWriteableBitmap(modifiedBitmap);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying color replacement: {ex.Message}");
                return originalImage;
            }
        }

        private static BitmapImage BitmapFromWriteableBitmap(WriteableBitmap writeableBitmap)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                    encoder.Save(stream);

                    // Convert the stream to a byte array, then create a new MemoryStream.
                    byte[] imageData = stream.ToArray();

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    // Remove direct caching options from the original stream and create a new stream from the byte array
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = new MemoryStream(imageData);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting WriteableBitmap to BitmapImage: {ex.Message}");
                return null;
            }
        }

        private static void ReplaceColorsInPixelData(byte[] pixelData, int width, int height, int stride, Color color1, Color color2, Color color3, Color selectedColor1, Color selectedColor2, Color selectedColor3)
        {
            int tolerance = 15;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * stride) + (x * 4);

                    byte b = pixelData[index];
                    byte g = pixelData[index + 1];
                    byte r = pixelData[index + 2];

                    Color originalColor = Color.FromRgb(r, g, b);
                    Color newColor = originalColor;

                    if (IsColorClose(originalColor, selectedColor1, tolerance))
                    {
                        newColor = color1;
                    }
                    else if (IsColorClose(originalColor, selectedColor2, tolerance))
                    {
                        newColor = color2;
                    }
                    else if (IsColorClose(originalColor, selectedColor3, tolerance))
                    {
                        newColor = color3;
                    }

                    pixelData[index] = newColor.B;
                    pixelData[index + 1] = newColor.G;
                    pixelData[index + 2] = newColor.R;
                }
            }
        }

        private static bool IsColorClose(Color actual, Color target, int tolerance)
        {
            return Math.Abs(actual.R - target.R) <= tolerance &&
                   Math.Abs(actual.G - target.G) <= tolerance &&
                   Math.Abs(actual.B - target.B) <= tolerance;
        }
    }
}