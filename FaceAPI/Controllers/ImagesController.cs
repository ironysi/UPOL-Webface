using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Mvc;

using Emgu.CV;
using Emgu.CV.Structure;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FaceAPI.Controllers
{
    public class ImagesController : Controller
    {
        [JsonProperty]
        private readonly JObject imgProperties = new JObject();

        private string haarFace = @"App_Data/haarcascade_frontalface_default.xml";
        private string haarEye = @"App_Data/haarcascade_eye.xml";
        private string rootPath = string.Empty;
     
        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);

            // now Server has been initialized
            this.rootPath = Server.MapPath("~/App_Data");
            this.haarFace = Server.MapPath("~/App_Data/haarcascade_frontalface_default.xml");
            this.haarEye = Server.MapPath("~/App_Data/haarcascade_eye1.xml");
        }

        public ActionResult Index()
        {
            return View();
        }


        // POST api/images/uploadImage 
        [HttpPost]
        public ActionResult UploadImage(string image, string fileName)
        {
            var filePath = Path.Combine(this.rootPath, "Uploads", fileName);
            bool isOk = false;
            string[] validExtensions = { ".jpg", ".png" };

            if (string.IsNullOrEmpty(image))
                return new HttpStatusCodeResult(400);

            if (!validExtensions.Contains(Path.GetExtension(filePath)))
                return new HttpStatusCodeResult(415);

            image = image.Replace("data:image/png;base64,", String.Empty);
            image = image.Replace("data:image/jpeg;base64,", String.Empty);

            image = image.Replace('-', '+');
            image = image.Replace('_', '/');

            Byte[] imageBytes = System.Convert.FromBase64String(image);

            Image img = (Bitmap)new ImageConverter().ConvertFrom(imageBytes);

            var img2 = new Bitmap((Bitmap)img ?? throw new InvalidOperationException(),
                new Size(250, 300));

            // '?' is checking for null
            img2.Save(filePath);

            var faces = Detect(img2, this.haarFace);
            var eyes = Detect(img2, this.haarEye);

            if (faces.Length == 1 && eyes.Length == 2)
                GetImgProperties(faces[0], eyes[0], eyes[1]);
            else
                GetImgProperties(faces, eyes);

           return Json(JsonConvert.SerializeObject(this.imgProperties));
        }

        [HttpGet]
        public ActionResult GetImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return new HttpStatusCodeResult(400);

            if (IsFileLocked(new FileInfo(Path.Combine(this.rootPath, "Uploads", fileName))))
                return new HttpStatusCodeResult(500);

            Convert(fileName);

            var path = Server.MapPath(Path.Combine("/App_data/Processed", fileName));

            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] data = new byte[(int)fileStream.Length];

            fileStream.Read(data, 0, data.Length);


            return Json(new { base64image = System.Convert.ToBase64String(data) }, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// Applies all transformation and detection methods to given image.
        /// Saves image to 'processed' folder.
        /// </summary>
        /// <param name="fileName">
        /// The image image name.
        /// </param>
        private void Convert(string fileName)
        {
            var bmp = (Bitmap)Image.FromFile(this.rootPath + "/uploads/" + fileName);

            this.imgProperties.Add("OriginalImageSize", bmp.Size.ToString());

            var img2 = new Bitmap(bmp, new Size(250, 300));
            this.imgProperties.Add("ProcessedImageSize", img2.Size.ToString());

            // face detection
            var faces = Detect(img2, haarFace);

            Graphics g = Graphics.FromImage(img2);

            foreach (var rectangle in faces)
            {
                g.DrawRectangle(new Pen(Color.FromArgb(80, 255, 0, 0), (float)3.8), rectangle);
            }

            g.Save();

            // Eye detection
            var eyes = Detect(img2, haarEye);

            Graphics g2 = Graphics.FromImage(img2);

            foreach (var rectangle in eyes)
            {
                g2.FillRectangle(new SolidBrush(Color.FromArgb(50, 0, 255, 0)), rectangle);
            }

            g2.Save();

            Console.WriteLine("Picture was processed and saved.");
            img2.Save(this.rootPath + "/processed/" + fileName);

            // get img properties
            if(faces.Length == 1 && eyes.Length == 2)
                GetImgProperties(faces[0], eyes[0], eyes[1]);
            else
                GetImgProperties(faces, eyes);

            // save json image
            //SaveImgProperties(fileName);
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
        /// Saves properties of image to the 'filename'.json image.
        /// </summary>
        /// <param name="fileName">
        /// Filename of the picture.
        /// </param>
        private void SaveImgProperties(string fileName)
        {
            string json = JsonConvert.SerializeObject(this.imgProperties, Formatting.Indented);

            System.IO.File.WriteAllText(this.rootPath + "/processed/properties_" +
                                                       fileName.Substring(0, fileName.Length - 4) + ".json", json);
        }

        /// <summary>
        /// Detects objects in the image.
        /// Objects are detected based on 'haar' image that is passed in.
        /// </summary>
        /// <param name="bmp">
        /// Bitmap of image
        /// </param>
        /// <param name="haarCascadeFile">
        /// The haar cascade image. (Pretrained image)
        /// </param>
        /// <returns>
        /// The <see cref="Rectangle[]"/>.
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

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}