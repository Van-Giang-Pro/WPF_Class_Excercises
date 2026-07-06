using System.Collections;
using System.Text.Json;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Tools;

namespace VisionFlow.Tools.Output;

[ToolMetadata("DataSaver", DisplayName = "Data Saver", Category = "OutputSource", Description = "Save result data to a JSON/TXT/CSV file.")]
public sealed class DataSaverTool : VisionTool
{
    private readonly InputPort<object> _data;
    private readonly ToolParameter<string> _outputPath;
    private readonly ToolParameter<string> _fileName;
    private readonly ToolParameter<string> _format;
    private readonly ToolParameter<bool> _overwrite;
    private readonly OutputPort<string> _savedPath;

    public DataSaverTool()
    {
        _data = AddInput<object>("Data", "Data");
        _outputPath = AddParameter("OutputPath", DefaultDir(), "Output Folder", category: "Output", order: 1);
        _fileName = AddParameter("FileName", "output", "File Name", category: "Output", order: 2);
        _format = AddChoiceParameter("SaveFormat", "JSON", new[] { "JSON", "TXT", "CSV" }, "Format", category: "Format", order: 1);
        _overwrite = AddParameter("OverwriteExisting", true, "Overwrite Existing", category: "Options", order: 1);
        _savedPath = AddOutput<string>("SavedFilePath", "Saved Path");
    }

    private static string DefaultDir()
    {
        try { return Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
        catch { return Path.GetTempPath(); }
    }

    protected override void OnExecute(IToolContext context)
    {
        var data = _data.Value;
        if (data is null) throw new ToolExecutionException("No input data to save. Connect the Data input.");

        var dir = _outputPath.Value;
        if (string.IsNullOrWhiteSpace(dir)) throw new ToolExecutionException("Output Folder is empty.");
        Directory.CreateDirectory(dir);

        var ext = _format.Value.ToUpperInvariant() switch { "TXT" => ".txt", "CSV" => ".csv", _ => ".json" };
        var path = Path.Combine(dir, _fileName.Value + ext);
        if (!_overwrite.Value && File.Exists(path)) path = UniquePath(path);

        var content = ext switch
        {
            ".json" => ToJson(data),
            ".csv" => ToCsv(data),
            _ => data.ToString() ?? string.Empty
        };
        File.WriteAllText(path, content);

        _savedPath.Value = path;
        context.Log($"DataSaver: saved {path}");
    }

    private static string ToJson(object data)
    {
        try
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });
        }
        catch
        {
            return data is IVisionImage img ? $"\"<image {img.Width}x{img.Height}>\"" : $"\"{data}\"";
        }
    }

    private static string ToCsv(object data)
    {
        if (data is IEnumerable e && data is not string)
            return string.Join("\n", e.Cast<object?>().Select(x => x?.ToString() ?? ""));
        return data.ToString() ?? string.Empty;
    }

    private static string UniquePath(string path)
    {
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var i = 1;
        var p = path;
        while (File.Exists(p)) p = Path.Combine(dir, $"{name}_{i++}{ext}");
        return p;
    }
}