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
    public class HomeController : Controller
    {
        private JObject ImgProperties = new JObject();

        private const string haarFace = "haarcascade_frontalface_default.xml";
        private const string haarEye = "haarCascade_eye.xml";

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult CleanFolder()
        {
            string folderName = "~/App_Data";

            DirectoryInfo d = new DirectoryInfo(Server.MapPath("~/App_Data/"));
            FileInfo[] files = d.GetFiles("*.jpg");


            foreach (var file in files)
            {
                // extract only the filename
                var fileName = Path.GetFileName(file.Name);
                // store the file inside ~/App_Data/uploads folder

                var filePath = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName ?? throw new InvalidOperationException());

                file.CopyTo(filePath, overwrite:true);

                EvaluateAndSaveImg(filePath, fileName);
            }

            return RedirectToAction("Index");
        }

        public ActionResult UploadAndProcess(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                // extract only the filename
                var fileName = Path.GetFileName(file.FileName);
                // store the file inside ~/App_Data/uploads folder
                var filePath = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName ?? throw new InvalidOperationException());

                // we do not need to save original file (at development stage)
                file.SaveAs(filePath);

                Convert(fileName);
            }
            else
            {
                Console.WriteLine("Upload error!");
            }

            // redirect back to the index action to show the form once again
            return RedirectToAction("Index");
        }

        private void Convert(string fileName)
        {
            var bmp = (Bitmap) Image.FromFile(Server.MapPath("~/App_Data/uploads/" + fileName));

            // add original img size to imgProperties json
            ImgProperties.Add("OriginalImageSize", bmp.Size.ToString());

            var img2 = new Bitmap(bmp, new Size(250, 300));
            ImgProperties.Add("ProcessedImageSize", img2.Size.ToString());

            // face detection
            var faces = Detect(img2, haarFace);
          
            Graphics g = Graphics.FromImage(img2);

            foreach (var rectangle in faces)
            {
                //g.FillRectangle(new SolidBrush(Color.FromArgb(50, 255, 0, 0)), rectangle);
                g.DrawRectangle(new Pen(Color.FromArgb(80, 255, 0, 0), (float)2.8), rectangle);
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

        private void EvaluateAndSaveImg(string fullFilePath, string fileName)
        {
            var bmp = (Bitmap) Image.FromFile(fullFilePath);

            var img2 = new Bitmap(bmp, new Size(250, 300));

            var ret = Detect(img2, "");

            if(ret.Length == 1)
                img2.Save(Server.MapPath("~/App_Data/cleaning/clean_data/" + fileName));
            else
                img2.Save(Server.MapPath("~/App_Data/cleaning/dirty_data/" + fileName));
        }


        private Rectangle[] Detect(Bitmap bmp, string haarCascadeFile)
        {
            Image<Rgb, Byte> x = new Image<Rgb, Byte>(bmp); 
            var img = x.Convert<Gray, Byte>();

            var cascadeClassifier =
                new CascadeClassifier(Server.MapPath("~/App_Data/HaarCascade/" + haarCascadeFile));

            using (var imageFrame = x)
            {
                if (imageFrame != null)
                {
                    var grayframe = imageFrame.Convert<Gray, Byte>();
                    var faces = cascadeClassifier.DetectMultiScale(grayframe, 1.1, 10,
                        Size.Empty); //the actual face detection happens here
                    return faces;
                }
            }
            return new Rectangle[0];
        }

        private void GetImgProperties(Rectangle face, Rectangle eye1, Rectangle eye2)
        {
                // get face position
                ImgProperties.Add("faceTop", face.Top);
                ImgProperties.Add("faceBottom", face.Bottom);
                ImgProperties.Add("faceLeft", face.Left);
                ImgProperties.Add("faceRight", face.Right);
                ImgProperties.Add("faceWidth", face.Width);
                ImgProperties.Add("faceHeight", face.Height);
                // get eye positions
                ImgProperties.Add("eye_1_horizontal",  (eye1.Top + eye1.Bottom)/2);
                ImgProperties.Add("eye_1_vertical", (eye1.Left + eye1.Right) / 2);
                ImgProperties.Add("eye_2_horizontal", (eye2.Top + eye2.Bottom) / 2);
                ImgProperties.Add("eye_2_vertical", (eye2.Left + eye2.Right) / 2);
                ImgProperties.Add("eyeTilt", ((eye1.Top + eye1.Bottom) / 2) - 
                                             ((eye2.Top + eye2.Bottom) / 2));
        }

        private void SaveImgProperties(string fileName)
        {
            string json = JsonConvert.SerializeObject(ImgProperties, Formatting.Indented);

            System.IO.File.WriteAllText(Server.MapPath("~/App_Data/processed/properties_" + 
                                                       fileName.Substring(0, fileName.Length - 4) + ".json"), json);
        }
  
    }
}
