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

            var ret = Analyze(img2);
          
            Graphics g = Graphics.FromImage(img2);

            foreach (var rectangle in ret)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, 255, 0, 0)), rectangle);
            }

            g.Save();

            if (ret.Length == 0)
            {
                Console.WriteLine("This picture does not contain a face!");
            }
            else if (ret.Length > 1)
            {
                Console.WriteLine("This picture contains too many faces!");
            }
            else if (ret.Length == 1)
            {
                Console.WriteLine("Picture was processed and saved.");
                img2.Save(Server.MapPath("~/App_Data/processed/" + fileName));

                // get rectangle position to properties
                ImgProperties.Add("faceTop", ret[0].Top);
                ImgProperties.Add("faceBottom", ret[0].Bottom);
                ImgProperties.Add("faceLeft", ret[0].Left);
                ImgProperties.Add("faceRight", ret[0].Right);
                ImgProperties.Add("faceWidth", ret[0].Width);
                ImgProperties.Add("faceHeight", ret[0].Height);
                // save json file
                SaveImgProperties(fileName);
            }
        }

        private void EvaluateAndSaveImg(string fullFilePath, string fileName)
        {
            var bmp = (Bitmap) Image.FromFile(fullFilePath);

            var img2 = new Bitmap(bmp, new Size(250, 300));

            var ret = Analyze(img2);

            if(ret.Length == 1)
                img2.Save(Server.MapPath("~/App_Data/cleaning/clean_data/" + fileName));
            else
                img2.Save(Server.MapPath("~/App_Data/cleaning/dirty_data/" + fileName));
        }


        private Rectangle[] Analyze(Bitmap bmp)
        {
            Image<Rgb, Byte> x = new Image<Rgb, Byte>(bmp); //cap.ToImage<Rgb, DepthType>();
            var img = x.Convert<Gray, Byte>();

            var cascadeClassifier =
                new CascadeClassifier(Server.MapPath("~/App_Data/" + "/haarcascade_frontalface_default.xml"));

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

        private void SaveImgProperties(string fileName)
        {
            string json = JsonConvert.SerializeObject(ImgProperties, Formatting.Indented);

            System.IO.File.WriteAllText(Server.MapPath("~/App_Data/processed/properties_" + 
                                                       fileName.Substring(0, fileName.Length - 4) + ".json"), json);
        }



  
    }
}
