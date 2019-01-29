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

    }
}