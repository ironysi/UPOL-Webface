using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

using AForge.Imaging;
using AForge.Imaging.Filters;

using Emgu.CV;
using Emgu.CV.Structure;

namespace WebFace.Controllers
{
    public static class ImageUtils
    {
        public static string HaarFace { get; } = "haarcascade_frontalface_default.xml";

        public static string HaarEye { get; } = "haarCascade_eye.xml";

        /// <summary>
        /// Detects objects in the image.
        /// Objects are detected based on 'haar' file that is passed in.
        /// </summary>
        /// <param name="bmp">
        /// Bitmap of image
        /// </param>
        /// <param name="haarCascadeFile">
        /// The haar cascade file. (Pretrained file)
        /// </param>
        /// <returns>
        /// The <see cref="Rectangle[]"/>.
        /// </returns>
        public static Rectangle[] Detect(Bitmap bmp, string haarCascadeFile)
        {
            Image<Rgb, Byte> x = new Image<Rgb, Byte>(bmp);

            var cascadeClassifier = new CascadeClassifier(haarCascadeFile);

            using (var imageFrame = x)
            {
                if (imageFrame != null)
                {
                    var grayframe = imageFrame.Convert<Gray, Byte>();
                    var detectedObject = cascadeClassifier.DetectMultiScale(grayframe, 1.1, 10,
                        Size.Empty); // the actual face detection happens here
                    return detectedObject;
                }
            }
            return new Rectangle[0];
        }

        public static Bitmap ApplyHistogram(Image<Gray, byte> imgInput, Pen pen)
        {
            DenseHistogram hist = new DenseHistogram(256, new RangeF(0.0f, 255f));
            hist.Calculate(new Image<Gray, byte>[] { imgInput }, true, null);

            // Get the max value of histogram
            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MinMaxLoc(hist, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

            // Scale histogram
            int width = imgInput.Size.Width;
            int height = imgInput.Size.Height;
            var histData = hist.GetBinValues();

            Bitmap histo = DrawHistogram(maxVal, width, height, histData, pen);
            return histo;
        }

        private static Bitmap DrawHistogram(double maxVal, int width, int height, float[] histData, Pen pen)
        {
            Bitmap histo = new Bitmap(width, height, PixelFormat.Format16bppRgb555);
            Graphics g = Graphics.FromImage(histo);
            g.Clear(SystemColors.Window);

            for (var i = 0; i < histData.GetLength(0); i++)
            {
                var val = (float)histData.GetValue(i);
                val = (float)(val * (maxVal != 0 ? height / maxVal : 0.0));

                Point s = new Point(i, height);
                Point e = new Point(i, height - (int)val);

                g.DrawLine(pen, s, e);
            }

            return histo;
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

        public static Bitmap AutoCrop(Bitmap selectedImage)
        {
            Bitmap autoCropImage = null;
            try
            {
                autoCropImage = selectedImage;

                // create gray scale filter (BT709)
                Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
                Bitmap grayImage = filter.Apply(autoCropImage);

                // create instance of skew checker
                DocumentSkewChecker skewChecker = new DocumentSkewChecker();

                // get documents skew angle
                double angle = 0; // skewChecker.GetSkewAngle(grayImage);
                                  // create rotation filter
                RotateBilinear rotationFilter = new RotateBilinear(-angle);
                rotationFilter.FillColor = Color.White;

                // rotate image applying the filter
                Bitmap rotatedImage = rotationFilter.Apply(grayImage);
                new ContrastStretch().ApplyInPlace(rotatedImage);
                new Threshold(25).ApplyInPlace(rotatedImage);
                BlobCounter bc = new BlobCounter();
                bc.FilterBlobs = true;

                bc.ProcessImage(rotatedImage);
                Rectangle[] rects = bc.GetObjectsRectangles();

                if (rects.Length == 0)
                {
                    // CAN'T CROP
                }
                else if (rects.Length == 1)
                {
                    autoCropImage = autoCropImage.Clone(rects[0], autoCropImage.PixelFormat);
                }
                else if (rects.Length > 1)
                {
                    // get largets rect
                    Console.WriteLine("Using largest rectangle found in image ");
                    var r2 = rects.OrderByDescending(r => r.Height * r.Width).ToList();
                    autoCropImage = autoCropImage.Clone(r2[0], autoCropImage.PixelFormat);
                }
                else
                {
                    Console.WriteLine("Huh? on image ");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return autoCropImage;
        }
    }
}