using System;
using System.Drawing;
using System.IO;
using System.Web;
using System.Web.Mvc;

using Emgu.CV;
using Emgu.CV.Structure;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebFace.Controllers
{
    using System.Collections.Generic;
    using System.Linq;

    public class HomeController : Controller
    {
        private readonly JObject imgProperties = new JObject();

        private readonly Dictionary<string, string> photoDict = new Dictionary<string, string>();

        private const string HaarFace = "haarcascade_frontalface_default.xml";
        private const string HaarEye = "haarCascade_eye.xml";


        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Takes all files from "App_Data/uploads/" and sorts them into "clean_data" and "dirty_data" based on face detection. 
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult CleanFolder()
        {
            DirectoryInfo dir = new DirectoryInfo(Server.MapPath("~/App_Data/"));
            FileInfo[] files = dir.GetFiles("*.jpg");

            foreach (var file in files)
            {
                // extract only the filename
                var fileName = Path.GetFileName(file.Name);

                // store the file inside ~/App_Data/uploads folder
                var filePath = Path.Combine(
                    Server.MapPath("~/App_Data/uploads"),
                    fileName ?? throw new InvalidOperationException());

                // file.CopyTo(filePath, overwrite:true);

                EvaluateAndSaveImg(filePath, fileName);
            }

            var csv = string.Join(
                Environment.NewLine,
                this.photoDict.Select(x => x.Key + "," + x.Value));

            System.IO.File.WriteAllText(Server.MapPath("~/App_Data/stats.csv"), csv);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// The upload and process fille.
        /// </summary>
        /// <param name="file">
        /// The file.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult UploadAndProcess(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                // extract only the filename
                var fileName = Path.GetFileName(file.FileName);

                // store the file inside ~/App_Data/uploads folder
                var filePath = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName ?? throw new InvalidOperationException());

                file.SaveAs(filePath);
                // ReSharper disable once ArrangeThisQualifier
                Convert(fileName);
            }
            else
            {
                Console.WriteLine("Upload error!");
            }

            // redirect back to the index action to show the form once again
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Applies all transformation and detection methods to given image.
        /// Saves image to 'processed' folder.
        /// </summary>
        /// <param name="fileName">
        /// The image file name.
        /// </param>
        private void Convert(string fileName)
        {
            var bmp = (Bitmap)Image.FromFile(Server.MapPath("~/App_Data/uploads/" + fileName));

            // add original img size to imgProperties json
            // ReSharper disable once ArrangeThisQualifier
            this.imgProperties.Add("OriginalImageSize", bmp.Size.ToString());

            var img2 = new Bitmap(bmp, new Size(250, 300));
            this.imgProperties.Add("ProcessedImageSize", img2.Size.ToString());

            // Create and save histograms of the image
            SaveHistograms(img2);

            // Crop is not working very well...
            // img2 = ImageUtils.Crop(img2);

            // face detection
            var faces = Detect(img2, HaarFace);
          
            Graphics g = Graphics.FromImage(img2);

            foreach (var rectangle in faces)
            {
                g.DrawRectangle(new Pen(Color.FromArgb(80, 255, 0, 0), (float)3.8), rectangle);
            }

            g.Save();
       
            // Eye detection
            var eyes = Detect(img2, HaarEye);

            Graphics g2 = Graphics.FromImage(img2);

            foreach (var rectangle in eyes)
            {
                g2.FillRectangle(new SolidBrush(Color.FromArgb(50, 0, 255, 0)), rectangle);
            }

            g2.Save();

            if (faces.Length == 1 && eyes.Length == 2)
            {
                Console.WriteLine("Picture was processed and saved.");
                img2.Save(Server.MapPath("~/App_Data/processed/" + fileName));

                // get img properties
                GetImgProperties(faces[0], eyes[0], eyes[1]);
                
                // save json file
                SaveImgProperties(fileName);
            }
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

            var img2 = new Bitmap(bmp, new Size(250, 300));

            var faces = Detect(img2, HaarFace);
            var eyes = Detect(img2, HaarEye);

            var photoProperties = eyes.Length.ToString();

            if (faces.Length == 1)
            {
                photoProperties = photoProperties + ", " + bmp.Height + ", " + bmp.Width + ", 1";

                this.photoDict.Add(fileName, photoProperties);
                img2.Save(Server.MapPath("~/App_Data/cleaning/clean_data/" + fileName));
            }
            else if (faces.Length < 1)
            {
                photoProperties = photoProperties + ", " + bmp.Height + ", " + bmp.Width + ", " + faces.Length;

                this.photoDict.Add(fileName, photoProperties);
                img2.Save(Server.MapPath("~/App_Data/cleaning/dirty_data/" + fileName));
            }
            else if (faces.Length > 1)
            {
                photoProperties = photoProperties + ", " + bmp.Height + ", " + bmp.Width + ", " + faces.Length;

                this.photoDict.Add(fileName, photoProperties);
                img2.Save(Server.MapPath("~/App_Data/cleaning/dirty_data/" + fileName));
            }
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
        private Rectangle[] Detect(Bitmap bmp, string haarCascadeFile)
        {
            Image<Rgb, Byte> x = new Image<Rgb, Byte>(bmp); 

            var cascadeClassifier =
                new CascadeClassifier(Server.MapPath("~/App_Data/HaarCascade/" + haarCascadeFile));

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

        /// <summary>
        /// Fills json object with properties of image.
        /// </summary>
        /// <param name="face">
        /// The face.
        /// </param>
        /// <param name="eye1">
        /// The eye 1.
        /// </param>
        /// <param name="eye2">
        /// The eye 2.
        /// </param>
        private void GetImgProperties(Rectangle face, Rectangle eye1, Rectangle eye2)
        {
                // get face position
                this.imgProperties.Add("faceTop", face.Top);
                this.imgProperties.Add("faceBottom", face.Bottom);
                this.imgProperties.Add("faceLeft", face.Left);
                this.imgProperties.Add("faceRight", face.Right);
                this.imgProperties.Add("faceWidth", face.Width);
                this.imgProperties.Add("faceHeight", face.Height);

                // get eye positions
                this.imgProperties.Add("eye_1_horizontal", (eye1.Top + eye1.Bottom) / 2);
                this.imgProperties.Add("eye_1_vertical", (eye1.Left + eye1.Right) / 2);
                this.imgProperties.Add("eye_2_horizontal", (eye2.Top + eye2.Bottom) / 2);
                this.imgProperties.Add("eye_2_vertical", (eye2.Left + eye2.Right) / 2);
                this.imgProperties.Add("eyeTilt", ((eye1.Top + eye1.Bottom) / 2) - ((eye2.Top + eye2.Bottom) / 2));
        }

        /// <summary>
        /// Saves properties of image to the 'filename'.json file.
        /// </summary>
        /// <param name="fileName">
        /// Filename of the picture.
        /// </param>
        private void SaveImgProperties(string fileName)
        {
            string json = JsonConvert.SerializeObject(this.imgProperties, Formatting.Indented);

            System.IO.File.WriteAllText(Server.MapPath("~/App_Data/processed/properties_" + 
                                                       fileName.Substring(0, fileName.Length - 4) + ".json"), json);
        }

        private void SaveHistograms(Bitmap image)
        {
            Image<Gray, Byte> img2Blue = new Image<Rgb, byte>(image)[2];
            Image<Gray, Byte> img2Green = new Image<Rgb, byte>(image)[1];
            Image<Gray, Byte> img2Red = new Image<Rgb, byte>(image)[0];

            var redHistogram = ImageUtils.ApplyHistogram(img2Red, new Pen(Brushes.Red));
            redHistogram.Save(Server.MapPath("~/App_Data/processed/redHist.jpg"));
            var greenHistogram = ImageUtils.ApplyHistogram(img2Green, new Pen(Brushes.LimeGreen));
            greenHistogram.Save(Server.MapPath("~/App_Data/processed/greenHist.jpg"));
            var blueHistogram = ImageUtils.ApplyHistogram(img2Blue, new Pen(Brushes.Blue));
            blueHistogram.Save(Server.MapPath("~/App_Data/processed/blueHist.jpg"));
        }
    }
}
