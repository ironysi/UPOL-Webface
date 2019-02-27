namespace FaceAPI.Controllers
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Web.Hosting;

    using Emgu.CV;
    using Emgu.CV.Structure;

    public static class ImageUtils
    {
        public static string HaarFace { get; } =
            HostingEnvironment.MapPath("~/App_Data/haarcascade_frontalface_default.xml");

        public static string HaarEye { get; } = HostingEnvironment.MapPath("~/App_Data/haarcascade_eye1.xml");

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
                var grayframe = imageFrame.Convert<Gray, Byte>();
                var detectedObject = cascadeClassifier.DetectMultiScale(grayframe, 1.1, 10,
                    Size.Empty); // the actual face detection happens here
                return detectedObject;
            }
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
    }
}