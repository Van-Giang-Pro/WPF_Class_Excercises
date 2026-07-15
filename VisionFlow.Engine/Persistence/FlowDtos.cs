namespace VisionFlow.Engine.Persistence;

internal sealed class FlowDto
{
    public string Name { get; set; } = "Untitled";
    public double PixelSize { get; set; } = 1.0;
    public List<NodeDto> Nodes { get; set; } = new();
    // Một danh sách chứa nhiều NoteDto với List<T> là khai báo kiểu generic
    // Với Node là tên property
    // Khởi tạo giá trị mặc định với một danh sách rỗng
    // Tạo một cái danh sách chứa nhiều NodeDto tên là Nodes và cho nó bắt đầu là danh sách rỗng (chưa có gì trong đó)
    public List<ConnectionDto> Connections { get; set; } = new();
}

//Khuôn chứa dữ liệu của một Node trong flow
internal sealed class NodeDto
{
    public string Id { get; set; } = string.Empty; // Mã định danh duy nhất của Node
    public string TypeKey { get; set; } = string.Empty; // Tên của loại tool mà node này đại diện
    public double X { get; set; } // Tọa độ đặt node trên màn hình trục X
    public double Y { get; set; } // Tọa độ đặt node trên màn hình trục Y
    public Dictionary<string, object?> Parameters { get; set; } = new();
    // Tạo ra một danh sách chứa các tham số của node với string là tên tham số và object là giá trị của tham số
}

// Mô tả dữ một dây nối giữa hai Node 
internal sealed class ConnectionDto
{
    public string SourceNodeId { get; set; } = string.Empty; // Id của Node nguồn
    public string SourcePort { get; set; } = string.Empty; // Id của Port nguồn
    public string TargetNodeId { get; set; } = string.Empty; // Id của Node đích
    public string TargetPort { get; set; } = string.Empty; // Id của Port đích
}