using OpenCvSharp;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Models;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Tools;
using VisionFlow.Tools.Imaging;
using P2 = VisionFlow.Core.Models.Point2d;

namespace VisionFlow.Tools.Finding;

[ToolMetadata("FindCircle", DisplayName = "Find Circle", Category = "Detection", Description = "Caliper based circle finder : radial calipers + edge detection + circle fit")]
public sealed class FindCircleTool : VisionTool
{
    private readonly InputPort<IVisionImage> _input;
    private readonly OutputPort<IVisionImage> _outImage;
    private readonly OutputPort<CircleResult> _outCircle;
    private readonly OutputPort<P2[]> _outEdges;
    private readonly OutputPort<double> _outRms;
    private readonly OutputPort<double> _outScore;
    
    private readonly ToolParameter<CircleRegion> _region;
    private readonly ToolParameter<bool> _useImageCenter;
    private readonly ToolParameter<double> _minRadius;
    private readonly ToolParameter<double> _maxRadius;
    private readonly ToolParameter<int> _numCalipers;
    private readonly ToolParameter<double> _caliperLength;
    private readonly ToolParameter<bool> _radialOutward;
    private readonly ToolParameter<double> _edgeThreshold;
    private readonly ToolParameter<string> _edgePolarity;
    private readonly ToolParameter<int> _edgeFilterWidth;
    private readonly ToolParameter<int> _minEdgePoints;
    private readonly ToolParameter<double> _maxRms;
    private readonly ToolParameter<double> _minScore;
    private readonly ToolParameter<bool> _outlierRejection;
    private readonly <ToolParameter<double> _outlierThreshold;
    private readonly ToolParameter<bool> _drawFitted;
    private readonly ToolParameter<bool> _ drawRegion;
    private readonly ToolParameter<bool> _drawEdges;

    publuc FindCircleTool()
    {
        _inout = AddInput<IVisionImage>("Image", "Image");
        _outImage = AddOuput<IVisionImage>("Image", "Overlay");
        _outCircle = AddOuput<CircleResult>("Circle", "Circle");
        _outEdges = AddOutput<P2[]>("EdgesPoints", "Edge Points");
        _outRms = AddOuput<double>("RMSError", "RMS Error");
        _outScore = AddOuput<double>("Score", "Score");

        _region = addParameter("Region", new CircleRegion(new P2(150, 150), 100), "Search Region", category: "Region", order: 1, ParameterInteraction: ParameterInteraction.CircleRegion);
        _useImageCenter = addParameter("UseImageCenter", false, "Use Image Center", category: "Region", order: 2);
        _minRadius = addParameter("MinRadius", 5.0, "Min Radius", 1.0, 10000.0, category: "Region", order: 3);
        _maxRadius = addParameter("MaxRadius", 5000.0, "Max Radius", 1.0, 10000.0, category: "Region", order: 4);

        _numCalipers = addParameter("NumberOfCalipers", 24, "Number Of Calipers", 3, 360, category: "Detection", order: 1);
        _caliperLength = addParameter("CaliperLength", 30.0, "Caliper Length", 4.0, 500.0, category: "Detection", order: 2);
        _radialOutward = addParameter("RadialSearchDirection", true, "Search Outward", category: "Detection", order: 3);

        _edgeThreshold = addParameter("EdgeThreshold", 20.0, "Edge Threshold", 1.0, 255.0, category: "Detection", order: 1);
        _edgePolarity = AddChoiceParameter("EdgePolarity", "Either", new[] { "DarkToLight", "LightToDark", "Either" }, "Edge Polarity", category: "Threshold", order: 2);
        _edgeFilterWidth = AddParameter("EdgeFilterWidth", 1, "Edge Filter Width", 0, 20, category: "Threshold", order: 3);
        _minEdgePoints = AddParameter("MinEdgePoints", 6, "Min Edge Points", 3, 360, category: "Threshold", order: 4);
        _maxRms = AddParameter("MaxRMSError", 5.0, "Max RMS Error", 0.1, 100.0, category: "Threshold", order: 5);
        _minScore = AddParameter("MinScore", 0.3, "Min Score", 0.0, 1.0, category: "Threshold", order: 6);

        _outlierRejection = AddParameter("OutlierRejection", true, "Outlier Rejection", category: "Advanced", order: 1);
        _outlierThreshold = AddParameter("OutlierThreshold", 2.0, "Outlier Threshold", 0.5, 10.0, category: "Advanced", order: 2);

        _drawFitted = AddParameter("DrawFittedCircle", true, "Draw Fitted Circle", category: "Display", order: 1);
        _drawRegion = AddParameter("DrawSearchRegion", true, "Draw Search Region", category: "Display", order: 2);
        _drawEdges = AddParameter("DrawEdgePoints", true, "Draw Edge Points", category: "Display", order: 3);
    }

    protected override void OnExecute(IToolContext context)
    {
        var src = _input.Value!.AsMat();

        Mat gray;
        var grayOwned = false;
        if (src.Channels() == 1) gray = src;
        else { gray = new Mat(); Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY); grayOwned = true; }

        var overlay = new Mat();
        Cv2.CvtColor(gray, overlay, ColorConversionCodes.GRAY2BGR);

        var region = _region.Value;
        var cx = region.Center.X;
        var cy = region.Center.Y;
        if (_useImageCenter.Value) { cx = gray.Width / 2.0; cy = gray.Height / 2.0; }
        var rr = region.Radius;
        
        var edges = new List<P2>();
        int n = Math.Max(3, _numCalipers.Value);
        double proj = _caliperLength.Value / 2.0;
        for (int i = 0; i < n; i++)
        {
            double a = i * 2 * Math.PI / n;
            double inner = Math.Max(0, rr - proj), outer = rr + proj;
            var pIn = new P2(cx + inner * Math.Cos(a), cy + inner * Math.Sin(a));
            var pOut = new P2(cx + outer * Math.Cos(a), cy + outer * Math.Sin(a));
            var (start, end) = _radialOutward.Value ? (pIn, pOut) : (pOut, pIn);
            var edge = FindEdgeAlongLine(gray, start, end, _edgeThreshold.Value, _edgePolarity.Value, _edgeFilterWidth.Value);
            if (edge.HasValue) edges.Add(edge.Value);
        }

        var result = new CircleResult { Judge = Judge.NG };
        double rms = 0, score = 0;
        
        if (edges.Count >= _minEdgePoints.Value)
        {
            var pts = edges;
            if (_outlierRejection.Value) pts = RejectOutliers(edges, _outlierThreshold.Value);

            if (pts.Count >= 3 && FitCircleKasa(pts, out var fc, out rms))
            {
                double edgeRatio = Math.Min(1.0, (double)pts.Count / n);
                double rmsScore = Math.Max(0, 1.0 - rms / _maxRms.Value);
                double radiusScore = (fc.Radius >= _minRadius.Value && fc.Radius <= _maxRadius.Value) ? 1.0 : 0.0;
                score = Math.Clamp(0.5 * edgeRatio + 0.3 * rmsScore + 0.2 * radiusScore, 0, 1);
                bool valid = fc.Radius >= _minRadius.Value && fc.Radius <= _maxRadius.Value && rms <= _maxRms.Value && score >= _minScore.Value;
                result.Circle = fc;
                result.Score = score;
                result.Judge = valid ? Judge.OK : Judge.NG;
            }
        }
        
        if (_drawRegion.Value)
            Cv2.Circle(overlay, (int)cx, (int)cy, (int)rr, new Scalar(255, 255, 0), 1);
        if (_drawEdges.Value)
            foreach (var e in edges) Cv2.Circle(overlay, (int)e.X, (int)e.Y, 2, new Scalar(0, 255, 0), -1);
        if (_drawFitted.Value && result.Judge == Judge.OK)
        {
            Cv2.Circle(overlay, (int)result.Circle.Center.X, (int)result.Circle.Center.Y, (int)result.Circle.Radius, new Scalar(0, 255, 255), 2);
            Cv2.Circle(overlay, (int)result.Circle.Center.X, (int)result.Circle.Center.Y, 4, new Scalar(0, 0, 255), -1);
        }

        if (grayOwned) gray.Dispose();

        _outImage.Value = new MatVisionImage(overlay);
        _outCircle.Value = result;
        _outEdges.Value = edges.ToArray();
        _outRms.Value = rms;
        _outScore.Value = score;
        context.Log($"FindCircle: edges={edges.Count} judge={result.Judge} R={result.Circle.Radius:F1} rms={rms:F2} score={score:F2}");
    }
    
    private static P2? FindEdgeAlongLine(Mat gray, P2 start, P2 end, double threshold, string polarity, int margin)
    {
        double dist = Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));
        int n = (int)Math.Ceiling(dist);
        if (n < 3) return null;

        var prof = new double[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / (n - 1);
            double x = start.X + (end.X - start.X) * t;
            double y = start.Y + (end.Y - start.Y) * t;
            prof[i] = SampleBilinear(gray, x, y);
        }

        int bestI = -1; double bestMag = 0; double bestSigned = 0;
        for (int i = 1; i < n - 1; i++)
        {
            double t = (double)i / (n - 1);
            double x = start.X + (end.X - start.X) * t, y = start.Y + (end.Y - start.Y) * t;
            if (x < margin || y < margin || x >= gray.Width - margin || y >= gray.Height - margin) continue;

            double g = (prof[i + 1] - prof[i - 1]) / 2.0; 
            double signed = polarity switch
            {
                "DarkToLight" => g,  
                "LightToDark" => -g, 
                _ => Math.Abs(g)
            };
            if (signed > threshold && signed > bestMag) { bestMag = signed; bestI = i; bestSigned = g; }
        }

        if (bestI < 0) return null;
        
        double sub = bestI;
        double gm1 = Math.Abs((prof[bestI] - prof[bestI - 2 < 0 ? 0 : bestI - 2]) / 2.0);
        double g0 = Math.Abs(bestSigned);
        double gp1 = Math.Abs((prof[Math.Min(n - 1, bestI + 2)] - prof[bestI]) / 2.0);
        double denom = gm1 - 2 * g0 + gp1;
        if (Math.Abs(denom) > 1e-6) sub = bestI + 0.5 * (gm1 - gp1) / denom;
        sub = Math.Clamp(sub, 0, n - 1);

        double tt = sub / (n - 1);
        return new P2(start.X + (end.X - start.X) * tt, start.Y + (end.Y - start.Y) * tt);
    }

    private static double SampleBilinear(Mat gray, double x, double y)
    {
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        if (x0 < 0 || y0 < 0 || x0 >= gray.Width - 1 || y0 >= gray.Height - 1)
        {
            int cxp = Math.Clamp(x0, 0, gray.Width - 1), cyp = Math.Clamp(y0, 0, gray.Height - 1);
            return gray.At<byte>(cyp, cxp);
        }
        double fx = x - x0, fy = y - y0;
        double i00 = gray.At<byte>(y0, x0), i10 = gray.At<byte>(y0, x0 + 1);
        double i01 = gray.At<byte>(y0 + 1, x0), i11 = gray.At<byte>(y0 + 1, x0 + 1);
        return i00 * (1 - fx) * (1 - fy) + i10 * fx * (1 - fy) + i01 * (1 - fx) * fy + i11 * fx * fy;
    }

    private static List<P2> RejectOutliers(List<P2> pts, double k)
    {
        if (pts.Count < 4 || !FitCircleKasa(pts, out var c, out var rms) || rms <= 0) return pts;
        var kept = new List<P2>();
        foreach (var p in pts)
        {
            double d = Math.Sqrt((p.X - c.Center.X) * (p.X - c.Center.X) + (p.Y - c.Center.Y) * (p.Y - c.Center.Y));
            if (Math.Abs(d - c.Radius) <= k * rms) kept.Add(p);
        }
        return kept.Count >= 3 ? kept : pts;
    }
    
    private static bool FitCircleKasa(List<P2> pts, out Circle circle, out double rms)
    {
        circle = default; rms = double.MaxValue;
        int n = pts.Count;
        if (n < 3) return false;

        double Sx = 0, Sy = 0, Sxx = 0, Syy = 0, Sxy = 0, Sxz = 0, Syz = 0, Sz = 0;
        foreach (var p in pts)
        {
            double x = p.X, y = p.Y, z = x * x + y * y;
            Sx += x; Sy += y; Sxx += x * x; Syy += y * y; Sxy += x * y;
            Sxz += x * z; Syz += y * z; Sz += z;
        }
        
        double[,] m = { { Sxx, Sxy, Sx }, { Sxy, Syy, Sy }, { Sx, Sy, n } };
        double[] v = { Sxz, Syz, Sz };
        if (!Solve3(m, v, out var sol)) return false;

        double a = sol[0] / 2.0, b = sol[1] / 2.0;
        double r2 = sol[2] + a * a + b * b;
        if (r2 <= 0) return false;
        double r = Math.Sqrt(r2);
        circle = new Circle(new P2(a, b), r);

        double se = 0;
        foreach (var p in pts)
        {
            double d = Math.Sqrt((p.X - a) * (p.X - a) + (p.Y - b) * (p.Y - b)) - r;
            se += d * d;
        }
        rms = Math.Sqrt(se / n);
        return true;
    }

    private static bool Solve3(double[,] m, double[] v, out double[] x)
    {
        x = new double[3];
        double det =
            m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) -
            m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0]) +
            m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
        if (Math.Abs(det) < 1e-9) return false;
        double Dx =
            v[0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) -
            m[0, 1] * (v[1] * m[2, 2] - m[1, 2] * v[2]) +
            m[0, 2] * (v[1] * m[2, 1] - m[1, 1] * v[2]);
        double Dy =
            m[0, 0] * (v[1] * m[2, 2] - m[1, 2] * v[2]) -
            v[0] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0]) +
            m[0, 2] * (m[1, 0] * v[2] - v[1] * m[2, 0]);
        double Dz =
            m[0, 0] * (m[1, 1] * v[2] - v[1] * m[2, 1]) -
            m[0, 1] * (m[1, 0] * v[2] - v[1] * m[2, 0]) +
            v[0] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
        x[0] = Dx / det; x[1] = Dy / det; x[2] = Dz / det;
        return true;
    }
}