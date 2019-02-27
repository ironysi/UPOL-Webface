using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Mvc;

using Emgu.CV;
using Emgu.CV.Structure;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FaceAPI.Controllers
{
    using System.Web.Http;

    public class ImagesController : Controller
    {
        [JsonProperty]
        private readonly JObject imgProperties = new JObject();

        private readonly string[] allowedFormats = { ".jpg", ".jpeg", ".png", ".gif" };

        private readonly double averageUpperX = (49 / 2.5) / 100;
        private readonly double averageUpperY = (67 / 3.0) / 100;
        private readonly double averageLowerX = ((49 + 150) / 2.5) / 100;
        private readonly double averageLowerY = ((67 + 150) / 3.0) / 100;

        public ActionResult Index()
        {
            return View();
        }


        // POST images/uploadImage 
        [System.Web.Mvc.HttpPost]
        public ActionResult UploadImage(string base64Image, string fileName)
        {
            if (string.IsNullOrEmpty(base64Image) || string.IsNullOrEmpty(base64Image))
            {
                WriteToLog("UploadImage() - Image is null or empty.");
                return new HttpStatusCodeResult(400);
            }

            if (!allowedFormats.Contains(Path.GetExtension(fileName)))
            {
                WriteToLog("UploadImage() - File extension not supported. Supported extensions: " 
                           + this.allowedFormats);
                return new HttpStatusCodeResult(415);
            }

            Bitmap img = Base64ToBitmap(base64Image);

            var faces = Detect(img, ImageUtils.HaarFace);
            var eyes = Detect(img, ImageUtils.HaarEye);

            if (faces.Length == 1 && eyes.Length == 2)
            {
                this.imgProperties.Add("imgScore", CalculateScore(faces[0]));
                GetImgProperties(faces[0], eyes[0], eyes[1]);
            }
            else if (faces.Length == 1) 
            {
                this.imgProperties.Add("imgScore", CalculateScore(faces[0]));
                GetImgProperties(faces, eyes);
            }
            else
            {
                this.imgProperties.Add("imgScore", 0.0);
                GetImgProperties(faces, eyes);
            }

            Bitmap image = Base64ToBitmap(base64Image);

            this.DrawObjects(image, faces, eyes);

            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Jpeg);
            byte[] byteImage = ms.ToArray();

            var processedImage = Convert.ToBase64String(byteImage);

            return Json(
                new { properties = JsonConvert.SerializeObject(this.imgProperties), processedImage });
        }

        private Bitmap Base64ToBitmap(string base64Img)
        {
            base64Img = base64Img.Replace("data:image/png;base64,", String.Empty);
            base64Img = base64Img.Replace("data:image/jpeg;base64,", String.Empty);

            base64Img = base64Img.Replace('-', '+');
            base64Img = base64Img.Replace('_', '/');

            Byte[] imageBytes = System.Convert.FromBase64String(base64Img);

            Image img = (Bitmap)new ImageConverter().ConvertFrom(imageBytes);

            var img2 = new Bitmap((Bitmap)img ?? throw new InvalidOperationException(),
                new Size(250, 300));

            return img2;
        }

        private double CalculateScore(Rectangle face)
        {
            var upperXDelta = Math.Abs(((face.X / 2.5) / 100) - this.averageUpperX);
            var upperYDelta = Math.Abs(((face.Y / 3.0) / 100) - this.averageUpperY);
            var lowerXDelta = Math.Abs((((face.X + face.Width) / 2.5) / 100) - this.averageLowerX);
            var lowerYDelta = Math.Abs((((face.Y + face.Height) / 3.0) / 100) - this.averageLowerY);

            var k = (1 - Math.Sqrt(upperXDelta));
            var p = 1 - Math.Sqrt(upperYDelta);
            var l = (1 - Math.Sqrt(lowerXDelta));
            var m = (1 - Math.Sqrt(lowerYDelta));

            var output = (1 - Math.Sqrt(upperXDelta)) * (1 - Math.Sqrt(upperYDelta)) * (1 - Math.Sqrt(lowerXDelta))
                   * (1 - Math.Sqrt(lowerYDelta));
            return output;
        }

        /// <summary>
        /// Applies all transformation and detection methods to given base64Image.
        /// Saves base64Image to 'processed' folder.
        /// </summary>
        /// <param name="image">
        /// The base64Image.
        /// </param>
        private void DrawObjects(Bitmap image)
        {
            // face detection
            var faces = Detect(image, ImageUtils.HaarFace);

            Graphics g = Graphics.FromImage(image);

            foreach (var rectangle in faces)
            {
                g.DrawRectangle(new Pen(Color.FromArgb(80, 255, 0, 0), (float)3.8), rectangle);
            }

            g.Save();

            // Eye detection
            var eyes = Detect(image, ImageUtils.HaarEye);

            Graphics g2 = Graphics.FromImage(image);

            foreach (var rectangle in eyes)
            {
                g2.FillRectangle(new SolidBrush(Color.FromArgb(50, 0, 255, 0)), rectangle);
            }
            g2.Save();
        }

        private void DrawObjects(Bitmap image, Rectangle[] faces, Rectangle[] eyes)
        {
            // face detection
            Graphics g = Graphics.FromImage(image);

            foreach (var rectangle in faces)
            {
                g.DrawRectangle(new Pen(Color.FromArgb(80, 255, 0, 0), (float)3.8), rectangle);
            }

            g.Save();

            // Eye detection
            Graphics g2 = Graphics.FromImage(image);

            foreach (var rectangle in eyes)
            {
                g2.FillRectangle(new SolidBrush(Color.FromArgb(50, 0, 255, 0)), rectangle);
            }

            g2.Save();
        }

        /// <summary>
        /// Fills json object with properties of base64Image.
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
            this.imgProperties.Add("numberFaces", 1);
            this.imgProperties.Add("numberEyes", 2);

            // get face position
            this.imgProperties.Add("face", JToken.FromObject(new { face.X, face.Y }));

            // get eye positions
            this.imgProperties.Add("eye_1", JToken.FromObject(new { eye1.X, eye1.Y }));
            this.imgProperties.Add("eye_2", JToken.FromObject(new { eye2.X, eye2.Y }));

            Tuple<int, int> eye1Pos = new Tuple<int, int>(
                (eye1.Top + eye1.Bottom) / 2,
                (eye1.Left + eye1.Right) / 2);
            Tuple<int, int> eye2Pos = new Tuple<int, int>(
                (eye2.Top + eye2.Bottom) / 2,
                (eye2.Left + eye2.Right) / 2);

            double angle = Math.Atan2(
                Math.Abs(eye2Pos.Item1 - eye1Pos.Item1),
                Math.Abs(eye2Pos.Item2 - eye1Pos.Item2));

            this.imgProperties.Add("eyeTilt", Math.Round(angle * 100));
        }

        private void GetImgProperties(Rectangle[] faces, Rectangle[] eyes)
        {
            this.imgProperties.Add("numberFaces", faces.Length);
            this.imgProperties.Add("numberEyes", eyes.Length);

            for (int i = 0; i < eyes.Length; i++)
            {
                this.imgProperties.Add("eye" + i, JToken.FromObject(new { eyes[i].X, eyes[i].Y }));
            }

            for (int i = 0; i < faces.Length; i++)
            {
                this.imgProperties.Add("face" + i, JToken.FromObject(new { faces[i].X, faces[i].Y }));
            }

        }

        /// <summary>
        /// Detects objects in the base64Image.
        /// Objects are detected based on 'haar' base64Image that is passed in.
        /// </summary>
        /// <param name="bmp">
        /// Bitmap of base64Image
        /// </param>
        /// <param name="haarCascadeFile">
        /// The haar cascade base64Image. (Pretrained base64Image)
        /// </param>
        /// <returns>
        /// The <see>
        ///     <cref>Rectangle[]</cref>
        /// </see>
        /// .
        /// </returns>
        private Rectangle[] Detect(Bitmap bmp, string haarCascadeFile)
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
        
        private void WriteToLog(string message)
        {
            string name = DateTime.Today.Day + "-" + DateTime.Today.Month + "-" + DateTime.Today.Year + "-log.txt";

            try
            {
                var filePath = Path.Combine(
                    HostingEnvironment.MapPath("~/App_data") ?? throw new InvalidOperationException(), name);

                using (StreamWriter file = new StreamWriter(filePath, true))
                {
                    file.WriteLine(DateTime.Now.TimeOfDay + "\t" + message);
                    file.Close();
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}