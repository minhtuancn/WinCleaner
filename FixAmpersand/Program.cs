using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        var file = @"D:\dev\setup-ai\WinCleaner\src\WinCleaner.App\Views\SettingsView.xaml";
        var bytes = File.ReadAllBytes(file);
        
        Console.WriteLine("First 20 bytes:");
        for (int i = 0; i < Math.Min(20, bytes.Length); i++)
        {
            Console.Write($"{bytes[i]:X2} ");
        }
        Console.WriteLine();
        
        // Check for BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            Console.WriteLine("Has UTF-8 BOM");
            // Check for double BOM
            if (bytes.Length >= 6 && bytes[3] == 0xEF && bytes[4] == 0xBB && bytes[5] == 0xBF)
            {
                Console.WriteLine("Has double BOM - fixing...");
                var newBytes = new byte[bytes.Length - 3];
                Array.Copy(bytes, 3, newBytes, 0, bytes.Length - 3);
                File.WriteAllBytes(file, newBytes);
                Console.WriteLine("Fixed double BOM");
            }
        }
        else
        {
            Console.WriteLine("No BOM");
        }
    }
}