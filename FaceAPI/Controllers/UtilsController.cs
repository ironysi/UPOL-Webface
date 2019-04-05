namespace FaceAPI.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Web.Hosting;
    using System.Web.Mvc;

    using ImageMagick;

    using Image = System.Drawing.Image;

    public class UtilsController : Controller
    {
        //private readonly Dictionary<string, string> photoDict = new Dictionary<string, string>();

        //private readonly MagickImageCollection facesCollection = new MagickImageCollection();
        //private readonly List<Rectangle> facesRectangles = new List<Rectangle>();

        //private readonly List<Tuple<string, Rectangle, double>> csvDataScore = new List<Tuple<string, Rectangle, double>>();
        //private readonly List<Tuple<string, double, double, double>> csvDataBrightness = new List<Tuple<string, double, double, double>>();

        //private readonly string rootPath = HostingEnvironment.MapPath("~/App_Data");

        //public ActionResult Histogram()
        //{
        //    List<Tuple<string, int[]>> histCollection = new List<Tuple<string, int[]>>();

        //    var dirPath = @"C:\\Users\\XXX\\Documents\\Visual_Studio_Projects\\"
        //                  + "FotkyKarty\\WebFace\\UPOL-Webface\\WebFace\\App_Data\\"
        //                  + "Uploads\\";

        //    DirectoryInfo dir = new DirectoryInfo(dirPath);
        //    FileInfo[] files = dir.GetFiles("*.jpg");

        //    for (int i = 0; i < files.Length; i++)
        //    {
        //        int[] brightnessHist = new int[256];
        //        var bmp = (Bitmap)Image.FromFile(dirPath + files[i].Name);
        //        var image = new Bitmap(bmp, new Size(250, 300));

        //        for (int x = 0; x < image.Width; x++)
        //        {
        //            for (int y = 0; y < image.Height; y++)
        //            {
        //                 Get pixel colors and brightness
        //                var br = (int)Math.Round((image.GetPixel(x, y).R * 0.2126) + (image.GetPixel(x, y).G * 0.7152) + (image.GetPixel(x, y).B * 0.0722), 0);

        //                brightnessHist[br]++;
        //            }
        //        }

        //        histCollection.Add(new Tuple<string, int[]>(files[i].Name, brightnessHist));

        //        if (i % 1000 == 0 && i != 0)
        //        {
        //            System.Text.StringBuilder sb = new System.Text.StringBuilder();
        //            sb.AppendLine("Name,Hist");

        //            foreach (var item in histCollection)
        //            {
        //                sb.AppendLine(item.Item1 + ", " + string.Join(",", item.Item2));
        //            }

        //            System.IO.File.WriteAllText(
        //                Path.Combine(this.rootPath, "brightness_histograms_" + (i / 1000) + "k.csv"),
        //                sb.ToString());

        //            histCollection = new List<Tuple<string, int[]>>();
        //        }
        //    }

        //    System.Text.StringBuilder sbx = new System.Text.StringBuilder();
        //    sbx.AppendLine("Name,Hist");

        //    foreach (var item in histCollection)
        //    {
        //        sbx.AppendLine(item.Item1 + ", " + string.Join(",", item.Item2));
        //    }

        //    System.IO.File.WriteAllText(Path.Combine(this.rootPath, "brightness_histograms_last.csv"), sbx.ToString());

        //    return new HttpStatusCodeResult(200);
        //}

        /// <summary>
        /// Takes all files from "App_Data/uploads/" and sorts them into "clean_data" and "dirty_data" based on face detection. 
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        //public ActionResult CleanFolder()
        //{
        //    DirectoryInfo dir = new DirectoryInfo(this.rootPath);
        //    FileInfo[] files = dir.GetFiles("*.jpg");

        //    int i = 0;

        //    foreach (var file in files)
        //    {
        //         store the file inside ~/App_Data/uploads folder
        //        var filePath = Path.Combine(this.rootPath + "uploads", file.Name ?? throw new InvalidOperationException());

        //         file.CopyTo(filePath, overwrite:true);

        //        this.EvaluateAndSaveImg(filePath, file.Name);

        //        i++;
        //        if (i == 2000)
        //            break;
        //    }

        //    var csv = string.Join(
        //        Environment.NewLine,
        //        this.photoDict.Select(x => x.Key + "," + x.Value));

        //    System.IO.File.WriteAllText(Path.Combine(this.rootPath, "processed", "stats_1000_originalSize.csvData"), csv);

        //    return new HttpStatusCodeResult(200);
        //}

        //public ActionResult SuperPositionJson()
        //{
        //    DirectoryInfo dir = new DirectoryInfo("C:\\Users\\XXX\\Documents\\Visual_Studio_Projects\\FotkyKarty\\WebFace\\UPOL-Webface\\WebFace\\App_Data\\uploads");
        //    FileInfo[] files = dir.GetFiles("*.jpg");

        //    var x = 0;
        //    var y = 0;
        //    var width = 0;


        //    foreach (var file in files)
        //    {
        //        var bmp = (Bitmap)Image.FromFile("C:\\Users\\XXX\\Documents\\Visual_Studio_Projects\\FotkyKarty\\WebFace\\UPOL-Webface\\WebFace\\App_Data\\uploads\\" + file.Name);
        //        var img2 = new Bitmap(bmp, new Size(250, 300));

        //         faces
        //        var faces = ImageUtils.Detect(img2, ImageUtils.HaarFace);


        //        if (faces.Length == 1)
        //        {
        //            this.facesRectangles.Add(faces[0]);
        //        }

        //    }

        //    foreach (var rect in this.facesRectangles)
        //    {
        //        x += rect.X;
        //        y += rect.Y;
        //        width += rect.Width;
        //    }

        //    x = x / this.facesRectangles.Count;
        //    y = y / this.facesRectangles.Count;
        //    width = width / this.facesRectangles.Count;

        //    var averages = "X:\t" + x + Environment.NewLine +
        //                   "Y:\t" + y + Environment.NewLine +
        //                   "Width:\t" + width + Environment.NewLine;

        //    System.IO.File.WriteAllText(this.rootPath + "/superposition.txt", averages);


        //    return new HttpStatusCodeResult(200);
        //}

        //public ActionResult SuperPosition()
        //{
        //    DirectoryInfo dir = new DirectoryInfo("C:\\Users\\XXX\\Documents\\Visual_Studio_Projects\\FotkyKarty\\WebFace\\UPOL-Webface\\WebFace\\App_Data\\uploads");
        //    FileInfo[] files = dir.GetFiles("*.jpg");

        //    int i = 0;

        //    foreach (var file in files)
        //    {
        //        var bmp = (Bitmap)Image.FromFile("C:\\Users\\XXX\\Documents\\Visual_Studio_Projects\\FotkyKarty\\WebFace\\UPOL-Webface\\WebFace\\App_Data\\uploads\\" + file.Name);
        //        var img2 = new Bitmap(bmp, new Size(250, 300));

        //         faces
        //        var faces = ImageUtils.Detect(img2, ImageUtils.HaarFace);

        //        Bitmap clearImg = new Bitmap(250, 300, PixelFormat.Format16bppRgb555);
        //        Graphics g = Graphics.FromImage(clearImg);

        //        foreach (var rectangle in faces)
        //        {
        //            g.FillRectangle(new SolidBrush(Color.FromArgb(90, 255, 0, 0)), rectangle);
        //        }

        //        g.Save();

        //        var eyes = ImageUtils.Detect(img2, ImageUtils.HaarEye);

        //        Graphics g2 = Graphics.FromImage(clearImg);

        //        foreach (var rectangle in eyes)
        //        {
        //            g2.FillRectangle(new SolidBrush(Color.FromArgb(90, 0, 255, 0)), rectangle);
        //        }

        //        g2.Save();

        //        this.facesCollection.Add(new MagickImage(clearImg));

        //        i++;
        //        if (i == 100)
        //            break;
        //    }

        //    this.facesCollection.Evaluate(EvaluateOperator.Mean).Write(
        //        Path.Combine(this.rootPath, "processed", "faceAndEyesSuperposition_negative.png"));

        //    return new HttpStatusCodeResult(200);
        //}

        //public ActionResult CalculateScores()
        //{
        //    DirectoryInfo dir = new DirectoryInfo("C:\\Users\\XXX\\Documents\\Visual_Studio_Projects\\"
        //                                          + "FotkyKarty\\WebFace\\UPOL-Webface\\WebFace\\App_Data\\"
        //                                          + "showcase_pics\\10_worst_pics");
        //    FileInfo[] files = dir.GetFiles("*.jpg");

        //    int i = 0;

        //    foreach (var file in files)
        //    {
        //        var bmp = (Bitmap)Image.FromFile(
        //            "C:\\Users\\XXX\\Documents\\Visual_Studio_Projects\\FotkyKarty\\WebFace\\UPOL-Webface\\WebFace\\App_Data\\uploads\\"
        //            + file.Name);
        //        var img = new Bitmap(bmp, new Size(250, 300));

        //        if (file.Name.Equals("12a21d09-b392-4ae1-a1ee-f02ed42f5a96.jpg"))
        //        {
        //            var imageBytes = (byte[])new ImageConverter().ConvertTo(img, typeof(byte[]));
        //            System.IO.File.WriteAllBytes(HostingEnvironment.MapPath("~/App_data/img2.txt"), imageBytes);
        //        }

        //        var faces = ImageUtils.Detect(img, ImageUtils.HaarFace);

        //        if (faces.Length == 1)
        //        {
        //            var score = ImageUtils.PositionalScore(faces[0]);
        //            this.csvDataScore.Add(new Tuple<string, Rectangle, double>(file.Name, faces[0], score));
        //        }
        //        else
        //        {
        //            this.csvDataScore.Add(new Tuple<string, Rectangle, double>(file.Name, new Rectangle(), 0.0));
        //        }

        //        i++;
        //        if (i == 100)
        //            break;
        //    }

        //    System.Text.StringBuilder sb = new System.Text.StringBuilder();
        //    sb.AppendLine("Name,X,Y,WIDTH,HEIGHT,SCORE");

        //    foreach (var row in this.csvDataScore)
        //    {
        //        sb.AppendLine(row.Item1 + "," + row.Item2.X + "," + row.Item2.Y + "," + row.Item2.Width + "," +
        //                      row.Item2.Height + "," + row.Item3);
        //    }

        //    System.IO.File.WriteAllText(Path.Combine(this.rootPath, "scores_worst.csv"), sb.ToString());

        //    return new HttpStatusCodeResult(200);
        //}

        //public ActionResult CalculateBrightness()
        //{
        //    var dirPath = @"C:\\Users\\XXX\\Documents\\Visual_Studio_Projects\\"
        //                  + "FotkyKarty\\WebFace\\UPOL-Webface\\WebFace\\App_Data\\"
        //                  + "Uploads\\";

        //    DirectoryInfo dir = new DirectoryInfo(dirPath);
        //    FileInfo[] files = dir.GetFiles("*.jpg");

        //    foreach (var file in files)
        //    {
        //        var bmp = (Bitmap)Image.FromFile(dirPath + file.Name);
        //        var img = new Bitmap(bmp, new Size(250, 300));

        //        var faces = ImageUtils.Detect(img, ImageUtils.HaarFace);

        //        if (faces.Length == 1)
        //        {
        //            var brightness = ImageUtils.CalcBrightness(ImageUtils.GetPartOfPicture(img, faces[0]));

        //            this.csvDataBrightness.Add(new Tuple<string, double, double, double>(file.Name, brightness.Item1,
        //                brightness.Item2, brightness.Item3));
        //        }
        //        else
        //        {
        //            this.csvDataBrightness.Add(new Tuple<string, double, double, double>(file.Name, 0.0, 0.0, 0.0));
        //        }
        //    }

        //    System.Text.StringBuilder sb = new System.Text.StringBuilder();
        //    sb.AppendLine("Name,RedBr,GreenBr,BlueBr");

        //    foreach (var (item1, item2, item3, item4) in this.csvDataBrightness)
        //    {
        //        sb.AppendLine(item1 + "," + item2 + "," + item3 + "," + item4);
        //    }

        //    System.IO.File.WriteAllText(Path.Combine(this.rootPath, "brightness.csv"), sb.ToString());

        //    return new HttpStatusCodeResult(200);
        //}

        //public ActionResult ChiSqrScore()
        //{
        //    string p = @"C:\\Users\\XXX\\Documents\\Visual_Studio_Projects\\FotkyKarty\\WebFace\\UPOL-Webface\\WebFace\\App_Data\\uploads\\";

        //    DirectoryInfo dir = new DirectoryInfo(p);
        //    FileInfo[] files = dir.GetFiles("*.jpg");

        //    var myCsv = new List<Tuple<string, double>>();

        //    for (int i = 0; i < files.Length; i++)
        //    {
        //        var bmp = (Bitmap)Image.FromFile(p + files[i].Name);
        //        var img = new Bitmap(bmp, new Size(250, 300));

        //        var xc = ImageUtils.BrightnessScore(img);
        //        myCsv.Add(new Tuple<string, double>(files[i].Name, Math.Round(xc, 2)));

        //        if (i == 5000)
        //            break;
        //    }

        //    System.Text.StringBuilder sb = new System.Text.StringBuilder();
        //    sb.AppendLine("Name,Brightness");

        //    foreach (var row in myCsv)
        //    {
        //        sb.AppendLine(row.Item1 + "," + row.Item2);
        //    }

        //    System.IO.File.WriteAllText(Path.Combine(this.rootPath, "br_comp_test_10k.csv"), sb.ToString());

        //    return new HttpStatusCodeResult(200);
        //}

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
        //private void EvaluateAndSaveImg(string fullFilePath, string fileName)
        //{
        //    var bmp = (Bitmap)Image.FromFile(fullFilePath);

        //    var img2 = new Bitmap(bmp, new Size(250, 300));
        //     var img2 = new Bitmap(bmp);

        //    var faces = ImageUtils.Detect(img2, ImageUtils.HaarFace);
        //    var eyes = ImageUtils.Detect(img2, ImageUtils.HaarEye);

        //    var photoProperties = faces.Length + ", " + eyes.Length;

        //     face tilt (angle of vector in between of eyes)
        //    if (eyes.Length == 2)
        //    {
        //        Tuple<int, int> eye1Pos = new Tuple<int, int>(
        //            (eyes[0].Top + eyes[0].Bottom) / 2,
        //            (eyes[0].Left + eyes[0].Right) / 2);
        //        Tuple<int, int> eye2Pos = new Tuple<int, int>(
        //            (eyes[1].Top + eyes[1].Bottom) / 2,
        //            (eyes[1].Left + eyes[1].Right) / 2);

        //        double angle = Math.Atan2(
        //            Math.Abs(eye2Pos.Item1 - eye1Pos.Item1),
        //            Math.Abs(eye2Pos.Item2 - eye1Pos.Item2));

        //        photoProperties = photoProperties + ", " + Math.Round(angle * 100);
        //    }
        //    else
        //    {
        //        photoProperties = photoProperties + ", NaN";
        //    }

        //    if (faces.Length == 1)
        //    {
        //        photoProperties = photoProperties + ", " + bmp.Height + ", " + bmp.Width;

        //        this.photoDict.Add(fileName, photoProperties);
        //         img2.Save(rootPath + "/cleaning/clean_data/" + fileName);
        //    }
        //    else if (faces.Length < 1)
        //    {
        //        photoProperties = photoProperties + ", " + bmp.Height + ", " + bmp.Width;

        //        this.photoDict.Add(fileName, photoProperties);
        //         img2.Save(rootPath + "/cleaning/dirty_data/" + fileName);
        //    }
        //    else if (faces.Length > 1)
        //    {
        //        photoProperties = photoProperties + ", " + bmp.Height + ", " + bmp.Width;

        //        this.photoDict.Add(fileName, photoProperties);
        //         img2.Save(rootPath + "/cleaning/dirty_data/" + fileName);
        //    }
        //}
    }
}