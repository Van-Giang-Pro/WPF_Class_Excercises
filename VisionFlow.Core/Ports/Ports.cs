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
{
    public InputPort(string name, string? displayName = null, bool isOptional = false)
    {
        Name = name;
        Displayname = displayName ?? name;
        IsOptional = isOptional;
    }
    
    public string Name { get; }
    public string Displayname { get; }
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
        set => Value;
        set => Value = value is null ? default :
    }
}