namespace VisionFlow.Core.Tools;

public enum ParameterInteraction
{
    None,
    ReactRegion,
    RotatedRectRegion,
    CircleRegion,
    Polygon,
    Caliper,
    Point,
    Template
}

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

public sealed class ToolParameter<T> : IToolParameter
{
    public ToolParameter(
        string name,
        T value,
        string? displayName = null,
        T? minimum = default,
        T? maximum = default,
        string category = "General",
        int order = 0,
        IReadOnlyList<string>? choices = null,
        ParameterInteraction interaction = ParameterInteraction.None)
    {
        Name = name;
        DisplayName = displayName ?? name;
        Value = value;
        Minimum = minimum;
        Maximum = maximum;
        Category = category;
        Order = order;
        Choices = choices;
        Interaction = interaction;
    }
    public string Name { get; }
    public string DisplayName { get; }
    public Type ValueType => typeof(T);
    
    public T Value { get; set; }
    public T? Maximum { get; }
    public T? Minimum { get; }

    public string Category { get; }
    public int Order { get; }
    public IReadOnlyList<string> Choices { get; }
    public ParameterInteraction Interaction { get; }

    object? IToolParameter.Value
    {
        get => Value;
        set => Value = Convert(value);
    }
    
    object? IToolParameter.Minimum => Minimum;
    object? IToolParameter.Maximum => Maximum;

    private T Convert(object? v)
    {
        if (v is null) return default;
        if (v is T t) return t;

        var target = typeof(T);
        var underlying = Nullable.GetUnderlyingType(target) ?? target;

        if (v is string s && string.IsNullOrWhiteSpace(s))
            return Value;

        try
        {
            if (underlying.IsEnum)
            {
                return v is string es
                    ? (T)Enum.Parse(underlying, es, ignoreCase: true)
                    : (T)Enum.ToObject(underlying, System.Convert.ChangeType(v, Enum.GetUnderlyingType(underlying)));
            }

            return (T)System.Convert.ChangeType(v, underlying, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            return Value;
        }
    }
}