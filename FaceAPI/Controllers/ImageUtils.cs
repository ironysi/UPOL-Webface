namespace FaceAPI.Controllers
{
    using System;
    using System.Drawing;
    using System.Web.Hosting;

    using Emgu.CV;
    using Emgu.CV.Structure;

    public static class ImageUtils
    {
        public static string HaarFace { get; } =
            HostingEnvironment.MapPath("~/App_Data/haarcascade_frontalface_default.xml");

        public static string HaarEye { get; } = HostingEnvironment.MapPath("~/App_Data/haarcascade_eye1.xml");

        public static float[] AvgHistogram { get;} =
                                                {
                                                    868, 249, 139, 97, 79, 72, 70, 68, 68, 71, 75, 77, 79, 79, 77, 76,
                                                    74, 70, 68, 65, 62, 59, 58, 58, 57, 57, 57, 58, 58, 59, 60, 61, 63,
                                                    65, 67, 70, 73, 76, 79, 82, 85, 89, 93, 99, 105, 113, 121, 130, 140,
                                                    150, 161, 172, 184, 195, 207, 216, 226, 235, 244, 252, 258, 263,
                                                    267, 270, 273, 274, 275, 274, 274, 273, 272, 271, 296, 270, 264,
                                                    261, 258, 254, 251, 247, 245, 242, 239, 236, 233, 230, 228, 224,
                                                    222, 219, 217, 214, 212, 210, 208, 206, 204, 202, 201, 199, 198,
                                                    197, 196, 195, 194, 194, 193, 192, 192, 192, 193, 192, 192, 192,
                                                    191, 191, 191, 191, 191, 192, 193, 194, 195, 196, 198, 199, 201,
                                                    202, 204, 205, 207, 209, 210, 212, 214, 216, 218, 220, 222, 225,
                                                    227, 229, 232, 235, 238, 240, 243, 246, 249, 251, 254, 257, 260,
                                                    263, 266, 269, 273, 276, 279, 282, 286, 289, 291, 295, 298, 301,
                                                    304, 308, 311, 315, 318, 321, 325, 328, 331, 334, 337, 339, 342,
                                                    345, 348, 350, 353, 355, 358, 360, 362, 365, 367, 370, 373, 375,
                                                    377, 379, 379, 380, 381, 383, 383, 383, 382, 384, 386, 387, 387,
                                                    387, 387, 385, 383, 382, 381, 382, 379, 377, 377, 374, 371, 370,
                                                    373, 370, 368, 367, 364, 359, 358, 357, 357, 358, 357, 359, 358,
                                                    358, 358, 355, 354, 355, 356, 359, 359, 359, 360, 365, 371, 544,
                                                    384, 398, 416, 419, 441, 462, 481, 486, 583, 659, 1129, 9076
                                                };

        /// <summary>
        /// Calculates brightness score for the image. Score is calculated by creating brightness histogram and 
        /// looking at how 'wide' is the gap of low intensity pixels at the beginning of histogram.
        /// Fx: If I upload a picture that does not have any dark pixels and therefore histogram of the picture
        /// begins at value 35 (meaning that values 0-34 are represented less then 10 times each) out of 256 then Brightness
        /// score for this image will be 34 / 256 = 0.1328125.
        /// </summary>
        /// <param name="image">Bitmap of image.</param>
        /// <returns>Brightness score between 0 and 1. Where 0 is worst and 1 is best.</returns>
        public static double BrightnessScore(Bitmap image)
        {
            float[] brightnessHist = new float[256];

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var r = image.GetPixel(x, y).R;
                    var g = image.GetPixel(x, y).G;
                    var b = image.GetPixel(x, y).B;
                    var br = (int)Math.Round((r * 0.2126) + (g * 0.7152) + (b * 0.0722), 0);

                    brightnessHist[br]++;
                }
            }

            var counter = 0;

            for (var i = 0; i < brightnessHist.Length; i++)
            {
                if (brightnessHist[i] < 10.0) 
                {
                    counter++;
                }
                else
                {
                    break;
                }
            }

            return 1 - ((double)counter / brightnessHist.Length);
        }

        /// <summary>
        /// Calculates positional score of face in the image. Score is calculated based on average position of face in our archived images. Scores above 0.1 are more less acceptable. This conclusion was made based on statistical analysis.
        /// </summary>
        /// <param name="face">Rectangle structure containing the face</param>
        /// <returns>Score between 0 and 1. Where 0 is lowest and 1 is perfect</returns>
        public static double PositionalScore(Rectangle face)
        {
            double averageUpperX = (49 / 2.5) / 100;
            double averageUpperY = (67 / 3.0) / 100;
            double averageLowerX = ((49 + 150) / 2.5) / 100;
            double averageLowerY = ((67 + 150) / 3.0) / 100;

            var upperXDelta = Math.Abs(((face.X / 2.5) / 100) - averageUpperX);
            var upperYDelta = Math.Abs(((face.Y / 3.0) / 100) - averageUpperY);
            var lowerXDelta = Math.Abs((((face.X + face.Width) / 2.5) / 100) - averageLowerX);
            var lowerYDelta = Math.Abs((((face.Y + face.Height) / 3.0) / 100) - averageLowerY);

            var output = (1 - Math.Sqrt(upperXDelta)) * (1 - Math.Sqrt(upperYDelta)) * (1 - Math.Sqrt(lowerXDelta))
                         * (1 - Math.Sqrt(lowerYDelta));
            return output;
        }

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
    }
}