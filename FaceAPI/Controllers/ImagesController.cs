﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Emgu.CV;
using Emgu.CV.Structure;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FaceAPI.Controllers
{
    public class ImagesController : Controller
    {
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

        // POST api/images/uploadImage 
        [HttpPost]
        public ActionResult UploadImage(HttpPostedFileBase file)
        {
            var filePath = Path.Combine(this.rootPath, "Uploads", file.FileName);
            bool isOk = false;
            string[] validExtensions = { ".jpg", ".tif", ".png", ".gif" };

            if (!validExtensions.Contains(Path.GetExtension(filePath)))
                return new HttpStatusCodeResult(415);

            if (file.ContentLength > 0)
            {
                file.SaveAs(filePath);

                var bmp = (Bitmap)Image.FromFile(this.rootPath + "/uploads/" + file.FileName);
                var img2 = new Bitmap(bmp, new Size(250, 300));

                if (Detect(img2, this.haarFace).Length == 1 && Detect(img2, this.haarEye).Length == 2)
                {
                    isOk = true;
                }
            }

            return Json(new { Label = isOk, imgName = file.FileName });
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
            var bmp = (Bitmap)Image.FromFile(this.rootPath + "/uploads/" + fileName);

            // add original img size to imgProperties json
            // ReSharper disable once ArrangeThisQualifier
            this.imgProperties.Add("OriginalImageSize", bmp.Size.ToString());

            var img2 = new Bitmap(bmp, new Size(250, 300));
            this.imgProperties.Add("ProcessedImageSize", img2.Size.ToString());

            // Crop is not working very well...
            // img2 = ImageUtils.Crop(img2);
            // img2 = ImageUtils.AutoCrop(img2);

            // face detection
            var faces = Detect(img2, this.rootPath + "/HaarCascade/" + haarFace);

            Graphics g = Graphics.FromImage(img2);

            foreach (var rectangle in faces)
            {
                g.DrawRectangle(new Pen(Color.FromArgb(80, 255, 0, 0), (float)3.8), rectangle);
            }

            g.Save();

            // Eye detection
            var eyes = Detect(img2, this.rootPath + "/HaarCascade/" + haarEye);

            Graphics g2 = Graphics.FromImage(img2);

            foreach (var rectangle in eyes)
            {
                g2.FillRectangle(new SolidBrush(Color.FromArgb(50, 0, 255, 0)), rectangle);
            }

            g2.Save();

            if (faces.Length == 1 && eyes.Length == 2)
            {
                Console.WriteLine("Picture was processed and saved.");
                img2.Save(this.rootPath + "/processed/" + fileName);

                // get img properties
                GetImgProperties(faces[0], eyes[0], eyes[1]);

                // save json file
                SaveImgProperties(fileName);
            }
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

            Tuple<int, int> eye1Pos = new Tuple<int, int>((eye1.Top + eye1.Bottom) / 2, (eye1.Left + eye1.Right) / 2);
            Tuple<int, int> eye2Pos = new Tuple<int, int>((eye2.Top + eye2.Bottom) / 2, (eye2.Left + eye2.Right) / 2);

            double angle = Math.Atan2(Math.Abs(eye2Pos.Item1 - eye1Pos.Item1), Math.Abs(eye2Pos.Item2 - eye1Pos.Item2));

            this.imgProperties.Add("eyeTilt", Math.Round(angle * 100));
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

            System.IO.File.WriteAllText(this.rootPath + "/processed/properties_" +
                                                       fileName.Substring(0, fileName.Length - 4) + ".json", json);
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

            var cascadeClassifier = new CascadeClassifier(haarCascadeFile);

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
            return null;
        }
    }
}