# QUYỂN 3 — Tools & Registry (bộ não của framework)

> Giải thích từng dòng: `ToolState.cs`, `ToolExecutionException.cs`, `ToolMetadataAttribute.cs`, `IToolContext.cs`, `ITool.cs`, `ToolParameter.cs`, `VisionTool.cs`, `ToolDescriptor.cs`, `IToolRegistrey.cs`, `ToolRegistry.cs`
>
> Quy ước: 🔴 = code hiện tại của bạn đang sai ở chỗ này.

---

## 1. `Tools/ToolState.cs`

```csharp
public enum ToolState
{
    Idle,
    Running,
    Completed,
    Failed,
    Skipped
}
```
Vòng đời của 1 tool trong 1 lần chạy flow: `Idle` (chưa chạy) → `Running` (đang chạy) → `Completed` (xong, có kết quả) hoặc `Failed` (lỗi) hoặc `Skipped` (bị bỏ qua — ví dụ nhánh điều kiện không được chọn). UI đọc enum này để tô màu node: xám/vàng/xanh/đỏ.

---

## 2. `Tools/ToolExecutionException.cs`

```csharp
public sealed class ToolExecutionException : Exception
```
**Dòng 3** — Tự chế một **loại lỗi riêng** bằng cách kế thừa `Exception` (class lỗi gốc của .NET). Tại sao không dùng `Exception` luôn? Để nơi bắt lỗi phân biệt được: `catch (ToolExecutionException)` = "lỗi nghiệp vụ của tool, hiển thị đẹp cho người vận hành"; các lỗi khác = bug thật, phải log full stack trace.

```csharp
    public ToolExecutionException(string message): base(message) { }
```
**Dòng 5** — Constructor nhận thông báo lỗi. Cú pháp mới: `: base(message)` — **gọi constructor của class cha** (`Exception`) trước, đưa `message` cho cha giữ (property `Message` nằm ở cha). Thân hàm `{ }` rỗng vì không có việc gì thêm.

```csharp
    public ToolExecutionException(string message, Exception inner): base(message, inner) { }
```
**Dòng 6** — Bản thứ 2 (**overload** — 2 hàm cùng tên khác tham số): kèm theo `inner` = lỗi gốc gây ra lỗi này. Ví dụ: OpenCV ném `OpenCVException` → tool bọc lại thành `ToolExecutionException("Threshold thất bại", ex)` — người đọc log thấy cả 2 tầng: lỗi nghiệp vụ VÀ nguyên nhân kỹ thuật bên dưới.

---

## 3. `Tools/ToolMetadataAttribute.cs`

Attribute = **nhãn dán** lên class. Không chạy, không làm gì — chỉ nằm đó chờ ai dùng reflection đến đọc. Cách dùng sau này:

```csharp
[ToolMetadata("Threshold", DisplayName = "Phân ngưỡng", Category = "Preprocess")]
public class ThresholdTool : VisionTool { ... }
```

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
```
**Dòng 3** — Nhãn dán cho... chính cái nhãn: quy định nhãn này chỉ được dán lên `class` (dán lên hàm/property là lỗi compile), và `Inherited = false` = class con KHÔNG tự động thừa hưởng nhãn của cha (mỗi tool phải tự khai nhãn riêng — hợp lý, vì key phải duy nhất).

```csharp
public sealed class ToolMetadataAttribute : Attribute
```
**Dòng 4** — Kế thừa `Attribute` là điều kiện để thành nhãn. Quy ước .NET: tên class kết thúc bằng `Attribute`, nhưng khi dán chỉ cần viết `[ToolMetadata(...)]` — compiler tự nối đuôi.

```csharp
    public ToolMetadataAttribute(string key)
    {
        Key = key;
    }

    public string Key { get; }
```
**Dòng 6-11** — `Key` đi qua constructor = **bắt buộc** khi dán nhãn: `[ToolMetadata("Threshold")]`. Key là "mã định danh" của loại tool — dùng làm khóa trong Registry, và ghi vào file lưu flow (`.json`) để mở lại đúng tool.

```csharp
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Description { get; set; } = string.Empty;
```
**Dòng 13-17** — Ba thuộc tính có `set` = **tùy chọn** khi dán nhãn, điền theo cú pháp `Tên = giá trị` bên trong ngoặc. Attribute là nơi hiếm hoi C# cho phép cú pháp gán property ngay trong lời gọi như vậy. `= string.Empty` = mặc định chuỗi rỗng (tránh null).

**Ý đồ lớn:** tách "thông tin mô tả tool" (tên, nhóm, mô tả — dữ liệu tĩnh) ra khỏi "logic tool" (code chạy). Registry chỉ cần đọc nhãn là dựng được menu tool cho UI mà **không cần tạo instance tool nào**.

---

## 4. `Tools/IToolContext.cs`

🔴 **File bạn đang sai từ dòng đầu:** bạn viết `public class IToolContext` nhưng thân lại theo kiểu interface (member không access modifier, không body) → không compile. Phải là `interface`:

```csharp
public interface IToolContext
{
    CancellationToken CancellationToken { get; }
    double PixelSize { get; }
    void Log(string message);
}
```

**Đây là gì?** "Bối cảnh" mà engine đưa cho tool lúc chạy — những dịch vụ dùng chung mà tool cần nhưng không tự có:

- **`CancellationToken CancellationToken`** — cơ chế **hủy giữa chừng** chuẩn của .NET. Người dùng bấm nút Stop → engine kích hoạt token → mọi tool đang chạy kiểm tra token này (`ThrowIfCancellationRequested()`) và tự dừng êm. Không có nó thì bấm Stop phải chờ tool chạy xong hoặc kill cả app.
- **`double PixelSize`** — hệ số quy đổi **pixel → milimét** (từ bước hiệu chuẩn camera). Tool đo đạc nhân với nó để trả kết quả theo đơn vị thật.
- **`void Log(string message)`** — cho tool ghi log mà không cần biết log đi đâu (console? file? cửa sổ UI?). Engine quyết định.

**Tại sao là interface mà không phải class?** Để test được: khi viết unit test cho tool, ta đưa vào một `FakeToolContext` tự chế thay vì phải dựng cả engine thật.

---

## 5. `Tools/ITool.cs`

```csharp
using VisionFlow.Core.Ports;
```
**Dòng 1** — Cần thấy `IInputPort`, `IOutputPort` từ gói Ports.

```csharp
public interface ITool
{
    string Id { get; set; }
```
**Dòng 5-7** — Hợp đồng của MỌI tool. `Id` — mã định danh của **từng instance** (2 node ThresholdTool trên cùng sơ đồ có 2 Id khác nhau; còn `TypeKey` giống nhau). Có `set` vì khi mở file flow đã lưu, phải gán lại Id cũ.

```csharp
    string TypeKey { get; }
    string DisplayName { get; }
    string Category { get; }
```
**Dòng 8-10** — Ba thông tin lấy từ nhãn `[ToolMetadata]`: mã loại tool, tên hiển thị, nhóm (để xếp menu).

```csharp
    IReadOnlyList<IInputPort> Inputs { get; }
    IReadOnlyList<IOutputPort> Outputs { get; }
    IReadOnlyList<IToolParameter> Parameters { get; }
```
**Dòng 12-14** — Ba danh sách "chỉ đọc từ bên ngoài": cổng vào, cổng ra, tham số. Kiểu phần tử toàn là **interface** (`IInputPort` chứ không phải `InputPort<T>`) — vì ở tầng hợp đồng này chưa ai biết T là gì.

```csharp
    ToolState State { get; }
    long ElapsedMs { get; }
    string? ErrorMessage { get; }
```
**Dòng 16-18** — Trạng thái sau khi chạy: đang ở pha nào, chạy hết bao nhiêu mili-giây (`long` = số nguyên 64-bit), thông báo lỗi nếu fail (`string?` — null khi không có lỗi).

```csharp
    void Execute(IToolContext context);
```
**Dòng 19** — Nghĩa vụ hành động duy nhất: "chạy đi, đây là bối cảnh". Engine chỉ cần biết đúng 1 câu này để chạy mọi tool.

---

## 6. `Tools/ToolParameter.cs`

```csharp
public enum ParameterInteraction
{
    None,
    ReactRegion,        // 🔴 typo — phải là RectRegion
    RotatedRectRegion,
    CircleRegion,
    Polygon,
    Caliper,
    Point,
    Template
}
```
**Dòng 3-13** — Enum báo cho UI biết tham số này **chỉnh bằng cách nào**: `None` = gõ số/chọn dropdown bình thường; các giá trị còn lại = phải **vẽ trên ảnh** (kéo chữ nhật, chữ nhật xoay, hình tròn, đa giác, dải caliper, chấm 1 điểm, chọn vùng mẫu). UI đọc enum này để quyết định mở editor nào.

```csharp
public interface IToolParameter
{
    string Name { get; }
    string DisplayName { get; }
    Type ValueType { get; }
    object? Value { get; set; }
    object? Minimum { get; }
    object? Maximum { get; }
    string Category { get; }
    int Order { get; }
    IReadOnlyList<string>? Choices { get; }
    ParameterInteraction Interaction { get; }
}
```
**Dòng 15-27** — Hợp đồng "mặt object" của tham số — giống chiến thuật 2 mặt của Ports (Quyển 2). UI property-grid cầm `IToolParameter` chung chung là render được mọi tham số:
- `ValueType` — kiểu để chọn editor (số → ô số, bool → checkbox, enum/`Choices` → dropdown).
- `Minimum`/`Maximum` — chặn khoảng cho slider/ô số; kiểu `object?` vì mỗi tham số một kiểu; null = không giới hạn.
- `Category` + `Order` — nhóm và thứ tự hiển thị trong grid.
- `Choices` — danh sách lựa chọn cố định; có nó là UI hiện dropdown.

```csharp
public sealed class ToolParameter<T> : IToolParameter
{
    public ToolParameter(
        string name,
        T value,
        string? displayName = null,
        T? minimum = default,
        T? maximum = default,
        string catagory = "General",       // 🔴 typo: phải là category
        int order = 0,
        IReadOnlyList<string>? choices = null,
        ParameterInteraction interaction = ParameterInteraction.None)
```
**Dòng 29-40** — Class thật, generic `<T>`. Constructor 9 tham số nhưng 7 cái có mặc định → gọi tối giản chỉ cần `new ToolParameter<int>("ThresholdValue", 128)`. `T? minimum = default` — mặc định là "trắng" của T (null với class, 0 với số — kết hợp với quy ước "0 = không giới hạn").
🔴 Dòng 37 gõ `catagory` nhưng dòng 47 dùng `category` → 2 tên khác nhau, lỗi "The name 'category' does not exist".

```csharp
    {
        Name = name;
        DisplayName = displayName ?? name;
        Value = value;
        Minumum = minimum;                  // 🔴 typo: phải là Minimum
        Maximum = maximum;
        Category = category;
        Order = order;
        Choices = choices;
        Interaction = interaction;
    }
```
**Dòng 41-51** — Đổ tham số vào property. Toàn kiến thức cũ (`??` xem Quyển 1). 🔴 `Minumum` gõ sai.

```csharp
    public string Name { get; }
    public string DisplayName { get; }
    public Type ValueType => typeof(T)      // 🔴 thiếu dấu ;
```
**Dòng 52-54** — 🔴 dòng 54 thiếu `;` cuối. `typeof(T)` xem Quyển 2.

```csharp
    public T Value { get; set; }
    public T? Maximum { get; }
    public T? Minimum { get; }
    public string Category { get; }
    public int Order { get; }
    public IReadOnlyList<string> Choices { get; }
    public ParameterInteraction Interaction { get; }
```
**Dòng 56-63** — "Mặt generic": `Value` kiểu `T` thật, min/max kiểu `T?`. (Lưu ý nhỏ: `Choices` nên khai là `IReadOnlyList<string>?` — có dấu `?` — vì constructor cho phép truyền null.)

```csharp
    object? IToolParameter.Value
    {
        get => Value;
        set => Value = Convert(value);
    }
```
**Dòng 65-69** — Explicit interface implementation (giải phẫu kỹ ở Quyển 2, mục `IPort.Value`). Khác một điểm: chiều `set` không ép kiểu thô `(T)value` mà đi qua hàm `Convert` tự viết bên dưới — vì giá trị đến từ **UI/file cấu hình** thường là chuỗi ("128") hay kiểu lệch (int vs double), ép thô sẽ nổ.

```csharp
    object? IToolParameter.Minimum => Minimum;
    object? IToolParameter.Maximum => Maximum;
```
**Dòng 71-72** — Cùng kỹ thuật: mặt object của min/max chỉ việc trưng bản generic ra (tự hộp hóa thành object).

```csharp
    private T Convert(object? v)
    {
        if (v is null) return default;
        if (v is T t) return t;
```
**Dòng 74-77** — Bộ chuyển đổi "bao dung". `private` = đồ dùng nội bộ. Hai lối tắt: null → giá trị trắng; đã đúng kiểu T → trả luôn (pattern `is T t` xem Quyển 1).

```csharp
        var target = typeof(T);
        var underlying = Nullable.GetUnderlyingType(target) ?? target;
```
**Dòng 79-80** — `var` = compiler tự suy kiểu biến. Dòng 80 xử lý ca T là kiểu nullable như `int?`: `Nullable.GetUnderlyingType(typeof(int?))` bóc ra `int`; nếu T không phải nullable thì nó trả null → `??` giữ nguyên `target`. Kết quả: `underlying` luôn là "kiểu lõi" để chuyển đổi.

```csharp
        if (v is string s && string.IsNullOrWhiteSpace(s))
            return Value;
```
**Dòng 82-83** — Người dùng xóa trắng ô nhập → đừng cố parse chuỗi rỗng, giữ nguyên giá trị hiện tại (`Value`). `&&` = "và" (vế trái sai thì bỏ qua vế phải luôn).

```csharp
        try
        {
            if (underlying.IsEnum)
            {
                return v is string es
                    ? (T)Enum.Parse((underlying, es, ignoreCase: true))      // 🔴 thừa ( trước underlying
                    : (T)Enum.ToObject(underlying, System.Convert.ChangeType(v, Enum.GetUnderlyingType(underlying))    // 🔴 thiếu )); ở cuối
            }
```
**Dòng 85-92** — Ca khó nhất: T là enum. Hai nguồn:
- Từ chuỗi (`"OK"`): `Enum.Parse(kiểuEnum, chuỗi, ignoreCase: true)` — `ignoreCase: true` là *named argument*, "ok"/"OK" đều nhận.
- Từ số (1): đổi số về đúng kiểu số lót của enum (`Enum.GetUnderlyingType`) rồi `Enum.ToObject` dựng giá trị enum.
Bản đúng của 2 dòng lỗi:
```csharp
return v is string es
    ? (T)Enum.Parse(underlying, es, ignoreCase: true)
    : (T)Enum.ToObject(underlying, System.Convert.ChangeType(v, Enum.GetUnderlyingType(underlying)));
```

```csharp
            return (T)System.Convert.ChangeType(v, underlying, System.Globalization.CultureInfo.InvariantCulture);
```
**Dòng 94** — Ca thường (số, chuỗi số, bool...): `Convert.ChangeType` là "máy đổi kiểu vạn năng" của .NET. Phải viết `System.Convert` đầy đủ vì class này có hàm tên `Convert` riêng — trùng tên, phải chỉ rõ họ tên. `CultureInfo.InvariantCulture` = luôn hiểu dấu chấm là thập phân ("3.14"), bất kể máy cài tiếng Việt (nơi "3,14" mới là chuẩn) — file cấu hình phải đọc được trên mọi máy.

```csharp
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            return Value;
        }
```
**Dòng 96-99** — `catch ... when (điều kiện)` = **exception filter**: chỉ bắt khi điều kiện đúng. `is A or B or C` = pattern matching nhiều kiểu. Ý nghĩa: 4 loại lỗi này đều là "dữ liệu người dùng nhập bậy" → nuốt, giữ giá trị cũ, app không sập. Lỗi loại khác (bug thật) → cho bay tiếp lên trên để còn thấy mà sửa.

---

## 7. `Tools/VisionTool.cs`

Class **xương sống**: mọi tool cụ thể sẽ kế thừa nó.

```csharp
using System.Reflection;                 // 🔴 bạn đang thiếu; và có dòng using VisionFlow.Core.Core.Ports thừa chữ Core
using VisionFlow.Core.Ports;
```
**Dòng 1-2** — `System.Reflection` cần cho `GetCustomAttribute`. 🔴 Code bạn: `using VisionFlow.Core.Core.Ports;` — thừa một `.Core` (IDE auto-import lỗi), phải là `VisionFlow.Core.Ports`.

```csharp
public abstract class VisionTool : ITool
{
    private readonly List<IInputPort> _inputs = new();
    private readonly List<IOutputPort> _outputs = new();
    private readonly List<IToolParameter> _parameters = new();
```
**Dòng 6-10** — `abstract` = chỉ làm cha, không `new` trực tiếp. Ba danh sách **private** (chỉ mình class này đụng được), `readonly` = biến không trỏ sang list khác được (nội dung list vẫn thêm/bớt bình thường), `new()` = viết tắt `new List<...>()`. Dấu `_` đầu tên là quy ước C# cho field private.

```csharp
    protected VisionTool()
    {
        Id = Guid.NewGuid().ToString("N");
```
**Dòng 12-14** — Constructor `protected` = chỉ class con gọi được (hợp lý: người ngoài không được `new VisionTool` vì nó abstract). `Guid.NewGuid()` = sinh mã định danh toàn cầu ngẫu nhiên 128-bit (xác suất trùng ≈ 0); `.ToString("N")` = in thành 32 ký tự hex liền nhau không gạch nối.

```csharp
        var meta = GetType().GetCustomAttributes<ToolMetadataAttribute>();   // 🔴 phải là GetCustomAttribute (số ít)
        TypeKey = meta?.Key ?? GetType().Name;
        DisplayName = !string.IsNullOrEmpty(meta?.DisplayName) ? meta!.DisplayName : GetType().Name;
        Category = meta?.Category ?? "General";
    }
```
**Dòng 15-19** — Đoạn **tự đọc nhãn dán trên chính mình**:
- `GetType()` — "tôi thực chất là class nào?". Gọi trong constructor của cha nhưng object là `ThresholdTool` thì trả về `ThresholdTool` — luôn ra kiểu thật.
- `GetCustomAttribute<ToolMetadataAttribute>()` — đọc nhãn `[ToolMetadata]` dán trên class đó; không có nhãn → null. 🔴 Bạn gõ bản **số nhiều** `GetCustomAttributes` — trả về *danh sách* nhãn, không phải 1 nhãn → `meta?.Key` không tồn tại, lỗi compile.
- `meta?.Key ?? GetType().Name` — có nhãn thì lấy Key, không thì lấy tên class làm key tạm.
- `meta!.DisplayName` — dấu `!` (null-forgiving): "tôi thề với compiler là chỗ này meta không null" — thề được vì vế điều kiện phía trước đã kiểm tra rồi. Chỉ tắt cảnh báo, không có tác dụng lúc chạy.

```csharp
    public string Id { get; set; }
    public string TypeKey { get; set; }
    public string DisplayName { get; }
    public string Category { get; }

    public IReadOnlyList<IInputPort> Inputs => _inputs;
    public IReadOnlyList<IOutputPort> Outputs => _outputs;
    public IReadOnlyList<IToolParameter> Parameters => _parameters;
```
**Dòng 21-28** — Thực hiện nghĩa vụ của `ITool`. Ba dòng cuối là chiêu "trong nhà sửa, khách chỉ ngắm": field là `List` (private, sửa được), phơi ra dưới kiểu `IReadOnlyList` (bên ngoài hết cửa `.Add`).

```csharp
    public ToolState State { get; internal set; } = ToolState.Idle;
    public long ElapsedMs { get; internal set; }
    public string? ErrorMessage { get; internal set; }
```
**Dòng 30-32** — Từ khóa mới: **`internal set`** = ai đọc cũng được (`get` public), nhưng chỉ code **trong cùng assembly** (project VisionFlow.Core) gán được. Ý đồ: chỉ engine (sẽ viết trong Core) được cập nhật trạng thái tool; tool con và code ngoài không được tự sửa `State`.

```csharp
    protected InputPort<T> AddInput<T>(string name. string? displayName = null, bool optional = false)   // 🔴 dấu . phải là ,
    {
        var port = new InputPort<T>(name, DisplayNameAttribute, optional);   // 🔴 phải là displayName
        _inputs.Add(port);
        return port;
    }
```
**Dòng 34-39** — Hàm tiện ích cho class con khai cổng: tạo port → nhét vào danh sách private → **trả về chính port đó** để class con giữ lại bản generic mà dùng. Class con sẽ viết:
```csharp
private readonly InputPort<IVisionImage> _in;
public ThresholdTool() { _in = AddInput<IVisionImage>("Image", "Ảnh vào"); }
```
🔴 Hai lỗi: dấu `.` thay `,` giữa 2 tham số; và `DisplayNameAttribute` (IDE tự điền bậy tên class có sẵn của .NET) — phải là tham số `displayName`.

```csharp
    protected OutputPort<T> AddOuput<T>(string name, string? displayName = null)   // 🔴 AddOuput → AddOutput
    {
        var port = new OutputPort<T>(name, displayName);
        _outputs.Add(port);
        return port;
    }
```
**Dòng 41-46** — Bản cổng ra, y hệt logic trên. 🔴 Thiếu chữ `t` trong tên hàm (chạy vẫn được nhưng ai gọi cũng phải gõ sai theo — sửa sớm).

```csharp
    protected ToolParameter<T> addParameter<T>(                              // 🔴 addParameter → AddParameter (quy ước PascalCase)
        string name, string value, IReadOnlyList<string> choices, ...)       // 🔴 string value → T value
    {
        var p = new ToolParameter<string>(name, value, ...);                 // 🔴 <string> → <T>
        ...
    }
```
**Dòng 48-56** 🔴 — Hàm này bạn gõ hỏng nặng nhất: khai generic `<T>` nhưng tham số `value` lại khóa cứng kiểu `string`, và bên trong tạo `ToolParameter<string>` → chỗ trống T thành vô nghĩa, tool không thể khai tham số kiểu int/double/enum. Bản đúng:
```csharp
    protected ToolParameter<T> AddParameter<T>(
        string name, T value, string? displayName = null,
        T? minimum = default, T? maximum = default,
        string category = "General", int order = 0,
        IReadOnlyList<string>? choices = null,
        ParameterInteraction interaction = ParameterInteraction.None)
    {
        var p = new ToolParameter<T>(name, value, displayName, minimum, maximum, category, order, choices, interaction);
        _parameters.Add(p);
        return p;
    }
```

```csharp
    public IInputPort? FindInput(string name) => _inputs.FirstOrDefault(p => p.Name == name);
    public IOutputPort? FindOutput(string name) => _outputs.FirstOrDefault(p => p.Name == name);
    public IToolParameter? FindParameter(string name) => _parameters.FirstOrDefault(p => p.Name == name);
```
**Dòng 58-60** — Tra cứu theo tên, dùng **LINQ + lambda**: `p => p.Name == name` là hàm-không-tên nhận `p` trả true/false; `FirstOrDefault` duyệt list trả phần tử đầu thỏa mãn, không có thì null (nên kiểu trả về có `?`). Engine dùng khi nối dây từ file lưu: "tìm cổng tên Image của tool X".

```csharp
    public void Execute(IToolContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        ValidateInputs();
        OnExecute(context);
    }

    protected abstract void OnExecute(IToolContext context);
```
**Dòng 62-69** — **Template Method pattern**, trái tim của file:
- `Execute` (public, KHÔNG virtual) = kịch bản cố định 3 bước: (1) người dùng đã bấm Stop chưa? `ThrowIfCancellationRequested()` ném `OperationCanceledException` nếu rồi; (2) kiểm tra đầu vào; (3) chạy phần việc riêng.
- `OnExecute` (protected **abstract**) = **chỗ trống trong kịch bản** — class con BẮT BUỘC điền (không điền là không compile). ThresholdTool điền code phân ngưỡng, GrabImageTool điền code bắt ảnh.
- Nhờ tách 2 tầng: mọi tool tự động có kiểm tra cancel + validate mà người viết tool **không phải nhớ** viết — framework lo.

```csharp
    protected virtual void ValidateInputs()
    {
        foreach (var input in _inputs)
        {
            if (!((IInputPort)input).IsOptional && input.Value is null)
            {
                throw new ToolExecutionException($"Required Input '{input.Name}' Of '{DisplayName}' Is Not Connected");
            }
        }
    }
```
**Dòng 71-80** — Kiểm tra mặc định: duyệt mọi cổng vào (`foreach`), cổng nào **bắt buộc** (`!IsOptional`) mà **trống** (`Value is null`) → ném lỗi tự chế (mục 2) với thông báo nêu đích danh cổng nào, tool nào. `virtual` = class con ĐƯỢC PHÉP `override` để kiểm tra kiểu khác (ví dụ "cần ít nhất 1 trong 2 cổng"), không override thì dùng bản này. (Ghi chú: `(IInputPort)input` ép kiểu ở đây thực ra thừa — `_inputs` đã là `List<IInputPort>` — viết `!input.IsOptional` là đủ.)

---

## 8. `Registry/ToolDescriptor.cs`

"Tấm danh thiếp" của một loại tool trong sổ đăng ký — đủ thông tin để UI hiện menu và để đúc tool mới, mà **chưa cần tạo tool nào**.

```csharp
public sealed class ToolDescriptor
{
    public ToolDescriptor(string key, string dsplayName, string category, string description, Type toolType, Func<VisionTool> factory)
    {
        Key = key;
        DisplayName = DisplayName;      // 🔴 tự gán chính nó!
        Category = category;
        ToolType = toolType;
        Factory = factory;              // 🔴 và quên gán Description
    }
```
**Dòng 5-14** — 🔴 Hai lỗi kinh điển:
- `DisplayName = DisplayName` — gán property cho **chính nó** (vô nghĩa, DisplayName mãi mãi null) vì tham số bị gõ sai thành `dsplayName` nên IDE không gợi ý đúng. Sửa: đổi tham số thành `displayName` và gán `DisplayName = displayName;`.
- Thiếu dòng `Description = description;` — tham số nhận vào rồi vứt.

```csharp
    public string Key { get; }
    public string DisplayName { get; }
    public string Category { get; }
    public string Description { get; }
    public Type ToolType { get; }
    public Func<VisionTool> Factory { get; }
```
**Dòng 16-21** — Toàn `{ get; }` = danh thiếp bất biến. Dòng cuối là kiểu mới: **`Func<VisionTool>`** = "một HÀM không tham số, trả về VisionTool". Hàm được lưu như dữ liệu — gọi lúc nào cũng được: `var tool = descriptor.Factory();`. Đây là **Factory pattern**: sổ đăng ký không giữ tool, giữ *công thức đúc tool*.

---

## 9. `Registry/IToolRegistrey.cs`  🔴 tên file + tên interface sai chính tả: `Registrey` → `Registry`

```csharp
using System.Reflection;      // 🔴 bạn đang có using FxResources.System.Reflection — auto-import bậy, xóa đi
using VisionFlow.Core.Tools;

public interface IToolRegistry
{
    IReadOnlyCollection<ToolDescriptor> Descriptors { get; }
    void Register(Type toolType);
    void RegisterAssembly(Assembly assembly);
    ToolDescriptor? Find(string key);
    VisionTool Create(string key);
}
```
Hợp đồng của sổ đăng ký, 5 nghĩa vụ:
- `Descriptors` — xem toàn bộ danh thiếp (UI dựng menu tool từ đây). `IReadOnlyCollection` = như IReadOnlyList nhưng không hứa truy cập theo chỉ số — vì nguồn là values của Dictionary, không có thứ tự.
- `Register(Type)` — đăng ký thủ công 1 loại tool.
- `RegisterAssembly(Assembly)` — quét nguyên 1 DLL, đăng ký tự động mọi tool tìm thấy (cơ chế **plugin**).
- `Find(key)` — tra danh thiếp theo mã; null nếu chưa đăng ký.
- `Create(key)` — đúc 1 tool mới từ mã.

🔴 Lưu ý tên interface bạn gõ `IToolRegistrey` nhưng ToolRegistry.cs lại viết `: IToolRegistry` → hai bên lệch nhau, phải thống nhất là `IToolRegistry` (sửa cả tên file).

---

## 10. `Registry/ToolRegistry.cs`

```csharp
using System.Reflection;        // 🔴 bạn đang import System.CodeDom và FxResources... — đều bậy, thay bằng dòng này
using VisionFlow.Core.Tools;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ToolDescriptor> _byKey = new(StringComparer.Ordinal);
    public IReadOnlyCollection<ToolDescriptor> Descriptors => _byKey.Values;
```
**Dòng 7-10** — Ruột sổ là **`Dictionary<string, ToolDescriptor>`** = bảng băm key → danh thiếp, tra cứu tức thì. `StringComparer.Ordinal` = so key từng byte, phân biệt hoa thường ("threshold" ≠ "Threshold"). `_byKey.Values` = phơi tập danh thiếp ra ngoài.

```csharp
    public void Register(Type toolType)
    {
        if (!typeof(VisionTool).IsAssignableForm(toolType) || toolType.IsAbstract)      // 🔴 IsAssignableForm → IsAssignableFrom
            throw new ArgumentException($"'{toolType.Name}' không phải là Visionl cụ thể", nameof(toolType));
```
**Dòng 12-15** — Guard đầu tiên: chỉ nhận đăng ký class (1) kế thừa VisionTool và (2) không abstract.
- `typeof(VisionTool).IsAssignableFrom(toolType)` — "biến kiểu VisionTool có gán được object kiểu toolType không?" = toolType có phải con cháu VisionTool không. **Mẹo nhớ chiều:** `cha.IsAssignableFrom(con)` → true. 🔴 Bạn gõ hoán chữ `Form`/`From`.
- `||` = "hoặc"; `toolType.IsAbstract` chặn đăng ký class nửa vời.

```csharp
        var meta = toolType.GetCustomAttributes<ToolMetadataAttribute>()     // 🔴 số ít: GetCustomAttribute
                   ?? throw new ArgumentException($"'{toolType.Name}' thiếu [ToolMetadata]", nameof(toolType));
```
**Dòng 17-18** — Đọc nhãn; không có nhãn → từ chối đăng ký (vì không có Key). Mẫu `x ?? throw ...` đã gặp ở MatVisionImage. 🔴 Lại lỗi số nhiều/số ít như VisionTool.cs.

```csharp
        if (toolType.GetConstructor(Type.EmptyTypes) is null)
            throw new ArgumentException($"'{toolType.Name}' cần constructor không tham số", nameof(toolType));
```
**Dòng 20-21** — Guard thứ ba: soi bằng reflection xem class có **constructor rỗng** không (`Type.EmptyTypes` = mảng kiểu rỗng = "tìm constructor 0 tham số"). Bắt buộc vì dòng kế sẽ `new` bằng máy — máy chỉ biết gọi constructor rỗng.

```csharp
        VisionTool Factory() => (VisionTool)Activator.CreateInstance(toolType);
```
**Dòng 23** — Cú pháp mới: **local function** — hàm khai báo *bên trong* hàm khác. `Activator.CreateInstance(type)` = "new bằng reflection": tạo object khi chỉ biết `Type` lúc runtime (trả về `object` nên ép `(VisionTool)`). Hàm `Factory` này sẽ được **lưu vào danh thiếp** như một giá trị — mỗi lần gọi đúc ra 1 tool mới tinh. Nó "nhớ" biến `toolType` của lần Register này (kỹ thuật gọi là *closure*).

```csharp
        var display = string.IsNullOrEmpty(meta.Display) ? toolType.Name : meta.DisplayName;    // 🔴 meta.Display → meta.DisplayName
        _byKey[meta.Key] = new ToolDescriptor(meta.Key, display, meta.Category, meta.Description, toolType, Factory);
    }
```
**Dòng 25-27** — Chốt tên hiển thị (nhãn không ghi thì lấy tên class) rồi ghi danh thiếp vào sổ. `_byKey[key] = value` với Dictionary: chưa có key thì thêm, có rồi thì **ghi đè** — đăng ký lại = cập nhật. 🔴 `meta.Display` — property không tồn tại, phải là `meta.DisplayName`.

```csharp
    public void RegisterAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(VisionTool).IsAssignableForm(type)) continue;   // 🔴 IsAssignableFrom
            if (type.GetCustomAttribute<ToolMetadataAttribute>() is null) continue;
            Register(type);
        }
    }
```
**Dòng 29-37** — Đăng ký hàng loạt: `assembly.GetTypes()` liệt kê **mọi class trong 1 DLL**; `continue` = bỏ qua phần tử này, sang phần tử kế. Lọc 2 lớp (không phải tool → bỏ; không dán nhãn → bỏ) rồi gọi `Register`. Cách dùng sau này: `registry.RegisterAssembly(typeof(ThresholdTool).Assembly);` — **một dòng nạp toàn bộ tool của project Tools**; thêm tool mới chỉ cần viết class + dán nhãn, không sửa registry. Đây là lý do tồn tại của cả cơ chế attribute + reflection.

```csharp
    public ToolDescriptor? Find(string key) => _byKey.GetValueOrDefault(key);
```
**Dòng 39** — Tra sổ: `GetValueOrDefault` = có key thì trả danh thiếp, không thì null (êm ái hơn `_byKey[key]` — bản này ném exception khi thiếu key).

```csharp
    public VisionTool Create(string key)
    {
        var descriptor = Find(key)
                         ?? throw new KeyNotFoundException($"Chưa đăng ký tool có key '{key}'");
        return descriptor.Factory();
    }
```
**Dòng 41-46** — Đúc tool: tra danh thiếp (không có → ném lỗi rõ ràng) rồi **gọi công thức**: `descriptor.Factory()` — chính local function đã lưu ở dòng 23, mỗi lần gọi ra 1 instance mới. Đây là hàm mà phần "mở file flow đã lưu" sẽ dùng: đọc key từ JSON → `Create(key)` → dựng lại node.

---

## Sơ đồ ghép toàn bộ 3 quyển

```
[ToolMetadata("Threshold")]                       ← Quyển 3: nhãn
class ThresholdTool : VisionTool                  ← Quyển 3: xương sống
{
    InputPort<IVisionImage> _in;                  ← Quyển 2: cổng, 2 mặt generic/object
    ToolParameter<int> _nguong;                   ← Quyển 3: tham số, UI tự render
    override OnExecute(ctx)                       ← Quyển 3: template method
    {
        var mat = _in.Value.AsMat();              ← Quyển 1: cầu nối IVisionImage → Mat
        ... OpenCV ... 
        _out.Value = new MatVisionImage(kq);      ← Quyển 1: bọc Mat lại thành IVisionImage
    }
}

ToolRegistry.RegisterAssembly(dll)                ← Quyển 3: reflection quét & đăng ký
UI menu  ← registry.Descriptors
Engine   ← registry.Create("Threshold") → nối Ports → Execute(context)
Kết quả  ← VisionResult.Judge = OK/NG             ← Quyển 1: models
```
