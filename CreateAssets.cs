using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main()
    {
        string dir = @"D:\dev\setup-ai\WinCleaner\src\WinCleaner.App\Assets";
        Directory.CreateDirectory(dir);
        
        var files = new[] { "StoreLogo.png", "Square150x150Logo.png", "Square44x44Logo.png", "Wide310x150Logo.png", "SplashScreen.png" };
        
        foreach (var file in files)
        {
            using var bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(0, 120, 212)); // Windows blue
            bmp.Save(Path.Combine(dir, file), ImageFormat.Png);
        }
        Console.WriteLine("Created placeholder images");
    }
}