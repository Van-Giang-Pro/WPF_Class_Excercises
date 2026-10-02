using System; // Sử dụng các lệnh cơ bản của .NET
using System.Collections.Generic; // Cung cấp các cấu trức dữ liệu dạng tập hợp
using System.IO; // Dùng cho các lệnh đọc, quản lý file
using System.Linq;
using System.Runtime.InteropServices.Marshalling; // Cũng cấp các hàm mở rộng của LinQ để truy vấn, sắp xếp dữ liệu trên mảng
using OpenCvSharp; // Thư viện wrapper C# của OpenCV

class Program
{
    const int NA = 3600; // Số hàng, tuơng đương một chu vi hình tròn
    const int NR = 240; // Số cột là bán kính

    // Tìm vật thể hình tròn
    static (Point2f center, float radius)? FindCircle(Mat gray) 
    {
        // Giá trị trả về là một kiểu tupple nên cần thêm value để thêm để truy xuất thông số center và radius
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
        for (int i = -half; i <= half; i++) run += sig[(i + NA) % NA]; // Lệnh này sẽ chứa tổng số độ sáng của những đểm trong mảng đó, tức là điểm 0, 150 điểm bên trái, 150 điểm bên phải và điểm tại góc 0
        // Trong C# không thể viết được sig[-1] chỉ số âm nên +NA để lấy ra đợc, việc +NA sẽ lấy ra 150 giá trị đầu của mảng đó
        // Chúng ta phải chia cho NA vì khi NA cộng với i sẽ over giá trị của một mảng nên cần chia lấy dư để lấy lại chỉ số trong giá trị của mảng
        for (int i = 0; i < NA; i++)
        {
            hp[i] = sig[i] - run / w; // Lấy độ sáng tại chính điểm i trừ cho độ sáng trung bình của của vùng xung quanh
            // Nếu tại điểm i có chữ in nổi hoặc hoa văn, giá trị sẽ dương vọt lên
            // Nếu tại điểm i chỉ là bề mặt phảng bóng mờ thì giá trị sẽ sấp xỉ bằng 0 (bóng đổ bị xóa sạch)
            run += sig[(i + half + 1) % NA] - sig[(i - half + NA) % NA] // Dịch chuyển cửa sổ quét sang phải một bước để chuẩn bị cho vòng lặp tiếp theo
            // Lấy tổng cộng số mới từ số cũ là sẽ được tổng của dãy mới sau khi dịch phải một điểm
        }
        return hp;
    } // Chạy hàm này cho đồng xu chuẩn thì ra sb còn cho đồng xu chuẩn thì ra sa
    
    // Tương quan vòng tròn
    static double[] CircularCorrScan(double[] sa, double[] sb)
    // Ta có sa là dấu vân tay 1D của đồng xu cần kiểm tra
    // Ta có sb là dấu vân tay 1D của đồng xu mẫu chuẩn
    // Ta có double[] trả về mảng điểm số khớp với từng góc xoay
    {
        double[] corr = new double[NA] // Tạo mảng chứa điểm số khớp tương ứng với từng góc xoay
        for (int shift = 0; shift < NA; shift++) // Thử xoay đồng xu lần lượt qua từng góc một
        {
            double s = 0;
            for (int i = 0; i < NA; i++)
                s += sa[(i + shift) % NA] * sb[i]; // Lấy giá trị của đồng xu sa tại điểm i sau khi đã xoay một đoạn là shift
            // Ta có s sẽ chứa tổng số điểm trùng khớp giữa 2 đồng xu, nếu có điểm trùng nhau giống nhau thì s sẽ lớn, còn không giống thì nó sẽ nhỏ
            // Ta có nhân với sb[i] là phép chấm điểm tương quan (Dot Product) nếu cùng lồi hoặc cùng lõm, thì khớp nét cộng điểm, nếu lệch nét thì ra số âm, lệch nét trừ điểm
            corr[shift] = s; // Mảng chứa điểm số ở mỗi góc xoay
        }
        return corr;
    }

    // Tương quan vòng tròn bằng biến đổi Fourier DFT nhanh hơn
    static double[] CirculaCorrFFT(double[] sa, double[] sb)
    {
        float[] fa = sa.Select(v => (float)v).ToArray(); 
        // Đổi toàn bộ mảng sa từ kiểu số double sang mảng mới fa có kiểu số float
        // Ta có Select là lệnh duyệt qua từng phần tử để biến đổi
        // Ta có v => (float)v là với mỗi con số v hãy ép nó thành số float (32 bit)
        // Ta có .ToArray() đóng gói tất cả các số vừa đổi thành mảng mới
        // Ta có float[] fa là lưu mảng mảng mới vào biến fa
        float[] fb = sb.Select(v => (float)v).ToArray();
        // Đổi qua fb dạng float 32 bit để dùng cho Fourier
        using Mat A = Mat.FromPixelData(1, NA, MatType.CV_32FC1, fa);
        // Ta có using là tự động giải phóng bộ nhớ RAM của ma trận A khi hàm chạy xong (tránh rỉ bộ nhớ)
        // Hàm đặc biệt của OpenCV là tạo nhanh một bức ảnh từ một mảng dữ liệu có sẵn trong RAM (không cần dùng vòng lặp for để chép từng số)
        // Ta có 1 và NA là số hàng và cột
        // Ta có fa là mảng số đưa vào để nạp cho ma trận A, lưu ý phải cùng kiểu dữ liệu. Để dùng cho hàm biến đổi Fourier của hàm OpenCV vì nó không nhận kiểu mảng
        using Mat B = Mat.FromPixelData(1, NA, MatType.CV_32FC1, fb);
        using Mat FA = new Mat(), FB = new Mat(), prod = new Mat(), corrM = new Mat();
        Cv2.Dft(A, FA, DftFlags.ComplexOutput);
        Cv2.Dft(B, FB, DftFlags.ComplexOutput);
        // Viết tắt của Discrete Fourier Transform (biến đổi Fourier rời rạc). Bóc tách hình vẽ hoa văn ngoằn ngoèo của đồng xu thành các con sóng tần số
        // Với A và B là 2 ma trận hoa văn của đồng xu
        // Với FA, FB là 2 ma trận kết quả sau khi qua biến đổi Fourier
        // Ta có DftFlags.ComplexOutput là xuất kết quả dạng số phức, gồm phần thực và phần ảo để lưu được độ lớn của sống và độ lệch pha của sóng
        Cv2.MulSpectrums(FA, FB, prod, DftFlags.None, conjB : true);
        // Ta có prod là thùng chứa kết quả lấy FA nhân với FB theo độ lớn còn góc là trừ nhau để ra độ lệch
        // Ta có prod chứa kết quả so khớp độ lệch góc giữa 2 đồng xu nhưng mắt thường nhìn vào sẽ không đọc hiểu được
        // Cho nên ta phải cần hàm này để dịch ngược toàn bộ kết quả trong prod thành mảng điểm số góc xoay bình thường (corrM)
        Cv2.Dft(prod, corrM, DftFlags.Inverse | DftFlags.RealOutput | DftFlags.Scale);
        // Ta có DftFlags.None có nghĩa là không bật bất kỳ chế độ đặc biệt nào cả, hãy nhân bình thường từ đầu đến cuối
        // Ta có conjB là conjugate of B (số phức liên hợp của B) tức là góc A + góc B nhưng conjugate của B sẽ là trừ thì nó sẽ là A - B sẽ ra góc lệch 
        // Ta có DftFlags.Inverse là biến đổi ngược, báo cho OpenCV biết là biến đổi từ tần số về góc xoay bình thường
        // Ta có DftFlags.ReadOutput là chỉ lấy phần thực bỏ phần ảo đi để làm điểm số chấm thi
        // Ta có quá trình biến đổi Fourier giống như cộng dồn 3600 điểm với nhau
        // Ta có DftFlags.Scale là chia kết quả cho 3600 sau khi biến đổi ngược, giúp các con số co về đúng giá trị thật 100%
        double[] corr = new double[NA]; // Tạo mảng một chiều chứa số thực và cấp phát bộ nhớ ban đầu cho mảng là 3600 số
        for (int i = 0; i < NA; i++) corr[i] = corrM.At<float>(0, i); // Ta có <float> là đọc ô đó dưới dạng số thực
        return corr;
    }
    
    // Tìm đỉnh và nội suy parabol để tìm góc xoay kèm chỉ số tin cậy
    static (double angle, double conf) PeakToAngle(double[] corr, double[] sa, double[] sb)
    {
        int k = 0;
        for (int i = 1; i < NA; i++) if (corr[i] > corr[k]) k = i;
        double y0 = corr[(k - 1 + NA) % NA], y1 = corr[k], y2 = corr[(k + 1) % NA]; // Lấy 3 điểm, một là đỉnh và 2 điểm liền kề
        double delta = 0.5 * (y0 - y2) / (y0 - 2 * y1 + y2 + 1e-12); // Tính phần số lẻ delta giúp xác định chính xác đỉnh thật sự của đỉnh đến từng phần trăm
        // Ta có 1e-12 tức là 1 nhân 10 mũ trừ 12 để cho mẫu số không bao giờ là 0 để không gây ra lỗi chia cho 0
        double angle = ((-(k + delta) * 360.0 / NA) % 360.0 + 360.0) % 360.0;
        // Ta có ((k + delta) * 360) / NA bi lệch bao nhiêu độ, thêm dấu - vô để kéo nó ngược về 
        // Ta có cụm % 360 để thu gọn góc về trong phạm vi một vòng tròn -359 đến +359
        // Ta có + 360 là kéo số âm lên thành số dương nhưng vô tình làm phình to quá mức
        // Ta có % 360 ở cuối là gọt bỏ phần phình to quá mức
        double na = Math.Sqrt(sa.Sum(v => v * v)), nb = Math.Sqrt(sb.Sum(v => v * v));
        // Với na, nb là độ dài của vector
        // Ta có v => v * v là quét từng phần tử trong mảng sb rồi lấy nó nhân với chính nó xong rồi cộng tổng lại
        return (angle, y1 / (na * nb + 1e-12)) // Kết quả là conf ~ 1 thì rất chắc, còn xa 1 thì thấp và nghi ngờ
    }
    
    // Trải hình tròn rồi gom bức ảnh 2D thành dãy số 1D sau đó tương quan vòng tròn bằng quét vòng tròn hoặc tương quan vòng tròn bằng biến đổi Fourier
    static (double angle, double conf)? EstimatePolar(Mat refG, Mat testG, bool useFFT)
    // Trả về góc mà tại đó có điểm tương quan cao nhất từ đó tính ra điểm tin cậy conf
    {
        var cr = FindCircle(refG);
        var ct = FindCircle(testG);
        if (cr == null || ct == null) return null;
        using Mat Pa = Unwrap(refG, cr.Value.center, cr.Value.radius) // Ta có using là tự động giải phóng bộ nhớ RAM của ma trận A khi hàm chạy xong (tránh rỉ bộ nhớ)
        using Mat Pb = Unwrap(testG, ct.Value.center, ct.Value.radius) // Ta có using là tự động giải phóng bộ nhớ RAM của ma trận A khi hàm chạy xong (tránh rỉ bộ nhớ)
        double[] sa = AngularSignature(Pa); // Gom bức ảnh 2D thành một dãy số 1D
        double[] sb = AngularSignature(Pb); // Gom bức ảnh 2D thành một dãy số 1D
        double[] corr = useFFT ? CirculaCorrFFT(sa, sb) : CircularCorrScan(sa, sb);
        return PeakToAngle(corr, sa, sb); // Hàm này trả về góc xoay và chỉ số tin cậy
        // Hàm này nhận đầu vào là refG là ảnh chuẩn tham chiếu còn testG là ảnh đê test
        // Dấu chấm hỏi cho phép nhận giá trị trả về có thể null
    }
    
    // Khử ánh sáng
    static Mat RemoveLighting(Mat gray)
    {
        
    }
}