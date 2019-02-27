namespace UpolWebFaceAPI.Controllers
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Web.Hosting;
    using System.Web.Http;
    using System.Web.Http.Results;

    using Emgu.CV;
    using Emgu.CV.Structure;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using UpolWebFaceAPI.Models;

    public class ImageController : ApiController
    {
        private readonly string haarFace = HostingEnvironment.MapPath("~/App_Data/haarcascade_frontalface_default.xml");

        private readonly string haarEye = HostingEnvironment.MapPath("~/App_Data/haarcascade_eye1.xml");


        [JsonProperty]
        private readonly JObject imgProperties = new JObject();

        private readonly double averageUpperX = (49 / 2.5) / 100;
        private readonly double averageUpperY = (67 / 3.0) / 100;
        private readonly double averageLowerX = ((49 + 150) / 2.5) / 100;
        private readonly double averageLowerY = ((67 + 150) / 3.0) / 100;

        [HttpPost]
        public IHttpActionResult UploadImage([FromBody]ImageModel model)
        {
            var base64Image = model.Base64Image;

            if (string.IsNullOrEmpty(base64Image) || string.IsNullOrEmpty(base64Image))
            {
                this.WriteToLog("UploadImage() - Image is null or empty.");
                return new BadRequestErrorMessageResult("UploadImage() - Image is null or empty.", this);
            }

            Bitmap img = this.Base64ToBitmap(base64Image);

            var faces = this.Detect(img, haarFace);
            var eyes = this.Detect(img, haarEye);

            if (faces.Length == 1 && eyes.Length == 2)
            {
                this.imgProperties.Add("imgScore", this.CalculateScore(faces[0]));
                this.GetImgProperties(faces[0], eyes[0], eyes[1]);
            }
            else
            {
                this.imgProperties.Add("imgScore", 0.0);
                this.GetImgProperties(faces, eyes);
            }

            return this.Json(
                new { properties = JsonConvert.SerializeObject(this.imgProperties) });
        }

        private double CalculateScore(Rectangle face)
        {
            var upperXDelta = Math.Abs(((face.X / 2.5) / 100) - this.averageUpperX);
            var upperYDelta = Math.Abs(((face.Y / 3.0) / 100) - this.averageUpperY);
            var lowerXDelta = Math.Abs((((face.X + face.Width) / 2.5) / 100) - this.averageLowerX);
            var lowerYDelta = Math.Abs((((face.Y + face.Height) / 3.0) / 100) - this.averageLowerY);

            var output = (1 - Math.Sqrt(upperXDelta)) * (1 - Math.Sqrt(upperYDelta)) * (1 - Math.Sqrt(lowerXDelta))
                         * (1 - Math.Sqrt(lowerYDelta));
            return output;
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