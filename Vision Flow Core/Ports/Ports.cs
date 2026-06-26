namespace Vision_Flow_Core.Ports;

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