using OpenCvSharp;
using OpenCvSharp.Flann;

namespace Rotation_Checking_Algorithm
{
    internal class Program
    {
        static (Point2f center, float radius)? FindCircle(Mat gray) // Dấu chấm hỏi cho biết kết quả có thể là null
        {
            Mat blurred = new Mat();
            // GaussianBlur là làm mịn mượt để khử nhiễu
            Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 2);
            // Ta có sigma là độ lệch chuẩn theo 2 chiều không gian của bức ảnh với sigma X kiểm soát độ mờ theo chiều ngang còn sigma Y là kiểm soát độ mờ theo chiều dọc
            CircleSegment[] circles = Cv2.HoughCircles(blurred, HoughModes.Gradient, dp: 1, minDist: 300, param1: 200, param2: 35, minRadius: 160, maxRadius: 235);
            // Parameter 1 là ngưỡng cho canny edge, Parameter 2 là ngưỡng cho độ tròn, còn minDist là ngưỡng tối thiểu giữa 2 tâm đường tròn
            if (circles.Length == 0) return null;
            return (circles[0].Center, circles[0].Radius);
        }

        static double NCC(Mat a, Mat b, Mat mask)
        {
            Mat af = new();
            Mat bf = new();
            a.ConvertTo(af, MatType.CV_32F); // Chuyển ảnh ra dạng số thực cho các giá trị pixel để lát nhân chia cộng trừ không bị mất số thập phân, thêm chính xác
            b.ConvertTo(bf, MatType.CV_32F); // Chuyển ảnh ra dạng số thực cho các giá trị pixel để lát nhân chia cộng trừ không bị mất số thập phân, thêm chính xác
            Scalar ma = Cv2.Mean(af, mask); // Tính độ sáng trung bình bằng cách cộng tổng tất cả lại rồi cho số điểm ảnh
            Scalar mb = Cv2.Mean(bf, mask); // Scalar là một hộp chứa 4 số thực
            Mat az = (af - ma).ToMat(); // Trừ đi độ sáng trung bình, độ sáng lệch bao nhiêu so với mức sáng trung bình của chính ảnh đó
            Mat bz = (bf - mb).ToMat(); // Khi thực hiện phép trừ bf - mb, kết quả trả về không phải là Mat ngay, mà là một đối tượng kiểu MatExpr (Matrix Expression – biểu thức ma trận) nên cần đổi về ToMat()
            az.SetTo(0, ~mask); // Đảo mặt nạ lại, xong cho OpenCV hàm set này set nó về 0 cho phần nền xung quanh, còn phần hình tròn thì không cần set, giữ nguyên
            bz.SetTo(0, ~mask);
            double dot = Cv2.Sum(az.Mul(bz)).Val0; // Trả về scalar một đối tượng chứa 4 con số
            double na = Math.Sqrt(Cv2.Sum(az.Mul(az)).Val0);
            double nb = Math.Sqrt(Cv2.Sum(bz.Mul(bz)).Val0);
            return dot / (na * nb + 1e-9); // Để chống lỗi chia cho 0, 1e-9 tức là 10 mũ -9
        }
        
        static double BruteForce(Mat refgray, Mat testgray) // Thuật toán Brute Force Matching để so sánh 2 bức ảnh với nhau
        {
            var cr = FindCircle(refgray)!.Value; // Dấu chấm than là tôi cam kết là kết quả không phải null đừng cảnh báo tui
            var ct = FindCircle(testgray)!.Value; // Dấu chấm than là tôi cam kết là kết quả không phải null đừng cảnh báo tui
            int r = (int)Math.Min(cr.radius, ct.radius);
            Mat A = new Mat(refgray, new Rect((int)(cr.center.X - r), (int)(cr.center.Y - r), 2 * r, 2 * r));
            Mat B = new Mat(testgray, new Rect((int)(ct.center.X - r), (int)(ct.center.Y - r), 2 * r, 2 * r));
            // Nhấn ctrl với click chuột trái để xem hàm khai báo hoặc ctrl với Q để xem quick docummentation
            // Trong OpenCV thì trục Y hướng xuống nha
            Mat mask = Mat.Zeros(A.Size(), MatType.CV_8UC1);
            Cv2.Circle(mask, new Point(r, r), r - 25, Scalar.All(255), -1);
            // Ta có 8UC1 là 8U là 8 bit unsigned kiểu số byte từ 0 đến 255
            // Ta có -1 là tô đặc bên trong hình tròn
            double bestscore = -2, bestangle = 0;
            for (int i = 0; i < 360; ++i)
            {
                Mat M = Cv2.GetRotationMatrix2D(new Point2f(r, r), i, 1.0);
                // Ta có 1.0 là giữ nguyên tỉ lệ bức ảnh, không phóng to hay thu nhỏ
                // Mat M chính là bản thiết kế toán học bảo OpenCV biết phải dịch chuyển từng pixel của ảnh A sang vị trí mới nào để tạo ra một bức ảnh đã được xoay góc i độ
                Mat rotated = new();
                Cv2.WarpAffine(A, rotated, M, A.Size()); // Xoay bức ảnh
                // Với A là anh gốc ban đầu
                // Ta có rotated ma trận chứa ảnh sau khi đã xoay xong
                // Kích thước của ảnh mới bằng kích thước ảnh gốc ban đầu
                double score = NCC(rotated, B, mask);
                if (score > bestscore)
                {
                    bestscore = score;
                    bestangle = i;
                }
            }
            return bestangle;
            // Một lưu ý quan trọng là khi xoay ảnh là chỗ đỗ bóng cũng sẽ bị xoay theo, trong khi vị trí đổ bóng là cố định so với những cột mốc trên ảnh
        }
        // Tách ánh sáng bằng cách lấy ảnh trừ đi bản làm mờ của chính nó thì sẽ còn lại đặc trưng
        static Mat RemoveLighting(Mat gray)
        {
            Mat f = new Mat();
            gray.ConvertTo(f, MatType.CV_32F);
            Mat lighting = new();
            Cv2.GaussianBlur(f, lighting, new Size(0, 0), sigmaX:25); // Size(0, 0) là để cho OpenCV tự tính toán dựa theo thông số sigmaX
            Mat detail = new();
            Cv2.Subtract(f, lighting, detail);
            return detail;
        }
        
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Mat imgref = Cv2.ImRead(@"C:\Users\fs120806\Desktop\Document\Programing Project\Net_Programming\Image Library\refs\base_A_ref.png", ImreadModes.Grayscale);
            Mat imgtest = Cv2.ImRead(@"C:\Users\fs120806\Desktop\Document\Programing Project\Net_Programming\Image Library\test\base_A_013.jpg", ImreadModes.Grayscale);
            //Console.WriteLine(BruteForce(imgref, imgtest));
            // Nhấn ctrl với click chuột trái để xem hàm khai báo hoặc ctrl với Q để xem quick docummentation
            // Trong OpenCV thì trục Y hướng xuống nha
            // Mat mask = Mat.Zeros(A.Size(), MatType.CV_8UC1);
            // Cv2.Circle(mask, new Point(r, r), r - 6, Scalar.All(255), -1);
            // Console.WriteLine($"Kích thước : {imgref.Rows} x {imgref.Cols}");
            // Console.WriteLine($"Giá trị pixel tại tâm ảnh : {imgref.At<byte>(256, 256)}");
            // Console.WriteLine($"Giá trị pixel tại góc bên trái nền tối : {imgref.At<byte>(10, 10)}");
            // (Point2f center, float radius)? res = FindCircle(imgref);
            // Console.WriteLine(res);
            // Cv2.Circle(imgref, new Point((int)res.Value.center.X, (int)res.Value.center.Y), (int)res.Value.radius, Scalar.Red, 3);
            // Cv2.ImShow("Image", mask);
            Mat res = new();
            res = RemoveLighting(imgref);
            Cv2.ImShow("Image", res);
            Cv2.WaitKey();
            // Phím tắt Ctrl + P để xem hàm đó có những thông số nào để set và thiết lập
            // Vì Circle yêu cầu giá trị point là int mà res trả về float nên cần tạo điểm mới đổi về int
            // Chấm value là vì khi chấm value bạn chắc rằng giá trị đó không null, nếu không chấm value mà giá trị đó null nó sẽ báo lỗi
        }
    }
}