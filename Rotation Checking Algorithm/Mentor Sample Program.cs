using System; // Sử dụng các lệnh cơ bản của .NET
using System.Collections.Generic; // Cung cấp các cấu trức dữ liệu dạng tập hợp
using System.IO; // Dùng cho các lệnh đọc, quản lý file
using System.Linq; // Cũng cấp các hàm mở rộng của LinQ để truy vấn, sắp xếp dữ liệu trên mảng
using OpenCvSharp; // Thư viện wrapper C# của OpenCV

class Program
{
    const int NA = 3600; // Số hàng, tuơng đương một chu vi hình tròn
    const int NR = 240; // Số cột là bán kính

    static (Point2f center, float radius)? FindCircle(Mat gray)
    {
        using Mat blurred = new Mat(); // Tạo ra một bức ảnh rỗng tên là blurred và tự động dọn dẹp giải phóng bộ nhớ RAM ngay khi hàm chạy xong
    }
}