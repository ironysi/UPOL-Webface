using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web.Mvc;

using Emgu.CV;
using Emgu.CV.Structure;

using ImageMagick;

namespace WebFace.Controllers
{
    public class UtilsController : Controller
    {
        private readonly MagickImageCollection redHistograms = new MagickImageCollection();
        private readonly MagickImageCollection greenHistograms = new MagickImageCollection();
        private readonly MagickImageCollection blueHistograms = new MagickImageCollection();

        private readonly Dictionary<string, string> photoDict = new Dictionary<string, string>();

        private readonly MagickImageCollection facesCollection = new MagickImageCollection();

        private string rootPath = string.Empty;

        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);

            // now Server has been initialized
            rootPath = Server.MapPath("~/App_Data");
        }

        public ActionResult Histogram()
        {
            DirectoryInfo dir = new DirectoryInfo(rootPath + "/cleaning/clean_data/");
            FileInfo[] files = dir.GetFiles("*.jpg");

            // int i = 0;

            foreach (var file in files)
            {
                var bmp = (Bitmap)Image.FromFile(rootPath + "/cleaning/clean_data/" + file.Name);
                var img2 = new Bitmap(bmp, new Size(250, 300));
                SaveHistograms(img2);

                // i++;
                // if (i == 100)
                //    break;
            }

            this.redHistograms.Evaluate(EvaluateOperator.Mean).Write(rootPath + "/processed/redHistogramMean_positives.png");
            this.greenHistograms.Evaluate(EvaluateOperator.Mean).Write(rootPath + "/processed/greenHistogramMean_positives.png");
            this.blueHistograms.Evaluate(EvaluateOperator.Mean).Write(rootPath + "/processed/blueHistogramMean_positives.png");

            return new HttpStatusCodeResult(200);
        }

        /// <summary>
        /// Takes all files from "App_Data/uploads/" and sorts them into "clean_data" and "dirty_data" based on face detection. 
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult CleanFolder()
        {
            DirectoryInfo dir = new DirectoryInfo(this.rootPath);
            FileInfo[] files = dir.GetFiles("*.jpg");

            // int i = 0;

            foreach (var file in files)
            {
                // store the file inside ~/App_Data/uploads folder
                var filePath = Path.Combine(rootPath + "/uploads", file.Name ?? throw new InvalidOperationException());

                // file.CopyTo(filePath, overwrite:true);

                EvaluateAndSaveImg(filePath, file.Name);

                // i++;
                // if (i == 2000)
                //    break;
            }

            var csv = string.Join(
                Environment.NewLine,
                this.photoDict.Select(x => x.Key + "," + x.Value));

            System.IO.File.WriteAllText(rootPath + "/processed/stats_1000_originalSize.csv", csv);

            return new HttpStatusCodeResult(200);
        }

        public ActionResult SuperPosition()
        {
            DirectoryInfo dir = new DirectoryInfo(Server.MapPath("~/App_Data/cleaning/dirty_data/"));
            FileInfo[] files = dir.GetFiles("*.jpg");

            // int i = 0;

            foreach (var file in files)
            {
                var bmp = (Bitmap)Image.FromFile(Server.MapPath("~/App_Data/cleaning/dirty_data/" + file.Name));
                var img2 = new Bitmap(bmp, new Size(250, 300));

                // faces
                var faces = ImageUtils.Detect(img2, ImageUtils.HaarFace);

                Bitmap clearImg = new Bitmap(250, 300, PixelFormat.Format16bppRgb555);
                Graphics g = Graphics.FromImage(clearImg);

                foreach (var rectangle in faces)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(90, 255, 0, 0)), rectangle);
                }

                g.Save();

                var eyes = ImageUtils.Detect(img2, ImageUtils.HaarEye);

                Graphics g2 = Graphics.FromImage(clearImg);

                foreach (var rectangle in eyes)
                {
                    g2.FillRectangle(new SolidBrush(Color.FromArgb(90, 0, 255, 0)), rectangle);
                }

                g2.Save();

                this.facesCollection.Add(new MagickImage(clearImg));

                // i++;
                // if (i == 100)
                //    break;
            }

            this.facesCollection.Evaluate(EvaluateOperator.Mean).Write(Server.MapPath(
                "~/App_Data/processed/faceAndEyesSuperposition_negative.png"));

            return new HttpStatusCodeResult(200);
        }

        /// <summary>
        /// Method for "cleaning" folder full of images.
        /// Detects images that contain face and saves them to "clean_data" folder,
        /// ones that don't will be saved to "dirty_data"
        /// </summary>
        /// <param name="fullFilePath">
        /// The full file path.
        /// </param>
        /// <param name="fileName">
        /// The file name.
        /// </param>
        private void EvaluateAndSaveImg(string fullFilePath, string fileName)
        {
            var bmp = (Bitmap)Image.FromFile(fullFilePath);

            // var img2 = new Bitmap(bmp, new Size(250, 300));
            var img2 = new Bitmap(bmp);

            var faces = ImageUtils.Detect(img2, this.rootPath + "/HaarCascade/" + ImageUtils.HaarFace);
            var eyes = ImageUtils.Detect(img2, this.rootPath + "/HaarCascade/" + ImageUtils.HaarEye);

            var photoProperties = faces.Length + ", " + eyes.Length;

            // face tilt (angle of vector in between of eyes)
            if (eyes.Length == 2)
            {
                Tuple<int, int> eye1Pos = new Tuple<int, int>(
                    (eyes[0].Top + eyes[0].Bottom) / 2,
                    (eyes[0].Left + eyes[0].Right) / 2);
                Tuple<int, int> eye2Pos = new Tuple<int, int>(
                    (eyes[1].Top + eyes[1].Bottom) / 2,
                    (eyes[1].Left + eyes[1].Right) / 2);

                double angle = Math.Atan2(
                    Math.Abs(eye2Pos.Item1 - eye1Pos.Item1),
                    Math.Abs(eye2Pos.Item2 - eye1Pos.Item2));

                photoProperties = photoProperties + ", " + Math.Round(angle * 100);
            }
            else
            {
                photoProperties = photoProperties + ", NaN";
            }

            if (faces.Length == 1)
            {
                photoProperties = photoProperties + ", " + bmp.Height + ", " + bmp.Width;

                this.photoDict.Add(fileName, photoProperties);
              // img2.Save(rootPath + "/cleaning/clean_data/" + fileName);
            }
            else if (faces.Length < 1)
            {
                photoProperties = photoProperties + ", " + bmp.Height + ", " + bmp.Width;

                this.photoDict.Add(fileName, photoProperties);
               // img2.Save(rootPath + "/cleaning/dirty_data/" + fileName);
            }
            else if (faces.Length > 1)
            {
                photoProperties = photoProperties + ", " + bmp.Height + ", " + bmp.Width;

                this.photoDict.Add(fileName, photoProperties);
               // img2.Save(rootPath + "/cleaning/dirty_data/" + fileName);
            }
        }

        private void SaveHistograms(Bitmap image)
        {
            Image<Gray, byte> img2Blue = new Image<Rgb, byte>(image)[2];
            Image<Gray, byte> img2Green = new Image<Rgb, byte>(image)[1];
            Image<Gray, byte> img2Red = new Image<Rgb, byte>(image)[0];

            var redHistogram = ImageUtils.ApplyHistogram(img2Red, new Pen(Brushes.Red));
            this.redHistograms.Add(new MagickImage(redHistogram));

            // redHistogram.Save(Server.MapPath("~/App_Data/processed/redHist1.jpg"));

            var greenHistogram = ImageUtils.ApplyHistogram(img2Green, new Pen(Brushes.LimeGreen));
            this.greenHistograms.Add(new MagickImage(greenHistogram));

            // greenHistogram.Save(Server.MapPath("~/App_Data/processed/greenHist1.jpg"));

            var blueHistogram = ImageUtils.ApplyHistogram(img2Blue, new Pen(Brushes.Blue));
            this.blueHistograms.Add(new MagickImage(blueHistogram));

            // blueHistogram.Save(Server.MapPath("~/App_Data/processed/blueHist1.jpg"));
        }
    }
}