using System;
using System.Threading;
using OpenCvSharp;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Models;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Tools;
using VisionFlow.Tools.Finding;
using VisionFlow.Tools.Imaging;
using P2 = VisionFlow.Core.Models.Point2d;

namespace Test;

public sealed class SimpleToolContext : IToolContext
{
    public CancellationToken CancellationToken => CancellationToken.None;

    public double PixelSize => 1.0;
    
    public void Log(string message) => Console.WriteLine($"[FindLineTool] {message}");
}

public class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string imagePath = " ";
        string outputPath = " ";

        Mat src = Cv2.ImRead(imagePath, ImreadModes.Color);
        if (src.Empty())
        {
            Console.WriteLine("Không thể load ảnh : " + imagePath);
            return;
        }
        Console.WriteLine("Đã load ảnh : " + imagePath);
    }
}

