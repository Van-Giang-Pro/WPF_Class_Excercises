namespace VisionFlow.Core.Models;

public readonly record struct RotationRectRegion(Point2d Center, double Width, double Height, double Theta);

public readonly record struct CircleRegion(Point2d Center, double Radius);

public sealed record PolygonRegion(IReadOnlyList<Point2d> Points);

public readonly record struct CaliperRegion(Point2d Center, double Width, double Height, double AngleDeg, int CaliperCount);

public sealed record TemplateImageRef(RotationRectRegion? SourceRegion = null, string? FilePath = null);