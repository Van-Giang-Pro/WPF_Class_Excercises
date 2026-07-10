namespace VisionFlow.Core.Ports;

public enum PortDirection
{
    Input,
    Output
}

public interface IPort
{
    string Name { get; }
    string DisplayName { get; }
    Type DataType { get; }
    PortDirection Direction { get; }
    object? Value { get; set; }
}

public interface IInputPort : IPort
{
    bool IsOptional { get; }
}

public interface IOutputPort : IPort
{
    
}

public sealed class InputPort<T> : IInputPort
// Định nghĩa một loại object tên InputPort
// Nó dùng kiểu generic<T> với <T> là một chỗ trống cho kiểu dữ liệu và sẽ được điền sau
// Giống công thức nấu ăn ghi 1 kg nguyên liệu — lúc viết công thức chưa biết là thịt hay cá, khi nấu mới điền
// Khai báo generic<T> để viết một class dùng chung cho mọi kiểu dữ liệu, thay vì copy paste nhiều class giống hệt nhau
{
    public InputPort(string name, string? displayName = null, bool isOptional = false)
    {
        Name = name;
        DisplayName = displayName ?? name;
        IsOptional = isOptional;
    }
    
    public string Name { get; }
    public string DisplayName { get; }
    public Type DataType => typeof(T);
    public PortDirection Direction => PortDirection.Input;
    public bool IsOptional { get; }
    public T? Value { get; set; }

    object? IPort.Value
    {
        get => Value;
        set => Value = value is null ? default : (T)value;
    }
}

public sealed class OutputPort<T> : IOutputPort
{
    public OutputPort(string name, string? displayName = null)
    {
        Name = name;
        DisplayName = displayName ?? name;
    }
    
    public string Name { get; }
    public string DisplayName { get; }
    public Type DataType => typeof(T);
    public PortDirection Direction => PortDirection.Output;
    
    public T? Value { get; set; }

    object? IPort.Value
    {
        get => Value;
        set => Value = value is null ? default : (T)value;
    }
}