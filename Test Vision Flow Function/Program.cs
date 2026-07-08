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
        Console.WriteLine($"Đã load ảnh : {src.Width}x{src.Height}");

        var tool = new FindLineTool();
        tool.FindInput("Image")!.Value = new MatVisionImage(src);

        tool.FindParameter("Number Of Caliper")!.Value = 15;
        tool.FindParameter("Edge Threshold")!.Value = 4.0;
        tool.FindParameter("Edge Polarity")!.Value = Either;

        var context = new SimpleToolContext();
        tool.Execute(context);

        var overLayImg = (IVisionImage)tool.FindOutput("Line")!.Value;
        var line = (LineResult)tool.FindOutput("Line")!.Value;
        var edges = (P2[])tool.FindOutput("EdgePoints")!.Value;
        var rms = (double)tool.FindOutput("RMSError")!.Value;
        var score = (double)tool.FindOutput("Score")!.Value;
        
        Console.WriteLine($"Judge={line.Judge} Angle=(line.AngleDeg:f2} rms={rms:F2} Score={score:F2} Edges={edges.Length}");

        Console.Imwrite(outputPath, overLayImg.AsMat());
        Console.WriteLine("Đã lưu : " + outputPath);
        
        overLayImg.Dispose();
        src.Dispose();

        Cv2.WaitKey();
        Cv2.DestroyAllWindows();
    }
}

