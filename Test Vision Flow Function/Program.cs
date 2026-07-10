using System;
using System.IO;
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
// Theo bản hợp đồng IToolContext
{
    public CancellationToken CancellationToken => CancellationToken.None;
    // Là một property có kiểu trả về là CancellationToken và giá trị trả về là CancellationToken.None
    // Là tool chạy đến hết không cần hủy giữ chừng
    public double PixelSize => 1.0;
    // Ghi log in ra console
    public void Log(string message) => Console.WriteLine($"[FindLineTool] {message}");
    // Tại sao hàm log lại được khai báo ở hàm main ?
    // Vì cái nào muốn xài toolbox context có Log đó thì nó sẽ gọi object context
    // Trong object context có Log nên hàm log được khai báo ở đây để ai muốn sử dụng thì gọi nó
}

public class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; // Đặt bảng mã ký tự cho cửa sổ console là UTF-8 để gõ tiếng việt

        string imagePath = @"C:\Users\fs120806\Desktop\Document\Programing Project\Net_Programming\WPF_Class_Exercises\Sample Images\Phone Inspection.jpeg";
        string outputPath = @"C:\Users\fs120806\Desktop\Images\FindLine_Result.png";
        // Chữ @ ở phía trước ể chỉ ra rằng dấu \ được hiểu đúng là chính nó, không bị hiểu là ký tự đặc biệt khi kết hợp với cái ký tự khác
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        // Tạo thư mục dựa theo đường dẫn, nếu chưa có thì không làm gì cả, không ảnh hưởng gì
        Mat src = Cv2.ImRead(imagePath, ImreadModes.Color);
        // Ta có Cv2.Imread mở file ảnh, xong giải mã thành ma trận pixel 
        // Ta có ImreadModes ép về 3 kênh màu RGB kể cả ảnh màu hay ảnh xám
        if (src.Empty()) // Có thể trả về ảnh rỗng nên cần check trường hợp này
        {
            Console.WriteLine("Can not load image : " + imagePath);
            return;
        }
        Console.WriteLine($"Image loaded : {src.Width}x{src.Height}");

        var tool = new FindLineTool(); // FindLineTool kế thừa VisionTool
        // FindInput là của thằng VisionTool
        tool.FindInput("Image")!.Value = new MatVisionImage(src); 
        // Tìm cái cổng tên image trong tool sau đó gán vào tool
        // Có src là dữ liệu thô kiểu Mat của OpenCV, ta có MatVisionImage(src) là cất giữ ảnh vào trong MatVisionImage
        // MatVisionImage kế thừa IVisionImage nên ảnh được bọc trong IVisionImage
        // Ta có !.Value (Null Forgiving Operator - Null Suppression Operator) là tôi chắc chắn 100% kết quả của tool.FindInput("Image") sẽ không bao giờ bị null
        tool.FindParameter("Region")!.Value = new RotatedRectRegion(new P2(150, 600), 100, 100, 0);
        tool.FindParameter("NumberOfCalipers")!.Value = 20;
        tool.FindParameter("EdgeThreshold")!.Value = 100.0;
        tool.FindParameter("EdgePolarity")!.Value = "Either";

        var context = new SimpleToolContext(); // Tọa ra hộp đồ nghề chứa sẵn Log, Pixel, CancellationToken - None
        tool.Execute(context); // Bảo tool chạy đi và đưa đồ nghề cho nó chạy
        // Ta có Execute là hàm được viết trong VisionTool, tool kế thừa nên nó xài được
        var overlayImg = (IVisionImage)tool.FindOutput("Image")!.Value!;
        // Dấu ! thứ nhất cổng Image output chắc chắn tồn tại
        // Dấu ! thứ 2 giá trị của cổng chắc chắn không null
        var line = (LineResult)tool.FindOutput("Line")!.Value!;
        // Tương tự
        var edges = (P2[])tool.FindOutput("EdgePoints")!.Value!;
        // Tương tự
        var rms = (double)tool.FindOutput("RMSError")!.Value!;
        // Tương tự
        var score = (double)tool.FindOutput("Score")!.Value!;
        // Tương tự
        Console.WriteLine($"Judge={line.Judge} Angle={line.AngleDeg:f2} rms={rms:F2} Score={score:F2} Edges={edges.Length}");

        Cv2.ImWrite(outputPath, overlayImg.AsMat()); 
        // Mở vỏ lấy lại ảnh kiểu dữ liệu Mat và ghi ảnh ra file theo đường dẫn trên folder
        // Vì OpenCV chỉ làm việc với ảnh kiểu dữ liệu Mat
        Console.WriteLine("Saved : " + outputPath);

        Cv2.ImShow("FindLine Result", overlayImg.AsMat());
        Cv2.WaitKey();
        Cv2.DestroyAllWindows();

        overlayImg.Dispose(); // Trả lại bộ nhớ mà ảnh overlayImg đã chiếm
        // Ta có IVisionImage : IDisposable vì overlayImg nó kế thừa IDisposable nên nó có hàm Dispose()
        src.Dispose(); // Trả lại bộ nhớ m ảnh gốc đã chiếm
        // Ta có src là kiểu Mat thuộc thư viên OpenCV có hàm Dispose nên ta gọi được
    }
}