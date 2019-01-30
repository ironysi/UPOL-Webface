using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebFace.Controllers
{
    public static class ImageUtils
    {
        public static (Mat, Mat, Mat) Histogram(Image<Rgb, Byte> image)
        {
            DenseHistogram histogram = new DenseHistogram(255, new RangeF(0, 255));

            Image<Gray, Byte> img2Blue = image[2];
            Image<Gray, Byte> img2Green = image[1];
            Image<Gray, Byte> img2Red = image[0];


            histogram.Calculate(new Image<Gray, Byte>[] { img2Blue }, false, null);
            Mat blueHist = new Mat();
            histogram.CopyTo(blueHist);
            histogram.Clear();

            histogram.Calculate(new Image<Gray, Byte>[] { img2Green }, false, null);
            Mat greenHist = new Mat();
            histogram.CopyTo(greenHist);
            histogram.Clear();

            histogram.Calculate(new Image<Gray, Byte>[] { img2Red }, false, null);
            Mat redHist = new Mat();
            histogram.CopyTo(redHist);
            histogram.Clear();

            return (redHist, greenHist, blueHist);
        }

        public static Bitmap Crop(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            Func<int, double> allWhiteRow = row =>
            {
                int isWhite = 0;

                for (int i = 0; i < w; ++i)
                    if (bmp.GetPixel(i, row).R == 255)
                        isWhite++;

                return isWhite / 250.0;
            };

            Func<int, double> allWhiteColumn = col =>
            {
                int isWhite = 0;

                for (int i = 0; i < h; ++i)
                    if (bmp.GetPixel(col, i).R == 255)
                        isWhite++;
                return isWhite / 300.0;
            };

            int topmost = 0;
            for (int row = 0; row < h; ++row)
            {
                double isAllWhite = allWhiteRow(row);
                if (isAllWhite >= 0.7)
                    topmost = row;
                else break;
            }

            int bottommost = 0;
            for (int row = h - 1; row >= 0; --row)
            {
                double isAllWhite = allWhiteRow(row);
                if (isAllWhite >= 0.7)
                    bottommost = row;
                else break;
            }

            int leftmost = 0, rightmost = 0;
            for (int col = 0; col < w; ++col)
            {
                double isAllWhite = allWhiteColumn(col);
                if (isAllWhite >= 0.7)
                    leftmost = col;
                else
                    break;
            }

            for (int col = w - 1; col >= 0; --col)
            {
                double isAllWhite = allWhiteColumn(col);
                if (isAllWhite >= 0.7)
                    rightmost = col;
                else
                    break;
            }

            if (rightmost == 0) rightmost = w; // As reached left
            if (bottommost == 0) bottommost = h; // As reached top.

            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;

            if (croppedWidth == 0) // No border on left or right
            {
                leftmost = 0;
                croppedWidth = w;
            }

            if (croppedHeight == 0) // No border on top or bottom
            {
                topmost = 0;
                croppedHeight = h;
            }

            try
            {
                var target = new Bitmap(croppedWidth, croppedHeight);
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(bmp,
                      new RectangleF(0, 0, croppedWidth, croppedHeight),
                      new RectangleF(leftmost, topmost, croppedWidth, croppedHeight),
                      GraphicsUnit.Pixel);
                }
                return target;
            }
            catch (Exception ex)
            {
                throw new Exception(
                  string.Format("Values are topmost={0} btm={1} left={2} right={3} croppedWidth={4} croppedHeight={5}", topmost, bottommost, leftmost, rightmost, croppedWidth, croppedHeight),
                  ex);
            }
        }

    }
}