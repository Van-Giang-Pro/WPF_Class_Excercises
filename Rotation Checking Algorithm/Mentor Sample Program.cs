using System; // Sử dụng các lệnh cơ bản của .NET
using System.Collections.Generic; // Cung cấp các cấu trức dữ liệu dạng tập hợp
using System.IO; // Dùng cho các lệnh đọc, quản lý file
using System.Linq; // Cũng cấp các hàm mở rộng của LinQ để truy vấn, sắp xếp dữ liệu trên mảng
using OpenCvSharp; // Thư viện wrapper C# của OpenCV

class Program
{
    const int NA = 3600; // Số hàng, tuơng đương một chu vi hình tròn
    const int NR = 240; // Số cột là bán kính

    // Tìm vật thể hình tròn
    static (Point2f center, float radius)? FindCircle(Mat gray)
    {
        using Mat blurred = new Mat(); // Tạo ra một bức ảnh rỗng tên là blurred và tự động dọn dẹp giải phóng bộ nhớ RAM ngay khi hàm chạy xong
        Cv2.GaussianBlur(gray, blurred, new Size(9, 9), 2); // Số 2 là sigma X quy định mức độ lan tỏa làm mở của thuật toán
        // Hough Circle Transform là tự động dò tìm các hình tròn trong bức ảnh và trả về danh sách các tọa độ tâm và bán kính của chúng
        CircleSegment[] circles = Cv2.HoughCircles(blurred, HoughModes.Gradient, 1, 300, param1 : 110, param2 : 35, minRadius : 160, maxRadius : 235);
        // Ta có param 2, độ nhạy càng thấp thì càng dễ phát hiện hình tròn bị mờ đứt khúc nhưng dễ bị nhận nhầm rác
        // Còn độ nhạy càng cao thì bắt được những hình ảnh rõ nét, loại bỏ hình ảo
        // Ta có param 1, ngưỡng trên của bộ lọc cạnh canny chạy ngầm bên trong để tìm ra các đường viền cạnh
        // HoughModes.Gradient là chế độ dò tìm dựa trên độ dốc gradient của các cạnh trong ảnh
        // Ta có 1 là dp, quét với độ phân giải nguyên bản của ảnh
        // Ta c mindist là 300 là khảng cách tối thiểu giữa 2 tâm đường tròn, tránh việc thuật toán bắt nhiều hình tròn trùng nhau
        if (circles.Length == 0) return null; // Ta có Length là số lượng của các hình tròn tìm được trong danh sách
        return (circles[0].Center, circles[0].Radius); // Hình tròn thứ 0 là rõ nét nhất
    }
    
    // Trải hình tròn ra thành hình chữ nhật
    static Mat Unwrap(Mat gray, Point2f center, float radius) // Point2f là kiểu dữ liệu cấu trúc (struct) dùng để biểu thị tọa độ của 1 điểm 2 chiều (X, Y) dưới dạng số thực thập phân
    {
        using Mat polar = new Mat(); // Tạo ra một bức ảnh rỗng tên là blurred và tự động dọn dẹp giải phóng bộ nhớ RAM ngay khi hàm chạy xong
        Cv2.WarpPolar(gray, polar, new Size(NR, NA), center, radius, InterpolationFlags.Linear, WarpPolarMode.Linear);
        // Ta có InterpolationFlags.Linear là nội suy làm mịn là thuật toán pha trộn màu các pixel lân cận, giúp ảnh sau khi trải ra mịn đẹp, không bị răng cưa
        // Còn có WarpPolarMode.Linear là chế độ trải tuyến tính là bán kính được trải phẳng đều đặn theo đường thẳng từ trong ra ngoài (r tăng đều theo cấp số cộng)
        using Mat outer = new Mat(polar, new Rect(NR / 4, 0, NR - NR / 4, NA)); // Cắt sau khi trải ra, góc tọa độ là bên mép trái, lấy phần ngoài, bỏ tâm ra
        // Tâm đồng xu là mép góc bên trái, cắt phần trong bỏ phần ngoài
        Mat f = new Mat();
        outer.ConvertTo(f, MatType.CV_32F); // Ta có CV_32F là kiểu số thực (số thập phân 32 bit) tương đương với kiểu float trong C#
        return f;
    }
    
    // Chu kỳ góc 1D với khử ánh sáng cố định
    static double[] AngularSignature(Mat polarF32) // Gom bức ảnh 2D thành một dãy số 1D
    // Polar là ảnh đã được unwrap từ hình tròn ra hình chữ nhật
    {
        using Mat rowSum = new Mat(); // Ta có rowSum có số hàng bằng đúng giá trị NA
        Cv2.Reduce(polarF32, rowSum, ReduceDimension.Column, ReduceTypes.Sum, MatType.CV_32F); // Dùng để ép, thu gọn một bức ảnh 2D (nhiều hàng, nhiều cột) thành một dải 1D (một hàng hoặc một cột)
        // Ta có ReduceDimension là gom tất cả các cột trên từng dòng lại, kết quả là một cột đứng
        double[] sig = new double[NA]; // Tạo ra một mảng số thực tên là sig, có độ dài bằng đúng NA phần tử, ta có new double[NA] là cấp phát bộ nhớ cho mảng có kích thước đúng bằng NA
        for (int i = 0; i < NA; i++) sig[i] = rowSum.At<float>(i, 0); // Ta có rowSum.At là truy cập vào ma trận rowSum và đọc giá trị đó dưới dạng số thực
        double mean = sig.Average();
        for (int i = 0; i < NA; i++) sig[i] -= mean; // Cân bằng độ sáng về mức quanh 0, giúp ảnh được chụp trong điều kiện sáng hay tối đều tương tự nhau, không có gì quá khác biệt
        // Tại đây ta có sig chứa NA giá trị với những giá trị đó là sau khi trừ đi trung bình
        int w = 301, half = w / 2; // Chọn cửa sổ quét là mảng một chiều có 301 giá trị, có 1 giá trị ở giữa, 2 bên là 150 giá trị
        double[] hp = new double[NA];
        double run = 0;
        for (int i = -half; i <= half; i++) run += sig[(i + NA) % NA]; // Lệnh này sẽ chứa tổng số độ sáng của những đểm xung quanh điểm center, tức là điểm 0, 150 điểm bên trái, 150 điểm bên phải và điểm tại góc 0
        // Trong C# không thể viế được sig[-1] chỉ số âm nên +NA để lấy ra đợc, việc +NA sẽ lấy ra 150 giá trị đầu của mảng đó
        // Chúng ta phải chia cho NA vì khi NA cộng với i sẽ over giá trị của một mảng nên cần chia lấy dư để lấy lại chỉ số trong giá trị của mảng
        for (int i = 0; i < NA; i++)
        {
            hp[i] = sig[i] - run / w; // Lấy độ sáng tại chính điểm i trừ cho độ sáng trung bình của của vùng xung quanh
            // Nếu tại điểm i có chữ in nổi hoặc hoa văn, giá trị sẽ dương vọt lên
            // Nếu tại điểm i chỉ là bề mặt phảng bóng mờ thì giá trị sẽ sấp xỉ bằng 0 (bóng đổ bị xóa sạch)
            run += sig[(i + half + 1) % NA] - sig[(i - half + NA) % NA]
        }
        return hp;
    }
}