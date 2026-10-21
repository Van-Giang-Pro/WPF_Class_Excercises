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
            run += sig[(i + half + 1) % NA] - sig[(i - half + NA) % NA]; // Dịch chuyển cửa sổ quét sang phải một bước để chuẩn bị cho vòng lặp tiếp theo
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
        double[] corr = new double[NA]; // Tạo mảng chứa điểm số khớp tương ứng với từng góc xoay
        for (int shift = 0; shift < NA; shift++) // Thử xoay đồng xu lần lượt qua từng góc một
        {
            double s = 0;
            for (int i = 0; i < NA; i++) s += sa[(i + shift) % NA] * sb[i]; // Lấy giá trị của đồng xu sa tại điểm i sau khi đã xoay một đoạn là shift
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
        Cv2.MulSpectrums(FB, FA, prod, DftFlags.None, conjB: true);
        // Nếu đảo 2 thông số FB và FA trước sau thì có thể cho ra góc âm dương đảo nhau, xem thêm phần trong ghi chú
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
        return (angle, y1 / (na * nb + 1e-12)); // Kết quả là conf ~ 1 thì rất chắc, còn xa 1 thì thấp và nghi ngờ
    }

    // Trải hình tròn rồi gom bức ảnh 2D thành dãy số 1D sau đó tương quan vòng tròn bằng quét vòng tròn hoặc tương quan vòng tròn bằng biến đổi Fourier
    static (double angle, double conf)? EstimatePolar(Mat refG, Mat testG, bool useFFT)
    // Trả về góc mà tại đó có điểm tương quan cao nhất từ đó tính ra điểm tin cậy conf
    {
        var cr = FindCircle(refG);
        var ct = FindCircle(testG);
        if (cr == null || ct == null) return null;
        using Mat Pa = Unwrap(refG, cr.Value.center, cr.Value.radius); // Ta có using là tự động giải phóng bộ nhớ RAM của ma trận A khi hàm chạy xong (tránh rỉ bộ nhớ)
        using Mat Pb = Unwrap(testG, ct.Value.center, ct.Value.radius); // Ta có using là tự động giải phóng bộ nhớ RAM của ma trận A khi hàm chạy xong (tránh rỉ bộ nhớ)
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
        Mat f = new Mat();
        gray.ConvertTo(f, MatType.CV_32F); // Chuyển đổi ảnh gray sang kiểu dữ liệu float 32 bit rồi lưu vào ảnh f, chuyê đổi vì 
        using Mat light = new Mat(); // Ta có using là tự động giải phóng bộ nhớ RAM của ma trận A khi hàm chạy xong (tránh rỉ bộ nhớ)
        Cv2.GaussianBlur(f, light, new Size(0, 0), 25); // Làm mờ ảnh, chi tiết ảnh nên sẽ còn lại ánh sáng nền
        // Vì light được tạo ra từ f nên nó cũng là kiểu float 32 bit luôn rồi detail cũng là float
        // Nếu không đổi về float thì sau khi trừ ra nó sẽ âm, thì mấy sẽ lưu là 0 cho nn phải lưu thành float
        Mat detail = new Mat();
        Cv2.Subtract(f, light, detail); // Nhấn ctrl Q để xem các thông số của hàm, ta có detail = f - light
        return detail;
    }

    // Ưóc lượng góc xoay bằng thuật toán Brute Force quét 360
    static double? EstimateBruteForce(Mat refG, Mat testG, bool fixLighting)
    {
        var cr = FindCircle(refG);
        var ct = FindCircle(testG);
        if (cr == null || ct == null) return null;
        int r = (int)Math.Min(cr.Value.radius, ct.Value.radius); // Lấy bán kính nhỏ nhất

        // Trong C#, khi bạn thêm dấu ? vào sau một Tuple hoặc Struct, C# sẽ tự động gói nó vào một cái hộp gọi là Nullable<T>
        // Ta có .HashValue là kiểm tra xem trong hộp có đồ hay đang rỗng
        // Ta có .Value là động tác mở hộp để lấy đồ thật bên trong ra
        Mat Crop(Mat g, Point2f c) // Ta có Point2f là kiểu dữ liệu của OpenCV để liêu tọa độ điểm (X, Y) dưới dạng số thực 
        // Bức ảnh gốc lớn cần được cắt vào
        {
            var rect = new Rect((int)c.X - r,  (int)c.Y - r, 2 * r, 2 * r); // Tạo một hình chữ nhật
            rect = rect.Intersect(new Rect(0, 0, g.Cols, g.Rows)); // Lấy giao nhau giữa 2 hình chữ nhật, cái nào lồi ra là bỏ đi, nằm trong là lấy
            // Ta có new Rect(0, 0, g.Cols, g.Rows) đã là toàn bộ phần viền của của bức ảnh gốc
            return new Mat(g, rect).Clone(); // Ta có thông số thứ 2 của hàm Mat là ROI
            // Còn ta có Clone là copy toàn bộ dữ liệu pixel của vùng vừa cắt ra một vùng nhớ RAM hoàn toàn mới, tách biệt 100% với ảnh gốc g
            // Ảnh cắt ra bây giờ là độc lập tự do, sau này ảnh gốc g có bị xóa đi chăng nữa thì ảnh cắt ra vẫn tồn tại an toàn, không bị lỗi vùng nhớ
            // Chúng ta lưu ý là cái rect là nó là hình chữ nhật vẽ từ tâm của hình tròn của nó và bán kính r min
            // Còn cái trong intersect là chính là cái tâm và dựa vào bán kính của hình tròn đó
        }

        using Mat A0 = Crop(refG, cr.Value.center); // Ta có using là tự động giải phóng bộ nhớ RAM của ma trận A khi hàm chạy xong (tránh rỉ bộ nhớ)
        using Mat B0 = Crop(testG, ct.Value.center); // Ta có using là tự động giải phóng bộ nhớ RAM của ma trận A khi hàm chạy xong (tránh rỉ bộ nhớ)
        using Mat A = fixLighting ? RemoveLighting(A0) : To32F(A0);
        using Mat B = fixLighting ? RemoveLighting(B0) : To32F(B0);

        using Mat mask = Mat.Zeros(A.Size(), MatType.CV_8UC1); // Ta có CV_8UC1 là 8 bit unsigned và có 1 channel
        Cv2.Circle(mask, new Point(r, r), r - 6, Scalar.All(255), -1); // Ta có r -6 là viền ngoài hình tròn có thể bị nhiễu, răng cưa
        // Ta có -1 là tô kín toàn bộ hình tròn
        double[] scores = new double[360]; // Tạo mảng cấp phát 360 vùng nhớ, mỗi vùng nhớ chứa kiểu số float
        for (int a = 0; a < 360; a++)
        {
            using Mat M = Cv2.GetRotationMatrix2D(new Point2f(r, r), a, 1.0); // Tính toán công thức tạo ra ma trận xoay để xoay bức hình
            // Với a là góc xoay còn 1.0 là scale giữ nguyên bức ảnh, không bóp méo thay đổi gì cả
            using Mat Ar = new Mat();
            Cv2.WarpAffine(A, Ar, M, A.Size()); // Hàm này là hàm thực sự cầm bức ảnh đi xoay với A.Size() là kích thước ảnh đầu ra
            scores[a] = MaskedNCC(Ar, B, mask); // Với Ar là ảnh A sau khi xoay, xong đem so sánh với ảnh B
        }

        int k = 0;
        for (int i = 1; i < 360; i++) if (scores[i] > scores[k]) k = i;
        double y0 = scores[(k + 359) % 360], y1 = scores[k], y2 = scores[(k + 1) % 360]; // Chia lấy dư cho 360 để tránh bị over mảng nếu chỉ số i là phần tử đầu hoặc cuối
        double d = 0.5 * (y0 - y2) / (y0 - 2 * y1 + y2 + 1e-12); // Công thức nội suy parabol, giá trị có thể bị âm và dao động từ -0.5 đến 0.5
        return ((k + d) % 360 + 360) % 360; // Ta có k là góc quay chẵn còn d là phần lẻ
    }

    // Đổi ảnh sang kểu dạng 32 bit float
    static Mat To32F(Mat g) // Dùng từ khóa static để có thể gọi hàm dùng ngay trực tiếp mà không cần phải khởi tạo đối tượng bằng từ khóa new
    {
        Mat f = new Mat();
        g.ConvertTo(f, MatType.CV_32F);
        return f;
    }

    // Masked NCC (Masked Normalized Cross Correlation) là hệ số tương quan chéo chuẩn hóa có mặt nạ
    // So sánh độ tương đồng của 2 bức ảnh trong vùng phạm vi nhất định
    static double MaskedNCC(Mat a, Mat b, Mat mask)
    {
        Scalar ma = Cv2.Mean(a, mask), mb = Cv2.Mean(b, mask); // Tính giá trị trung bình trong vùng mặt nạ
        // Scalar trong OpenCV là một kiểu dữ liệu dùng để chứa tối đa 4 con số thực, thường dùng để đại diện cho giá trị màu sắc hoặc kết quả tính toán của các kênh màu
        using Mat az = (a - ma).ToMat(), bz = (b - mb).ToMat();
        using Mat inv = new Mat();
        Cv2.BitwiseNot(mask, inv); // Kết quả được lưu vào inv
        // Hàm SetTo chỉ áp dụng cho mặt nạ vùng màu trắng mà thôi
        az.SetTo(0, inv);
        bz.SetTo(0, inv); // Ảnh az và bz đều có vùng xung quanh vòng tròn là màu đen
        double dot = Cv2.Sum(az.Mul(bz)).Val0; // Ta có Cv2.Sum là kiểu dữ liệu scalar, lấy ra val0 là kênh màu ảnh mức xám
        double na = Math.Sqrt(Cv2.Sum(az.Mul(az)).Val0);
        double nb = Math.Sqrt(Cv2.Sum(bz.Mul(bz)).Val0);
        return dot / (na * nb + 1e-9);
    }

    // ORB (Oriented FAST and Rotated BRIEF) dịch ra là bắt điểm góc cực nhanh và nhận diện điểm đó dù vật thể có bị xoay tròn ở bất kỳ góc nào
    // Bầu chọn góc
    static double? EstimateByORB(Mat refG, Mat testG)
    {
        using var orb = ORB.Create(nFeatures: 2000); // Khởi tạo bộ xử lý thuật toán ORB và ra lệnh tìm tối đa 2000 đặc trưng cuả bức ảnh
        // Ta có ORB là một thuật toán cua OpenCV giúp tìm các điểm mốc nổi bật trên ảnh, tạo mã nhận diện cho từng điểm đó
        // Và giúp máy tính so sánh xem 2 bức ảnh có cùng một vật thể hay không, và vật thể đó bị xoay bao nhiêu độ
        using Mat d1 = new Mat(), d2 = new Mat();
        orb.DetectAndCompute(refG, null, out KeyPoint[] k1, d1);
        // Ta có KeyPoint là kiểu dữ liệu của OpenCV dùng để lưu trữ thông tin của một đắc trưng, nó là một struct, nó có nhiều thông số trong đó
        // Ta có refG là bức ảnh bạn muốn quét để tìm đặc trưng
        // Ta có null là mặt nạ
        // Ta có d1 là ma trận đầu ra chưa dấu vân tay của đặt trưng, dùng để so sánh với các đặc trưng khác xem có giống hay không
        orb.DetectAndCompute(testG, null, out KeyPoint[] k2, d2);
        if (d1.Empty() || d2.Empty()) return null;
        using var matcher = new BFMatcher(NormTypes.Hamming, crossCheck: true);
        // Ta có BFMatcher là Brute Force Matcher là bộ thuật toán brute force, cầm dấu vân tay của từng đặc trưng đem đi so sánh, có 2000 đặc trưng
        // Ta có NormTypes.Hamming là khoảng cách Hamming chỉ đơn giản là đếm xem có bao nhiêu số bit khác nhau giữa 2 chuỗi (dùng phép toán XOR ở phần cứng CPU)
        // Càng ít bit khác nhau thì 2 điểm đó càng giống nhau. Cách đo này chạy siêu nhanh, tốn cực ít CPU
        // Ta có crossCheck là kiểm tra 2 chiều, điểm A ảnh 1 có giống điểm A ảnh 2 không và ngược lại
        DMatch[] ms = matcher.Match(d1, d2); // Ra lệnh cho bộ so sánh so sánh những dấu vân tay của nhưng đặc trưng ở 2 ảnh, trả về cặp ảnh giống nhau
        if (ms.Length < 8) return null; // Ta có ms là một mảng chứa nhiều struct nó chứa thông số distance cho biết độ sai lệch giữa 2 dấu vân tay
        var votes = ms.OrderBy(m => m.Distance).Take(200).Select(m => ((k1[m.QueryIdx].Angle - k2[m.TrainIdx].Angle) % 360 + 360) % 360).ToList();
        // Ta có cách viết này là theo kiêu lamda trong C#, sắp xếp độ sai lệch lại theo từ thấp tới lớn nhất, viết take(200) được là nhờ bộ thư viện Linq
        int[] hist = new int[72];
        foreach (var v in votes) hist[(int)(v / 5) % 72]++; // Số 72 là chia thành 72 khoảng góc, mỗi khoảng được 5 độ, chia cho 5 để xem giá trị v thuộc khoảng nào, mỗi lần có cái nào giống nhau thì nó cộng lên
        // Kết quả là ta có một histogram cho 72 khoảng
        int peak = Array.IndexOf(hist, hist.Max()); // Muốn tìm một phần tử trong mảng thì phải dùng Array.IndexOf()
        double center = peak * 5 + 2.5; // Nhân 5 để ra mép bắt đầu của khoảng góc đó, rồi cộng 2.5 là phân nửa khoảng góc của đoạn đó
        var inl = votes.Where(v => CircDiff(v, center) < 7.5).ToList(); // Khoảng cách so với tâm center đã tìm trước đó nếu nhỏ hơn 7.5 thì thêm vào list
        if (inl.Count == 0) return null; // Ta có inl là danh sách các góc đã được lọc sạch
        double s = inl.Sum(v => Math.Sin(v * Math.PI / 180)); // Ta có cụm này v * Math.PI / 180 đổi từ đơn vị độ sang radian
        double c = inl.Sum(v => Math.Cos(v * Math.PI / 180)); // Tìm hiểu về mean of circular quantities
        return (Math.Atan2(s, c) * 180 / Math.PI + 360) % 360; // Ta có 180 / Math.PI là đổi ra độ
        // Ta có độ bằng radian nhân 180 độ chia cho pi
        // Ta có + 360 để kéo về góc dương, % 360 là để quy ra khoảng 0 đến 360
    }

    static double CircDiff(double a, double b) // Tính khoảng cách góc lệch ngắn nhất giữa 2 góc trong một vòng tròn khép kín
    {
        double d = Math.Abs(a - b) % 360; // Chia 360 sợ trường hợp góc xoay lớn hơn 360 độ
        return Math.Min(d, 360 - d); // Có 2 đường đi, lấy đường vòng lại hoặc chạy tới
    }

    // Chương trình chính
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; // Quy định chuỗi in ra màn hình
        string root = "C:\\Users\\fs120806\\Desktop\\Document\\Programing Project\\Dot_Net_Project\\Library\\Rotation_Checking";
        var refs = "ABC".ToDictionary(b => b.ToString(), b => Cv2.ImRead($"{root}/refs/base_{b}_ref.png", ImreadModes.Grayscale));
        // Ta có "ABC" là một chuỗi string gồm nhiều ký tự char ghép lại
        // Ta có ToDictionary là chuyển đổi một danh sách thành cuốn từ điển
        var labels = File.ReadAllLines($"{root}/labels.csv").Skip(1) // Ta có skip(1) là bỏ qua dòng đầu tiên
            .Select(l => l.Split(','))
            .Select(p => (file: p[0], baseId: p[1], gt: double.Parse(p[2]))) // Ta có gt là ground truth là góc chuẩn
            .ToList();
        // Ta có trước khi gọi ToList() thì kết quả của các hàm LINQ này (skip, select) trả về một kiểu dữ liệu gọi là IEnumerable<...>
        // Sau đó gọi ToList() thì chuỗi lệnh đó sẽ được thực thi, nếu không gọi thì nó sẽ không thực thi và được lưu vào list
        
        // Demo 4 ảnh chỉ định trong bài giảng
        Console.WriteLine("Demo 4 Ảnh Chỉ Định Dùng Phương Pháp Trải Hình Tròn Và Tìm Tương Quan Bằng Phương Pháp Biến Đổi Fourier");
        foreach (var f in new[] { "base_A_003.jpg", "base_A_000.jpg", "base_B_002.jpg", "base_C_005.jpg" })
        {
            var row = labels.First(l => l.file == f); // Tìm phần tử đầu tiên trong list thỏa mãn điều kiện
            using Mat t = Cv2.ImRead($"{root}/test/{f}", ImreadModes.Grayscale);
            var r = EstimatePolar(refs[row.baseId], t, useFFT: true);
            Console.WriteLine(r == null
                ? $"{f} : Không Tìm Thấy Hình Tròn"
                : $"{f} : Góc Thật = {row.gt,8:F3} Máy Đoán = {r.Value.angle,8:F3} " +
                  $"Sai Số = {CircDiff(r.Value.angle, row.gt):F3} Độ Tin Cậy = {r.Value.conf:F3}");
            // Ta có 8 là dành ra 8 độ rộng để in con số này nếu không đủ thì thêm khoảng trắng, 3 là lấy 3 chữ số thập phân
        }
        
        // Benchmark
        void Bench(string name, Func<Mat, Mat, double?> fn, int step = 1)
        // Func là một kiểu dữ liệu dùng để truyền vào một hàm làm tham số của một hàm khác (Delegate)
        // Nhận vào 2 ảnh và trả về kiểu double
        {
            var errs = new List<double>();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int n = 0;
            for (int i = 0; i < labels.Count; i += step)
            {
                var row = labels[i];
                using Mat t = Cv2.ImRead($"{root}/test/{row.file}", ImreadModes.Grayscale);
                double? est = fn(refs[row.baseId], t);
                errs.Add(est == null ? 180 : CircDiff(est.Value, row.gt));
                n++;
            }
            sw.Stop();
            errs.Sort(); // Sắp xếp tăng dần
            double acc1 = errs.Count(e => e < 1) * 100 / n;
            double acc3 = errs.Count(e => e < 3) * 100 / n;
            Console.WriteLine($"{name, -50} n = {n, 3} median = {errs[n / 2], 15:F3} " +
                              $"acc@1 = {acc1, 15:F1}% " +
                              $"acc@3 = {acc3, 15:F1}% " +
                              $"{sw.ElapsedMilliseconds / (double)n, 15:F1}"); 
            // Ta có -34 là canh lề trái còn 3 là độ rộng bên phải
            // Tốc độ xử lý trên một bức ảnh
        }
        
        // In ra màn hình
        Console.WriteLine("\nBenchmark Trên Bộ Dữ Liệu");
        Bench("Phương Pháp Trải Tròn Với Quét Trượt", (a, b) => EstimatePolar(a, b, false)?.angle); // Tham khảo thêm về lamda expression 
        Bench("Phương Pháp Trải Tròn Với Biến Đổi Fourier", (a, b) => EstimatePolar(a, b, true)?.angle);
        Bench("Phương Pháp ORB Và Bầu Chọn", EstimateByORB);
        if (args.Contains("full"))
        {
            Bench("Phương Pháp Brute Force (Không Khử Sáng)", (a, b) => EstimateBruteForce(a, b, false), step : 6); // Số 6 là cứ 6 bức ảnh sẽ lấy ra một bức ảnh để test
            Bench("Phương Pháp Brute Force (Có Khử Sáng)", (a, b) => EstimateBruteForce(a, b, true), step : 6);
        }
    }
}

    
    
    
    