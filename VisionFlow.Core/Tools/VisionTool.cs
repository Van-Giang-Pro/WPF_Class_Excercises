using System.Reflection;
using VisionFlow.Core.Ports;

namespace VisionFlow.Core.Tools;

public abstract class VisionTool : ITool
{
    private readonly List<IInputPort> _inputs = new();
    private readonly List<IOutputPort> _outputs = new();
    private readonly List<IToolParameter> _parameters = new();

    protected VisionTool()
    {
        Id = Guid.NewGuid().ToString("N");
        var meta = GetType().GetCustomAttribute<ToolMetadataAttribute>();
        TypeKey = meta?.Key ?? GetType().Name;
        DisplayName = !string.IsNullOrEmpty(meta?.DisplayName) ? meta!.DisplayName: GetType().Name;
        Category = meta?.Category ?? "General";
    }
    
    public string Id { get; set; }
    public string TypeKey { get; set; }
    public string DisplayName { get; }
    public string Category { get; }

    public IReadOnlyList<IInputPort> Inputs => _inputs;
    public IReadOnlyList<IOutputPort> Outputs => _outputs;
    public IReadOnlyList<IToolParameter> Parameters => _parameters;

    public ToolState State { get; internal set; } = ToolState.Idle;
    public long ElapsedMs { get; internal set; }
    public string? ErrorMessage { get; internal set; }
    
    protected InputPort<T> AddInput<T>(string name, string? displayName = null, bool optional = false)
    {
        var port = new InputPort<T>(name, displayName, optional);
        _inputs.Add(port);
        return port;
    }

    protected OutputPort<T> AddOutput<T>(string name, string? displayName = null)
    {
        var port = new OutputPort<T>(name, displayName);
        _outputs.Add(port);
        return port;
    }

    protected ToolParameter<T> AddParameter<T>(
        string name, T value, string? displayName = null,
        T? minimum = default, T? maximum = default,
        string category = "General", int order = 0,
        IReadOnlyList<string>? choices = null,
        ParameterInteraction interaction = ParameterInteraction.None)
    {
        var p = new ToolParameter<T>(name, value, displayName, minimum, maximum, category, order,
            choices, interaction);
        _parameters.Add(p);
        return p;
    }

    protected ToolParameter<string> AddChoiceParameter(
        string name, string value, IReadOnlyList<string> choices, string? displayName = null,
        string category = "General", int order = 0)
    {
        var p = new ToolParameter<string>(name, value, displayName, minimum: default, maximum: default,
            category, order, choices);
        _parameters.Add(p);
        return p;
    }

    public IInputPort? FindInput(string name) => _inputs.FirstOrDefault(p => p.Name == name);
    public IOutputPort? FindOutput(string name) => _outputs.FirstOrDefault(p => p.Name == name);
    public IToolParameter? FindParameter(string name) => _parameters.FirstOrDefault(p => p.Name == name);

    public void Execute(IToolContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        ValidateInputs();
        OnExecute(context);
    }

    protected abstract void OnExecute(IToolContext context);

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
}