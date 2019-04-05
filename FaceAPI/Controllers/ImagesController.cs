namespace FaceAPI.Controllers
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Web.Hosting;
    using System.Web.Mvc;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ImagesController : Controller
    {
        [JsonProperty]
        private readonly JObject imgProperties = new JObject();

        private readonly string[] allowedFormats = { ".jpg", ".jpeg", ".png"};
         
        public ActionResult Index()
        {
            return View();
        }

        // POST images/uploadImage 
        [HttpPost]
        public ActionResult UploadImage(string base64Image, string fileName)
        {
            fileName = fileName.ToLower();  

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

            var faces = ImageUtils.Detect(img, ImageUtils.HaarFace);
            var eyes = ImageUtils.Detect(img, ImageUtils.HaarEye);


            if (faces.Length == 1 && eyes.Length == 2)
            {
                GetImgProperties(faces[0], eyes[0], eyes[1]);

                this.imgProperties.Add("positionScore", ImageUtils.PositionalScore(faces[0]));
                this.imgProperties.Add("brightnessScore", ImageUtils.BrightnessScore(img));
            }
            else if (faces.Length == 1) 
            { 
                GetImgProperties(faces, eyes);

                this.imgProperties.Add("positionScore", ImageUtils.PositionalScore(faces[0]));
                this.imgProperties.Add("brightnessScore", ImageUtils.BrightnessScore(img));
            }
            else
            {
                this.imgProperties.Add("positionScore", 0.0);
                this.imgProperties.Add("brightnessScore", ImageUtils.BrightnessScore(img));
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
            base64Img = base64Img.Replace("data:image/png;base64,", string.Empty);
            base64Img = base64Img.Replace("data:image/jpeg;base64,", string.Empty);

            base64Img = base64Img.Replace('-', '+');
            base64Img = base64Img.Replace('_', '/');

            byte[] imageBytes = Convert.FromBase64String(base64Img);

            var img = (Bitmap)new ImageConverter().ConvertFrom(imageBytes);

            var img2 = new Bitmap(img ?? throw new InvalidOperationException(),
                new Size(250, 300));

            return img2;
        }

        /// <summary>
        /// Applies all transformation and detection methods to given base64Image.
        /// Saves base64Image to 'processed' folder.
        /// </summary>
        /// <param name="image">
        /// The base64Image.
        /// </param>
        /// <param name="faces">
        /// The faces.
        /// </param>
        /// <param name="eyes">
        /// The eyes.
        /// </param>
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
        
        private void WriteToLog(string message)
        {
            string name = DateTime.Today.Day + "-" + DateTime.Today.Month + "-" + DateTime.Today.Year + "-log.txt";

            var filePath = Path.Combine(
                HostingEnvironment.MapPath("~/App_data") ?? throw new InvalidOperationException(), name);

            using (StreamWriter file = new StreamWriter(filePath, true))
            {
                file.WriteLine(DateTime.Now.TimeOfDay + "\t" + message);
                file.Close();
            }
        }
    }
}