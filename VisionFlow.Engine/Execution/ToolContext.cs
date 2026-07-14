using VisionFlow.Core.Tools;

namespace VisionFlow.Engine.Execution;

public sealed class ToolContext : IToolContext
{
    private readonly Action<string>? _logger;

    public ToolContext(double pixelSize = 1.0, CancellationToken cancellationToken = default, Action<string>? logger = null)
    {
        PixelSize = pixelSize;
        CancellationToken = cancellationToken;
        _logger = logger;
    }
    
    public CancellationToken CancellationToken { get; }
    public double PixelSize { get; }
    public void Log(string message) => _logger?.Invoke(message);
}