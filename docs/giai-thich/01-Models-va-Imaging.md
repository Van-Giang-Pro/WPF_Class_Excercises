# QUYỂN 1 — Models & Imaging (tầng dữ liệu)

> Giải thích từng dòng: `Geometry.cs`, `Regions.cs`, `Results.cs`, `VisionBlob.cs`, `IVisionImage.cs`, `MatVisionImage.cs`
>
> Quy ước: 🔴 = code hiện tại của bạn đang sai ở chỗ này.

---

## 1. `VisionFlow.Core/Models/Geometry.cs`

```csharp
namespace VisionFlow.Core.Models;
```
**Dòng 1** — Mọi thứ trong file thuộc "địa chỉ" `VisionFlow.Core.Models`. Dấu `;` cuối = file-scoped namespace, áp cho cả file (khỏi cần ngoặc `{}` bao quanh như C# cũ).

```csharp
public class Geometry
{
    public enum Judge
    {
        None,
        OK,
        NG
    }
}
```
**Dòng 3-11** 🔴 — `enum Judge` là kiểu liệt kê: một biến kiểu `Judge` chỉ được mang đúng 1 trong 3 giá trị. Bên dưới, compiler gán số nguyên: `None`=0, `OK`=1, `NG`=2. Ý nghĩa nghiệp vụ: kết quả phán định của tool — `None` = chưa chạy, `OK` = đạt, `NG` (No Good) = không đạt.

**Vấn đề:** enum này đang bị **lồng bên trong** class `Geometry`. Hậu quả: file khác muốn dùng phải viết `Geometry.Judge` chứ không viết `Judge` được — mà `Results.cs` của bạn lại viết `Judge` trần → lỗi biên dịch "The type or namespace name 'Judge' could not be found". Class `Geometry` không chứa gì khác ngoài enum này, nên sửa bằng cách **xóa vỏ class, đưa enum ra ngoài**:

```csharp
public enum Judge
{
    None,
    OK,
    NG
}
```

```csharp
public readonly record struct Point2d(double X, double Y);
```
**Dòng 13** — Dòng quan trọng nhất file. Tách từng chữ:
- `public` — ai cũng dùng được.
- `readonly` — bất biến: tạo xong là không sửa được `X`, `Y` nữa. Muốn "đổi" thì tạo cái mới.
- `record` — kiểu "bản ghi": so sánh theo **giá trị** (`new Point2d(1,2) == new Point2d(1,2)` là `true`), tự có `ToString()` in đẹp: `Point2d { X = 1, Y = 2 }`.
- `struct` — kiểu giá trị: nằm trên stack, không tốn heap, gán là copy. Hợp với dữ liệu nhỏ như tọa độ (2 số double = 16 byte).
- `(double X, double Y)` — **positional syntax**: một cú pháp sinh ra 3 thứ: constructor `new Point2d(3, 5)`, property chỉ-đọc `X`, property chỉ-đọc `Y`.
- Dùng `double` chứ không phải `int` vì machine vision cần độ chính xác **sub-pixel** (tâm hình tròn có thể ở tọa độ 315.274, không phải 315).

```csharp
public readonly record struct Circle(Point2d Center, double Radius);
```
**Dòng 15** — Hình tròn = tâm + bán kính. Chú ý record dùng record khác làm field: `Center` là `Point2d`. Truy cập lồng: `circle.Center.X`.

```csharp
public readonly record struct LineSegment(Point2d Start, Point2d End);
```
**Dòng 17** — Đoạn thẳng = 2 điểm đầu-cuối. Kết quả của tool dò cạnh (edge/line finder).

```csharp
public readonly record struct RectRegion(double X, double Y, double Width, double Height);
```
**Dòng 19** — Hình chữ nhật **thẳng trục** (không xoay): góc trên-trái `(X, Y)` + kích thước. Dùng làm bounding box, vùng ROI đơn giản.

```csharp
public readonly record struct XYTOffset(double X, double Y, double Theta);
```
**Dòng 21** — Độ lệch vị trí: lệch ngang `X`, lệch dọc `Y`, lệch góc xoay `Theta` (đơn vị thường là độ). Đây là output của tool **alignment**: "vật đang lệch khỏi vị trí chuẩn bao nhiêu" → gửi cho robot/trục máy bù lại.

---

## 2. `VisionFlow.Core/Models/Regions.cs`

```csharp
namespace VisionFlow.Core.Models;
```
**Dòng 1** — Cùng namespace với Geometry.cs → hai file thấy nhau tự nhiên, không cần `using`.

```csharp
public readonly record struct RotationRectRegion(Point2d Center, double Width, double Height, double Theta);
```
**Dòng 3** — Chữ nhật **có xoay**: định nghĩa bằng tâm + rộng/cao + góc `Theta`. Khác `RectRegion` (định nghĩa bằng góc trên-trái, không xoay). Dùng khi vật trên băng chuyền nằm nghiêng.

```csharp
public readonly record struct CircleRegion(Point2d Center, double Radius);
```
**Dòng 5** — Vùng ROI hình tròn. Nhìn giống hệt `Circle` ở Geometry.cs nhưng **cố tình tách làm 2 kiểu**: `Circle` là *kết quả đo được*, `CircleRegion` là *vùng người dùng vẽ để giới hạn tìm kiếm*. Hai vai trò khác nhau → hai kiểu khác nhau, sau này thêm field riêng cho từng bên không vướng nhau.

```csharp
public sealed record PolygonRegion(IReadOnlyList<Point2d> Points);
```
**Dòng 7** — Vùng đa giác tự do (danh sách đỉnh). Để ý: đây là `record` **không có `struct`** → là class, nằm trên heap. Lý do: nó chứa `IReadOnlyList` (một danh sách, kích thước không biết trước, có thể hàng trăm điểm) — dữ liệu lớn/không cố định thì để heap, không nhét vào struct. `sealed` = cấm kế thừa tiếp (khóa thiết kế, compiler cũng tối ưu tốt hơn).
- `IReadOnlyList<Point2d>` = "danh sách điểm **chỉ đọc**": người nhận chỉ được duyệt/đếm, không `.Add`/`.Remove` được → không ai sửa trộm hình dạng vùng sau khi tạo.

```csharp
public readonly record struct CaliperRegion(Point2d Center, double Width, double Height, double AngleDeg, int CaliperCount);
```
**Dòng 9** — Vùng **caliper** (thước cặp) — khái niệm kinh điển của Cognex: một dải chữ nhật xoay, bên trong rải `CaliperCount` "thước đo" nhỏ song song; mỗi thước quét tìm 1 điểm cạnh → fit thành đường thẳng/hình tròn. `AngleDeg` = góc đặt dải (độ).

```csharp
public sealed record TemplateImageRef(RotationRectRegion? SourceRegion = null, string? FilePath = null);
```
**Dòng 11** — Tham chiếu đến **ảnh mẫu** (template) cho tool so khớp mẫu. Hai cách cung cấp mẫu, chọn 1:
- `SourceRegion` — cắt mẫu từ một vùng trên ảnh đang chạy.
- `FilePath` — nạp mẫu từ file trên đĩa.
Cú pháp đáng học ở dòng này:
- `RotationRectRegion?` — dấu `?` sau kiểu: giá trị này **được phép null** (nghĩa là "không dùng cách này").
- `= null` — **giá trị mặc định của tham số**: cho phép gọi `new TemplateImageRef()` (cả hai null), `new TemplateImageRef(FilePath: "mau.png")` (chỉ đường dẫn — cú pháp `Tên: giá_trị` gọi là *named argument*, chỉ định đích danh tham số nào).

---

## 3. `VisionFlow.Core/Models/Results.cs`

```csharp
public abstract class VisionResult
{
    public Judge Judge { get; set; } = Judge.None;
}
```
**Dòng 3-6** — Class **cha** của mọi kết quả.
- `abstract` = không được `new VisionResult()` trực tiếp — nó chỉ tồn tại để làm gốc chung. Chỉ tạo được class con (`CircleResult`...).
- `public Judge Judge` — property tên `Judge`, kiểu `Judge` (C# cho phép tên property trùng tên kiểu).
- `{ get; set; }` — đọc/ghi tự do: tool chạy xong sẽ gán `result.Judge = Judge.OK;`.
- `= Judge.None` — giá trị khởi tạo: kết quả mới sinh ra ở trạng thái "chưa phán định".
- **Ý đồ thiết kế:** vì MỌI kết quả đều kế thừa `VisionResult`, chỗ nào trong hệ thống cũng có thể hỏi `result.Judge` mà không cần biết đó là kết quả loại gì → màn hình tổng chỉ cần duyệt list `VisionResult` là biết OK/NG toàn trạm.
- 🔴 Dòng này chỉ compile khi bạn đưa enum `Judge` ra khỏi class `Geometry` (xem mục 1).

```csharp
public sealed class CircleResult : VisionResult
{
    public Circle Circle { get; set; }
    public double Score { get; set; }
}
```
**Dòng 8-13** — Kết quả của tool tìm hình tròn.
- `: VisionResult` — kế thừa: tự động có sẵn property `Judge` từ cha, cộng thêm 2 property riêng.
- `sealed` — cấm ai kế thừa tiếp `CircleResult` (cây kế thừa chỉ sâu 2 tầng, giữ thiết kế đơn giản).
- `Circle` — hình tròn đo được (tâm + bán kính, kiểu từ Geometry.cs).
- `Score` — điểm tin cậy của phép khớp, thường 0..1 hoặc 0..100: "tôi chắc chắn bao nhiêu % đây đúng là hình tròn".

```csharp
public sealed class LineResult : VisionResult
{
    public LineSegment Segment { get; set; }
    public double AngleDeg { get; set; }
}
```
**Dòng 15-20** — Kết quả tìm đường thẳng: đoạn thẳng tìm được + góc nghiêng của nó (độ). Góc tách riêng vì hầu hết ứng dụng chỉ cần góc (kiểm tra vật có nằm thẳng không).

```csharp
public sealed class AlignResult : VisionResult
{
    public XYTOffset Offset { get; set; }
}
```
**Dòng 22-25** — Kết quả căn chỉnh: chỉ cần bộ 3 số lệch X-Y-Theta.

---

## 4. `VisionFlow.Core/Models/VisionBlob.cs`

"Blob" = **vùng pixel liên thông** sau khi phân ngưỡng ảnh (ví dụ: sau khi tách "mọi pixel sáng hơn 128", mỗi cụm dính liền nhau là 1 blob = 1 vật thể/khuyết tật). Class này là "hồ sơ lý lịch" của 1 blob.

```csharp
public sealed class VisionBlob
```
**Dòng 3** — `sealed class` (không phải record/struct) vì blob có nhiều field + chứa danh sách contour → để heap, và thực tế không cần so sánh 2 blob bằng giá trị.

```csharp
    public int Id { get; set; }
```
**Dòng 5** — Số thứ tự blob trong ảnh (blob 0, 1, 2...) để hiển thị/tra cứu.

```csharp
    public Point2d CenterOfMass { get; set; }
    public RectRegion BoundingBox { get; set; }
    public double Area { get; set; }
    public double Perimeter { get; set; }
```
**Dòng 6-9** — Nhóm "vị trí & kích thước":
- `CenterOfMass` — trọng tâm (trung bình tọa độ mọi pixel trong blob). Dùng để định vị vật.
- `BoundingBox` — chữ nhật nhỏ nhất bao trọn blob.
- `Area` — diện tích = số pixel (double vì có thể quy đổi ra mm²).
- `Perimeter` — chu vi đường biên.

```csharp
    public double Orientation { get; set; }
    public double MajorAxisLength { get; set; }
    public double MinorAxisLength { get; set; }
    public double AspectRatio { get; set; }
```
**Dòng 11-14** — Nhóm "hình dạng qua ellipse tương đương" (fit 1 ellipse trùm lên blob):
- `Orientation` — góc trục dài của ellipse = vật đang nằm nghiêng bao nhiêu độ.
- `MajorAxisLength` / `MinorAxisLength` — độ dài trục dài / trục ngắn.
- `AspectRatio` — tỉ lệ dài/ngắn: gần 1 = tròn/vuông, lớn = thon dài (que, sợi).

```csharp
    public double Roundness { get; set; }
```
**Dòng 16** — Độ tròn, công thức phổ biến `4π·Area / Perimeter²`: hình tròn hoàn hảo = 1.0, càng méo/răng cưa càng nhỏ. Dùng lọc: "chỉ lấy blob tròn > 0.8" để tìm lỗ khoan chẳng hạn.

```csharp
    public double Solidity { get; set; }
```
**Dòng 18** — Độ đặc = `Area / diện tích bao lồi (convex hull)`. Vật đặc lành lặn ≈ 1.0; vật bị khuyết, lõm, nứt → nhỏ hơn hẳn. Chỉ số bắt khuyết tật rất mạnh.

```csharp
    public IReadOnlyList<Point2d> Contour { get; set; } = Array.Empty<Point2d>();
```
**Dòng 20** — Đường biên của blob = chuỗi điểm nối liền, dùng để vẽ overlay lên ảnh cho người vận hành xem.
- `= Array.Empty<Point2d>()` — khởi tạo bằng **mảng rỗng dùng chung** thay vì `null`. Hai lợi ích: (1) không bao giờ null → code duyệt `foreach (var p in blob.Contour)` không cần check null; (2) `Array.Empty<T>()` trả về đúng 1 mảng rỗng cache sẵn toàn chương trình — không tốn bộ nhớ tạo mảng rỗng mới mỗi lần.

---

## 5. `VisionFlow.Core/Imaging/IVisionImage.cs`

```csharp
public interface IVisionImage : IDisposable
{
    int Width { get; }
    int Height { get; }
    int Channels { get; }
    bool IsDisposed { get; }
    IVisionImage Clone();
}
```
- **Dòng 3** — `interface` = bản hợp đồng: chỉ kê khai, không chứa code chạy. `: IDisposable` = kèm luôn hợp đồng dọn bộ nhớ của .NET (ai ký phải có thêm `void Dispose()`).
- **Dòng 5-7** — 3 nghĩa vụ: có `Width`, `Height` (kích thước ảnh, pixel), `Channels` (số kênh màu: 1=xám, 3=màu BGR, 4=BGRA). Trong interface không viết `public` (mặc định public), không có body (không nói *tính thế nào*, chỉ nói *phải có*), chỉ đòi `{ get; }` (*đọc được* là đủ).
- **Dòng 8** — `IsDisposed`: cờ "ảnh đã bị giải phóng chưa" để nơi khác kiểm tra trước khi dùng, tránh crash.
- **Dòng 9** — `Clone()`: hàm (có `()`), trả về `IVisionImage` — copy sâu ra một ảnh độc lập. Trả về *interface* chứ không phải class cụ thể → người gọi không cần biết ruột là gì.
- **Tư tưởng:** tầng Core làm việc với "ảnh" thuần khái niệm, không biết OpenCV tồn tại → Core không cần cài OpenCV, sau đổi thư viện ảnh chỉ sửa tầng Tools.

---

## 6. `VisionFlow.Tools/Imaging/MatVisionImage.cs`

```csharp
using OpenCvSharp;
using VisionFlow.Core.Imaging;
```
**Dòng 1-2** — Nhập 2 "địa chỉ": `OpenCvSharp` để dùng class `Mat`; `VisionFlow.Core.Imaging` để dùng `IVisionImage`. Dòng 2 chạy được nhờ project Tools có **reference** sang project Core (khai trong file `.csproj`).

`Mat` (Matrix) là kiểu ảnh của OpenCV: ảnh = ma trận số (ảnh xám 640×480 = ma trận 480 hàng × 640 cột, mỗi ô là độ sáng 0-255; ảnh màu thì mỗi ô có 3 số B,G,R).

```csharp
public class MatVisionImage : IVisionImage
```
**Dòng 6** — "Class `MatVisionImage`, **ký hợp đồng** `IVisionImage`". Từ lúc có dấu `:` này, compiler mở checklist 6 nghĩa vụ ra soi, thiếu cái nào báo lỗi cái đó. Và cũng nhờ nó, câu `IVisionImage img = new MatVisionImage(mat);` mới hợp lệ.

```csharp
    public MatVisionImage(Mat mat)
    {
        Mat = mat ?? throw new ArgumentNullException(nameof(mat));
    }
```
**Dòng 8-11** — Constructor (trùng tên class, không có kiểu trả về, chạy đúng 1 lần lúc `new`). Dòng 10 tách từng mảnh:
- `Mat` (hoa) = property của class; `mat` (thường) = tham số. C# phân biệt hoa/thường.
- `a ?? b` = "a khác null thì lấy a, null thì lấy b".
- Vế b ở đây là `throw new ArgumentNullException(nameof(mat))` — **ném lỗi ngay**, object không được tạo. `nameof(mat)` = chuỗi `"mat"` (tên biến dạng text, để thông báo lỗi chỉ đích danh tham số nào null; dùng `nameof` thay vì gõ `"mat"` tay để khi rename biến, chuỗi tự đổi theo).
- Kỹ thuật này gọi là **guard clause**: chặn dữ liệu rác ngay tại cửa. Nếu lặng lẽ nhận null, lỗi sẽ nổ muộn ở một dòng `Mat.Width` nào đó rất xa nơi gây lỗi — cực khó debug.

```csharp
    public Mat Mat { get; }
```
**Dòng 13** — "Cái két" giữ ảnh OpenCV thật. `{ get; }` = chỉ gán được trong constructor, sau đó khóa — không ai tráo ruột giữa chừng.

```csharp
    public int Width => Mat.Width;
    public int Height => Mat.Height;
    public int Channels => Mat.Channels();
    public bool IsDisposed => Mat.IsDisposed;
```
**Dòng 15-18** — Thực hiện 4 nghĩa vụ đầu bằng cách **chuyển lời** cho `Mat` bên trong. `=>` = property tính toán: mỗi lần được hỏi mới đi hỏi lại `Mat`, không lưu gì. Chi tiết tinh: `Mat.Height` không có `()` (OpenCvSharp để nó là property) còn `Mat.Channels()` có `()` (OpenCvSharp để nó là hàm) — vỏ bọc của ta che sự thiếu nhất quán đó, bên ngoài chỉ thấy 2 property đều nhau. Cả class là một **adapter/wrapper**: vỏ mỏng phiên dịch giữa 2 thế giới.

```csharp
    public IVisionImage Clone() => new MatVisionImage(Mat.Clone());
```
**Dòng 20** — Nghĩa vụ 5. Đọc từ trong ra: `Mat.Clone()` = OpenCV copy toàn bộ ma trận pixel (copy sâu thật sự, tốn RAM) → bọc bản copy vào `MatVisionImage` mới → trả ra dạng `IVisionImage`. Dấu `=>` ở đây là **hàm viết gọn 1 dòng**, tương đương `{ return ...; }`.

```csharp
    public void Dispose()
    {
        if (!Mat.IsDisposed) Mat.Dispose();
    }
```
**Dòng 22-25** — Nghĩa vụ 6 (từ `IDisposable`). `!` = phủ định. Cái `if` giúp gọi `Dispose()` 2 lần không sao (lần 2 bỏ qua). **Vì sao sống còn:** ma trận ảnh nằm ở bộ nhớ **native C++** — garbage collector của .NET không nhìn thấy để tự dọn. Máy chụp 10 ảnh/giây mà quên dispose = tràn RAM sau vài giờ chạy. Cách dùng chuẩn: `using var img = new MatVisionImage(mat);` — hết khối lệnh tự gọi `Dispose()`.

```csharp
public static class VisionImageExtensions
{
    public static Mat AsMat(this IVisionImage image)
    {
        if (image is MatVisionImage m) return m.Mat;
        throw new InvalidOperationException($"Ảnh không phải MatVisionImage (thực tế: {image.GetType().Name})");
    }
}
```
**Dòng 28-35** — Extension method, nằm **ngoài** class trên (bắt buộc: extension phải ở static class cấp cao nhất, không được lồng).
- `static class` = hộp đựng hàm, không bao giờ `new`.
- Chữ **`this`** trước tham số đầu là chìa khóa: nó "độn" hàm `AsMat` vào mọi `IVisionImage` → chỗ khác gọi được `image.AsMat()` thay vì `VisionImageExtensions.AsMat(image)`.
- `if (image is MatVisionImage m) return m.Mat;` — pattern matching 3-trong-1: kiểm tra kiểu thật của `image` + nếu đúng thì ép kiểu + đặt tên `m`, rồi trả về ruột `m.Mat`.
- Dòng `throw`: rơi xuống đây = ảnh không phải loại OpenCV → báo lỗi rõ ràng. `$"..."` = string interpolation (nhét biểu thức trong `{}` vào chuỗi); `image.GetType().Name` = tên class thật lúc runtime, để debug biết ngay "ảnh gì lạc vào đây".
- **Vai trò:** tool nhận `IVisionImage` (trung lập) nhưng cần `Mat` để gọi OpenCV. `AsMat()` là **cây cầu duy nhất** được phép mở vỏ — mọi chỗ khác trong Tools cấm đụng chữ `Mat` trực tiếp.
