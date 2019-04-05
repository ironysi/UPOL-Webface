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

    [RoutePrefix("api/image")]
    public class ImageController : ApiController
    {
        private readonly string haarFace = HostingEnvironment.MapPath("~/App_Data/haarcascade_frontalface_default.xml");

        private readonly string haarEye = HostingEnvironment.MapPath("~/App_Data/haarcascade_eye1.xml");

        [JsonProperty]
        private readonly JObject imgProperties = new JObject();

        private const double AverageUpperX = (49 / 2.5) / 100;
        private const double AverageUpperY = (67 / 3.0) / 100;
        private const double AverageLowerX = ((49 + 150) / 2.5) / 100;
        private const double AverageLowerY = ((67 + 150) / 3.0) / 100;

        [HttpPost]
        [Route("")]
        public IHttpActionResult GetScore([FromBody]ImageModel model)
        {
            var base64Image = model.Base64Image;

            if (string.IsNullOrEmpty(base64Image) || string.IsNullOrEmpty(base64Image))
            {
                this.WriteToLog("UploadImage() - Image is null or empty.");
                return new BadRequestErrorMessageResult("UploadImage() - Image is null or empty.", this);
            }

            Bitmap img = this.Base64ToBitmap(base64Image);

            var faces = new Rectangle[0];
            var positionalScore = 0.0;
            var brightnessScore = 0.0;

            try
            {
                faces = this.Detect(img, haarFace);
            }
            catch
            {
                WriteToLog("(UploadImage() - Error occured during face and eye detection. )");
            }          

            if (faces.Length == 1)
            {
                positionalScore = PositionalScore(faces[0]);
                brightnessScore = BrightnessScore(img);
            }

            return this.Json(new { positionalScore, brightnessScore });
        }

        [HttpPost]
        [Route("getDetailedScore")]
        public IHttpActionResult GetDetailedScore([FromBody] ImageModel model)
        {
            var base64Image = model.Base64Image;

            if (string.IsNullOrEmpty(base64Image) || string.IsNullOrEmpty(base64Image))
            {
                this.WriteToLog("GetDetailedScore() - Image is null or empty.");
                return new BadRequestErrorMessageResult("GetDetailedScore() - Image is null or empty.", this);
            }

            var img = this.Base64ToBitmap(base64Image);  

            Rectangle[] faces;
            Rectangle[] eyes;

            try
            {
                faces = this.Detect(img, haarFace);
                eyes = this.Detect(img, haarEye);
            }
            catch
            {
                WriteToLog("GetDetailedScore() - Error occured during face and eye detection.");
                return new BadRequestErrorMessageResult("GetDetailedScore() - Could not find HaarCascade data files. Please make sure that they are in ~/App_Data/ folder with required permissions.", this);
            }

            if (faces.Length == 1 && eyes.Length == 2)
            {
                this.imgProperties.Add("positionalScore", this.PositionalScore(faces[0]));
                this.imgProperties.Add("brightnessScore", this.BrightnessScore(img));
                this.GetImgProperties(faces[0], eyes[0], eyes[1]);
            }
            else if (faces.Length == 1)
            {
                this.imgProperties.Add("positionalScore", this.PositionalScore(faces[0]));
                this.imgProperties.Add("brightnessScore", this.BrightnessScore(img));
                this.GetImgProperties(faces, eyes);
            }
            else
            {
                this.imgProperties.Add("positionalScore", 0.0);
                this.imgProperties.Add("brightnessScore", this.BrightnessScore(img));
                this.GetImgProperties(faces, eyes);
            }

            return this.Json(new { properties = JsonConvert.SerializeObject(this.imgProperties) });
        }

        /// <summary>
        /// Calculates positional score of face in the image. Score is calculated based on average position of face in our archived images. Scores above 0.1 are more less acceptable. This conclusion was made based on statistical analysis.
        /// </summary>
        /// <param name="face">Rectangle structure containing the face</param>
        /// <returns>Score between 0 and 1 where 0 is lowest and 1 is perfect</returns>
        private double PositionalScore(Rectangle face)
        {
            var upperXDelta = Math.Abs(((face.X / 2.5) / 100) - AverageUpperX);
            var upperYDelta = Math.Abs(((face.Y / 3.0) / 100) - AverageUpperY);
            var lowerXDelta = Math.Abs((((face.X + face.Width) / 2.5) / 100) - AverageLowerX);
            var lowerYDelta = Math.Abs((((face.Y + face.Height) / 3.0) / 100) - AverageLowerY);

            var output = (1 - Math.Sqrt(upperXDelta)) * (1 - Math.Sqrt(upperYDelta)) * (1 - Math.Sqrt(lowerXDelta))
                         * (1 - Math.Sqrt(lowerYDelta));
            return output;
        }

        private Bitmap Base64ToBitmap(string base64Img)
        {
            base64Img = base64Img.Replace("data:image/png;base64,", string.Empty);
            base64Img = base64Img.Replace("data:image/jpeg;base64,", string.Empty);

            base64Img = base64Img.Replace('-', '+');
            base64Img = base64Img.Replace('_', '/');

            Byte[] imageBytes = Convert.FromBase64String(base64Img);

            var img = (Bitmap)new ImageConverter().ConvertFrom(imageBytes);

            var img2 = new Bitmap(img ?? throw new InvalidOperationException(),
                new Size(250, 300));

            return img2;
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

        /// <summary>
        /// Calculates brightness score for the image. Score is calculated by creating brightness histogram and 
        /// looking at how 'wide' is the gap of low intensity pixels at the beginning of histogram.
        /// Fx: If I upload a picture that does not have any dark pixels and therefore histogram of the picture
        /// begins at value 35 (meaning that values 0-34 are represented less then 10 times each) out of 256 then Brightness
        /// score for this image will be: 1 - (34 / 256) = 0.8671875
        /// </summary>
        /// <param name="image">Bitmap of image.</param>
        /// <returns>Brightness score between 0 and 1 where 0 is worst and 1 is best</returns>
        private double BrightnessScore(Bitmap image)
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

            return (double)counter / brightnessHist.Length;
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
    }
}