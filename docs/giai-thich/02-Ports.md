# QUYỂN 2 — Ports.cs (hệ thống cổng dữ liệu)

> File khó nhất về cú pháp. Đọc chậm, mỗi khối một hơi thở.
>
> Quy ước: 🔴 = code hiện tại của bạn đang sai ở chỗ này.

## Bức tranh lớn trước khi vào từng dòng

Mỗi tool có các **cổng**: cổng vào nhận dữ liệu, cổng ra phát dữ liệu. Engine nối `OutputPort` của tool A vào `InputPort` của tool B — như cắm dây tín hiệu giữa 2 thiết bị:

```
[GrabImageTool]──Image(out)──►(in)Image──[ThresholdTool]──Binary(out)──►...
```

Vấn đề thiết kế phải giải: **engine nối dây không biết kiểu dữ liệu lúc compile** (nó đọc sơ đồ từ file cấu hình lúc runtime), nhưng **code viết tool lại muốn kiểu an toàn** (gán nhầm kiểu là compiler báo ngay). Giải pháp = mỗi port có 2 "mặt": mặt generic `T` cho tool, mặt `object` cho engine. Toàn bộ sự phức tạp của file này sinh ra từ đó.

---

## Từng dòng

```csharp
namespace VisionFlow.Core.Ports;
```
**Dòng 1** — Địa chỉ gói. (Xem Quyển 1 nếu quên.)

```csharp
public enum PortDirection
{
    Input,
    Output
}
```
**Dòng 3-7** — Enum 2 giá trị: cổng này là chiều vào hay chiều ra. UI cần nó để vẽ cổng bên trái (in) hay bên phải (out) của node; engine cần nó để cấm nối in-với-in.

```csharp
public interface IPort
{
```
**Dòng 9-10** — Hợp đồng chung cho MỌI cổng, bất kể chiều và kiểu dữ liệu. Ai cầm `IPort` là làm việc được với mọi cổng — đây là "mặt object" cho engine.

```csharp
    string Name { get; }
```
**Dòng 11** — Tên máy của cổng, ví dụ `"Image"` — dùng làm khóa tra cứu khi nối dây, không đổi.

```csharp
    string DisplayName { get; }
```
**Dòng 12** — Tên hiển thị cho người dùng, ví dụ `"Ảnh vào"` — được phép đẹp, có dấu, đổi thoải mái mà không hỏng sơ đồ nối dây (vì nối dây bám theo `Name`).

```csharp
    Type DataType { get; }
```
**Dòng 13** — Cổng này chở dữ liệu kiểu gì? `Type` là class của .NET **mô tả một kiểu** (metadata). Engine dùng để kiểm tra trước khi nối: `output.DataType == input.DataType` — không cho cắm dây "ảnh" vào lỗ "số".

```csharp
    PortDirection Direction { get; }
```
**Dòng 14** — Chiều của cổng (enum vừa khai báo ở trên).

```csharp
    object? Value { get; set; }
```
**Dòng 15** — Giá trị đang nằm trên cổng, dưới dạng `object?` — kiểu "mọi thứ đều được, kể cả null". Vì `object` là tổ tiên của mọi kiểu trong .NET, engine copy giá trị giữa 2 cổng bất kỳ chỉ bằng 1 câu:
```csharp
inputPort.Value = outputPort.Value;   // không cần biết bên trong là ảnh hay số
```
Đây chính là "mặt object". Nhược điểm của nó (mất an toàn kiểu) sẽ được vá ở phần class bên dưới.

```csharp
public interface IInputPort : IPort
{
    bool IsOptional { get; }
}
```
**Dòng 18-21** — Hợp đồng cổng vào = hợp đồng chung (`: IPort` — interface kế thừa interface, cộng dồn nghĩa vụ) **cộng thêm** 1 điều khoản riêng: `IsOptional` — cổng này có được phép bỏ trống không? Ví dụ cổng "Mask" (mặt nạ che vùng bỏ qua) là tùy chọn; cổng "Image" là bắt buộc. Hàm `ValidateInputs()` trong `VisionTool` sẽ đọc cờ này: cổng bắt buộc mà trống → ném lỗi trước khi chạy.

```csharp
public interface IOutputPort : IPort
{

}
```
**Dòng 23-26** — Hợp đồng cổng ra: chưa có điều khoản riêng nào, thân rỗng. Hỏi hay: **rỗng thì khai làm gì?** Ba lý do: (1) để khai kiểu tường minh `List<IOutputPort>` thay vì `List<IPort>` — người đọc code biết ngay danh sách này chỉ chứa cổng ra; (2) compiler chặn được lỗi nhét nhầm cổng vào danh sách cổng ra; (3) chỗ trống sẵn cho tương lai (ví dụ thêm `bool IsVisualized`). Kỹ thuật này gọi là *marker interface*.

```csharp
public sealed class InputPort<T> : IInputPort
{
```
**Dòng 28-29** — Bắt đầu class thật (có code chạy). Tách từng mảnh:
- `sealed` — cấm kế thừa. Port là mảnh ghép nền tảng, không ai được sửa hành vi bằng cách kế thừa.
- `<T>` — **generic**: `T` là chỗ trống điền kiểu sau. Viết class này 1 lần, dùng cho mọi kiểu dữ liệu: `InputPort<IVisionImage>`, `InputPort<double>`, `InputPort<Circle>`...
- `: IInputPort` — ký hợp đồng cổng vào (tức gánh đủ: Name, DisplayName, DataType, Direction, Value dạng object, IsOptional).

```csharp
    public InputPort(string name, string? displayName = null, bool isOptional = false)
    {
        Name = name;
        Displayname = displayName ?? name;   // 🔴
        IsOptional = isOptional;
    }
```
**Dòng 30-35** — Constructor với **tham số mặc định**: `displayName = null` và `isOptional = false` nghĩa là 2 tham số này bỏ qua được. Ba cách gọi hợp lệ:
```csharp
new InputPort<double>("Threshold")                    // tên hiển thị = tên máy, bắt buộc
new InputPort<double>("Threshold", "Ngưỡng")          // có tên đẹp
new InputPort<Mask>("Mask", "Mặt nạ", true)           // tùy chọn
```
- `Name = name;` — nhét tham số vào property chỉ-đọc (constructor là nơi DUY NHẤT gán được property `{ get; }`).
- `displayName ?? name` — không đưa tên đẹp thì lấy luôn tên máy làm tên hiển thị.
- 🔴 **Lỗi của bạn:** `Displayname` — chữ `n` viết thường. C# phân biệt hoa/thường nên `Displayname` và `DisplayName` là **2 property khác nhau**. Bạn vô tình tạo ra property mới `Displayname` (dòng 38 hiện tại), còn nghĩa vụ `DisplayName` của hợp đồng `IPort` thì **chưa ai thực hiện** → lỗi biên dịch "does not implement interface member 'IPort.DisplayName'". Sửa: đổi cả dòng 33 và dòng 38 thành `DisplayName`.

```csharp
    public string Name { get; }
    public string Displayname { get; }   // 🔴 phải là DisplayName
```
**Dòng 37-38** — Hai property chỉ-đọc lưu tên. (Lỗi hoa/thường như vừa nói.)

```csharp
    public Type DataType => typeof(T);
```
**Dòng 39** — Thực hiện nghĩa vụ `DataType` bằng `typeof(T)`: lấy object `Type` mô tả kiểu đã điền vào chỗ trống `T`. Với `InputPort<double>` nó trả về "kiểu double". Đây là cách "mặt generic" tự giới thiệu kiểu của mình cho "mặt object" của engine kiểm tra.

```csharp
    public PortDirection Direction => PortDirection.Input;
```
**Dòng 40** — Cổng vào thì chiều luôn là Input — hằng số, nên dùng `=>` trả thẳng, không cần lưu trữ.

```csharp
    public bool IsOptional { get; }
```
**Dòng 41** — Cờ tùy chọn, gán 1 lần trong constructor.

```csharp
    public T? Value { get; set; }
```
**Dòng 42** — **Mặt generic** của giá trị: kiểu `T?` (T hoặc rỗng). Code viết tool dùng mặt này:
```csharp
InputPort<double> port = ...;
double nguong = port.Value;      // ra thẳng double, không ép kiểu
port.Value = "abc";              // ❌ compiler chặn ngay lúc gõ!
```
So với mặt `object?` thì mặt này an toàn tuyệt đối — gõ sai kiểu là biết liền, không đợi chạy mới nổ.

```csharp
    object? IPort.Value
    {
        get => Value;
        set => Value = value is null ? default : (T)value;
    }
```
**Dòng 44-48** — **Cú pháp lạ nhất toàn dự án: explicit interface implementation.** Bóc từng lớp:

*Tại sao cần?* Class này giờ có 2 property cùng tên `Value`: bản `T?` (dòng 42) và bản `object?` mà hợp đồng `IPort` đòi. C# không cho 2 property trùng tên bình thường — nhưng cho phép **một bản gắn đích danh vào interface** bằng cách viết `IPort.Value` (tên interface + dấu chấm + tên member).

*Hiệu ứng:* bản `IPort.Value` bị "ẩn" — chỉ hiện ra khi biến được nhìn qua lăng kính `IPort`:
```csharp
var port = new InputPort<double>("X");
port.Value          // → bản T? (double), dành cho tool
IPort p = port;
p.Value             // → bản object?, dành cho engine
```
Cùng một object, 2 cách nhìn, không lẫn nhau.

*Từng dòng bên trong:*
- `get => Value;` — ai hỏi qua mặt object thì lấy giá trị từ mặt generic đưa ra (T tự động "hộp hóa" thành object).
- `set => Value = value is null ? default : (T)value;` — ai gán qua mặt object thì:
  - `value` (thường) = từ khóa có sẵn trong mọi `set`: giá trị người ta vừa gán.
  - `value is null ? A : B` — điều kiện ba ngôi: null thì A, không thì B.
  - `default` — giá trị "trắng" của T: `null` nếu T là class, `0` nếu là số, struct-toàn-0 nếu là struct. Phải dùng `default` chứ không viết thẳng `null` vì T có thể là kiểu không nhận null (như `double`).
  - `(T)value` — ép `object` về `T`. Nếu ruột thật không phải T (engine cắm nhầm dây) → runtime ném `InvalidCastException` ngay tại đây — thà nổ sớm ở cổng còn hơn nổ muộn trong ruột tool.

```csharp
public sealed class OutputPort<T> : IOutputPort
{
    public OutputPort(string name, string? displayName = null)
    {
        Name = name;
        DisplayName = displayName ?? name;
    }
```
**Dòng 51-57** — Cổng ra, giống hệt cổng vào nhưng: không có `isOptional` (khái niệm "tùy chọn" chỉ có nghĩa với đầu vào), và bên này bạn gõ `DisplayName` **đúng** hoa/thường.

```csharp
    public string Name { get; }
    public string DisplayName { get; }
    public Type DataType => typeof(T);
    public PortDirection Direction => PortDirection.Output;

    public T? Value { get; set; }
```
**Dòng 59-64** — Y hệt bên InputPort, chỉ khác `Direction` trả `Output`.

```csharp
    object? IPort.Value
    {
        set => Value;                                    // 🔴
        set => Value = value is null ? default :         // 🔴
    }
```
**Dòng 66-70** 🔴 — Đây là khối bạn gõ dở, hiện có 3 lỗi: hai `set` (một block chỉ được 1 get + 1 set), dòng 68 phải là `get`, dòng 69 đứt đuôi sau dấu `:`. Bản đúng — **y hệt bên InputPort**:
```csharp
    object? IPort.Value
    {
        get => Value;
        set => Value = value is null ? default : (T)value;
    }
```
Vì sao y hệt? Vì nhu cầu 2 mặt (generic cho tool / object cho engine) của cổng ra giống hệt cổng vào. Khi engine chép dây `input.Value = output.Value` (qua interface), nó đi qua `get` của cổng ra và `set` của cổng vào.

---

## Tóm tắt Quyển 2 bằng 1 ví dụ chạy thật

```csharp
// Tool A khai báo cổng ra, tool B khai báo cổng vào
var outP = new OutputPort<double>("Angle", "Góc");
var inP  = new InputPort<double>("Angle");

// —— Thế giới của TOOL (mặt generic, an toàn kiểu) ——
outP.Value = 12.5;              // gõ nhầm chuỗi vào đây là compiler chửi

// —— Thế giới của ENGINE (mặt object, vạn năng) ——
IPort from = outP;
IPort to   = inP;
if (from.DataType == to.DataType)   // kiểm tra "chân cắm" khớp nhau
    to.Value = from.Value;           // truyền dây, không biết kiểu vẫn truyền được

// —— Về lại thế giới của TOOL ——
double nhan = inP.Value;        // 12.5, ra thẳng double
```
