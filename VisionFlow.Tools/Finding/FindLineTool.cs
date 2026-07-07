using OpenCvSharp;
using VisionFlow.Core.Imaging;
using VisionFlow.Core.Models;
using VisionFlow.Core.Ports;
using VisionFlow.Core.Tools;
using VisionFlow.Tools.Imaging;
using P2 = VisionFlow.Core.Models.Point2d;

namespace VisionFlow.Tools.Finding;

[ToolMetadata("FindLine", DisplayName = "Find Line", Category = "Detection", Description = "Caliper-based line finder (COGNEX-style edge models + RANSAC + PCA fit).")]
public sealed class FindLineTool : VisionTool
{
    private readonly InputPort<IVisionImage> _input;
    private readonly OutputPort<IVisionImage> _outImage;
    private readonly OutputPort<LineResult> _outLine;
    private readonly OutputPort<P2[]> _outEdges;
    private readonly OutputPort<double> _outRms;
    private readonly OutputPort<double> _outScore;

    private readonly ToolParameter<RotatedRectRegion> _region;
    
    private readonly ToolParameter<int> _numCalipers;
    private readonly ToolParameter<double> _caliperWidth;     
    private readonly ToolParameter<double> _caliperSpacing;   
    private readonly ToolParameter<bool> _dirForward;


    private readonly ToolParameter<double> _edgeThreshold;
    private readonly ToolParameter<string> _edgePolarity;
    private readonly ToolParameter<double> _edgeFilterWidth;  
    private readonly ToolParameter<double> _minEdgeStrength;  
    private readonly ToolParameter<int> _maxEdgesPerCaliper;  
    private readonly ToolParameter<bool> _subPixel;         
    
    private readonly ToolParameter<int> _minEdgePoints;
    private readonly ToolParameter<double> _maxRms;
    private readonly ToolParameter<double> _minScore;

    private readonly ToolParameter<bool> _outlierRemoval;
    private readonly ToolParameter<double> _outlierThreshold;
    private readonly ToolParameter<double> _minInlierRatio;
    
    private readonly ToolParameter<bool> _drawFitted;
    private readonly ToolParameter<bool> _drawEdges;
    private readonly ToolParameter<bool> _drawRegion;

    public FindLineTool()
    {
        _input = AddInput<IVisionImage>("Image", "Image");
        _outImage = AddOutput<IVisionImage>("Image", "Overlay");
        _outLine = AddOutput<LineResult>("Line", "Line");
        _outEdges = AddOutput<P2[]>("EdgePoints", "Edge Points");
        _outRms = AddOutput<double>("RMSError", "RMS Error");
        _outScore = AddOutput<double>("Score", "Score");
        
        _region = AddParameter("Region", new RotatedRectRegion(new P2(320, 240), 200, 50, 0), "Search Region",
            category: "Region", order: 1, interaction: ParameterInteraction.RotatedRectRegion);

        _numCalipers = AddParameter("NumberOfCalipers", 10, "Number Of Calipers", 1, 10000, category: "Detection", order: 1);
        _caliperWidth = AddParameter("CaliperWidth", 3.0, "Caliper Width", 1.0, 10000.0, category: "Detection", order: 2);
        _caliperSpacing = AddParameter("CaliperSpacing", 2.0, "Caliper Spacing", 0.0, 100000.0, category: "Detection", order: 3);
        _dirForward = AddParameter("CaliperDirectionForward", true, "Search Forward", category: "Detection", order: 4);

        _edgeThreshold = AddParameter("EdgeThreshold", 1.5, "Edge Threshold", 0.0, 255.0, category: "Threshold", order: 1);
        _edgePolarity = AddChoiceParameter("EdgePolarity", "Either", new[] { "DarkToLight", "LightToDark", "Either" }, "Edge Polarity", category: "Threshold", order: 2);
        _edgeFilterWidth = AddParameter("EdgeFilterWidth", 1.0, "Edge Filter Width", 0.0, 10000.0, category: "Threshold", order: 3);
        _minEdgeStrength = AddParameter("MinEdgeStrength", 2.0, "Min Edge Strength", 0.0, 255.0, category: "Threshold", order: 4);
        _maxEdgesPerCaliper = AddParameter("MaxEdgesPerCaliper", 1, "Max Edges Per Caliper", 1, 100000, category: "Detection", order: 5);
        _subPixel = AddParameter("SubPixelAccuracy", true, "Sub-Pixel Accuracy", category: "Advanced", order: 6);

        _minEdgePoints = AddParameter("MinEdgePoints", 3, "Min Edge Points", 1, 1000000, category: "Threshold", order: 5);
        _maxRms = AddParameter("MaxRMSError", 50.0, "Max RMS Error", 0.0, 100000.0, category: "Threshold", order: 6);
        _minScore = AddParameter("MinScore", 0.3, "Min Score", 0.0, 1.0, category: "Threshold", order: 7);

        _outlierRemoval = AddParameter("EnableOutlierRemoval", true, "Outlier Removal", category: "Advanced", order: 1);
        _outlierThreshold = AddParameter("OutlierThreshold", 2.0, "Outlier Threshold", 0.0, 100000.0, category: "Advanced", order: 2);
        _minInlierRatio = AddParameter("MinInlierRatio", 0.7, "Min Inlier Ratio", 0.0, 1.0, category: "Advanced", order: 3);

        _drawFitted = AddParameter("DrawFittedLine", true, "Draw Fitted Line", category: "Display", order: 1);
        _drawEdges = AddParameter("DrawEdgePoints", true, "Draw Edge Points", category: "Display", order: 2);
        _drawRegion = AddParameter("DrawSearchRegion", true, "Draw Search Region", category: "Display", order: 3);
    }
    
    protected override void OnExecute(IToolContext context)
    {
        var srcImg = _input.Value!.AsMat();
        
        Mat overlay;
        if (srcImg.Channels() == 1)
        {
            overlay = new Mat();
            Cv2.CvtColor(srcImg, overlay, ColorConversionCodes.GRAY2BGR);
        }
        else overlay = srcImg.Clone();
        
        Mat gray;
        var grayOwned = false;
        if (srcImg.Channels() == 1) gray = srcImg;
        else { gray = new Mat(); Cv2.CvtColor(srcImg, gray, ColorConversionCodes.BGR2GRAY); grayOwned = true; }
        
        var calipers = GenerateRectangleCalipers();
        var allEdges = new List<P2>();
        foreach (var (start, end) in calipers)
            allEdges.AddRange(ProcessCaliperAdvanced(gray, start, end));
        
        var filtered = allEdges;
        if (_outlierRemoval.Value && allEdges.Count >= _minEdgePoints.Value)
            filtered = RemoveOutliers(allEdges);
        
        var result = new LineResult { Judge = Judge.NG };
        double rms = 0, score = 0;
        var outEdges = allEdges;

        LineSegment seg = default;
        double fittedRms = 0;
        bool haveFit = filtered.Count >= _minEdgePoints.Value && FitLineLeastSquares(filtered, out seg, out fittedRms);
        if (haveFit)
        {
            rms = fittedRms;
            score = CalculateLineScore(filtered, fittedRms);
            result.Segment = seg;
            result.AngleDeg = Math.Atan2(seg.P2.Y - seg.P1.Y, seg.P2.X - seg.P1.X) * 180.0 / Math.PI;
            result.Judge = fittedRms <= _maxRms.Value && score >= _minScore.Value ? Judge.OK : Judge.NG;
            if (result.Judge == Judge.OK) outEdges = filtered;
        }
        
        if (_drawRegion.Value)
        {
            DrawRotatedRegion(overlay, new Scalar(255, 255, 0)); 
            DrawCalipersOnImage(overlay, calipers);                 
        }
        if (_drawEdges.Value)
            foreach (var e in allEdges) Cv2.Circle(overlay, (int)e.X, (int)e.Y, 2, new Scalar(102, 255, 0), -1); // điểm cạnh (lime)
        if (_drawFitted.Value && haveFit)
            Cv2.Line(overlay, (int)seg.P1.X, (int)seg.P1.Y, (int)seg.P2.X, (int)seg.P2.Y,
                result.Judge == Judge.OK ? new Scalar(0, 165, 255) : new Scalar(0, 0, 255), 2); // OK = cam, NG = đỏ

        if (grayOwned) gray.Dispose();

        _outImage.Value = new MatVisionImage(overlay);
        _outLine.Value = result;
        _outEdges.Value = outEdges.ToArray();
        _outRms.Value = rms;
        _outScore.Value = score;
        context.Log($"FindLine: edges={allEdges.Count} kept={filtered.Count} judge={result.Judge} angle={result.AngleDeg:F1} rms={rms:F2} score={score:F2}");
    }
    
    private List<(P2 start, P2 end)> GenerateRectangleCalipers()
    {
        var list = new List<(P2 start, P2 end)>();
        var r = _region.Value;
        double cx = r.Center.X, cy = r.Center.Y;
        double regionWidth = r.Width, caliperLength = r.Height;
        double angle = r.AngleDeg * Math.PI / 180.0;
        int n = Math.Max(1, _numCalipers.Value);
        bool forward = _dirForward.Value;
        
        bool searchingForHorizontalLine = regionWidth > caliperLength;

        for (int i = 0; i < n; i++)
        {
            double t = (double)i / Math.Max(1, n - 1);
            P2 start, end;

            if (searchingForHorizontalLine)
            {
                double x = cx + (t - 0.5) * regionWidth;
                double y = cy;
                (start, end) = forward
                    ? (new P2(x, y - caliperLength / 2), new P2(x, y + caliperLength / 2))  
                    : (new P2(x, y + caliperLength / 2), new P2(x, y - caliperLength / 2));  
            }
            else
            {
                double x = cx;
                double y = cy + (t - 0.5) * caliperLength;
                (start, end) = forward
                    ? (new P2(x - caliperLength / 2, y), new P2(x + caliperLength / 2, y))   
                    : (new P2(x + caliperLength / 2, y), new P2(x - caliperLength / 2, y));   
            }

            if (Math.Abs(angle) > 0.01)
            {
                start = RotatePoint(start, cx, cy, angle);
                end = RotatePoint(end, cx, cy, angle);
            }
            list.Add((start, end));
        }
        return list;
    }
    
    private List<P2> ProcessCaliperAdvanced(Mat gray, P2 start, P2 end)
    {
        var valid = new List<P2>();
        double dx = end.X - start.X, dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1) return valid;

        var profile = Extract1DProfile(gray, start, end);
        if (profile.Length < 3) return valid;

        var responses = Apply1DEdgeKernels(profile);
        var candidates = DetectEdgesWithModels(responses, start, end);
        var scored = ScoreEdgeCandidates(candidates, profile);

        if (scored.Count > 0)
        {
            double thr = _minScore.Value * 0.5;
            var best = scored.OrderByDescending(e => e.score).First();
            if (best.score >= thr) valid.Add(best.point);
        }
        return valid;
    }

    private static double[] Extract1DProfile(Mat gray, P2 start, P2 end)
    {
        double dx = end.X - start.X, dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        int numSamples = Math.Max(3, (int)length);
        var profile = new double[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / (numSamples - 1);
            double x = start.X + t * dx, y = start.Y + t * dy;
            profile[i] = (x >= 0 && x < gray.Width - 1 && y >= 0 && y < gray.Height - 1)
                ? GetInterpolatedPixel(gray, x, y)
                : 0;
        }
        return profile;
    }

    private static double[] Apply1DEdgeKernels(double[] profile)
    {
        if (profile.Length < 3) return Array.Empty<double>();
        var responses = new double[profile.Length];
        for (int i = 1; i < profile.Length - 1; i++)
        {
            double sobel = profile[i + 1] - profile[i - 1];   // [-1,0,1]
            double roberts = profile[i] - profile[i - 1];     // [1,-1]
            double prewitt = (profile[i + 1] - profile[i - 1]) * 0.5;
            responses[i] = sobel * 0.5 + roberts * 0.3 + prewitt * 0.2;
        }
        return responses;
    }

    private List<(P2 point, double strength, string model)> DetectEdgesWithModels(double[] responses, P2 start, P2 end)
    {
        var candidates = new List<(P2 point, double strength, string model)>();
        double dx = end.X - start.X, dy = end.Y - start.Y;
        double threshold = _edgeThreshold.Value;
        string polarity = _edgePolarity.Value;

        for (int i = 1; i < responses.Length - 1; i++)
        {
            double response = responses[i];
            string bestModel = "None";
            double bestStrength = 0;

            if (Math.Abs(response) > threshold)
            {
                bool validPolarity = polarity switch
                {
                    "DarkToLight" => response > threshold,
                    "LightToDark" => response < -threshold,
                    _ => Math.Abs(response) > threshold
                };
                if (validPolarity && Math.Abs(response) > bestStrength)
                {
                    bestStrength = Math.Abs(response);
                    bestModel = "SharpEdge";
                }
            }
            
            if (i >= 2 && i < responses.Length - 2)
            {
                double stepStrength = Math.Abs(responses[i - 1] + response + responses[i + 1]) / 3.0;
                if (stepStrength > threshold * 0.7 && stepStrength > bestStrength)
                {
                    bestStrength = stepStrength;
                    bestModel = "StepEdge";
                }
            }
            
            if (Math.Abs(response) > Math.Abs(responses[i - 1]) &&
                Math.Abs(response) > Math.Abs(responses[i + 1]) &&
                Math.Abs(response) > threshold * 0.5)
            {
                if (Math.Abs(response) > bestStrength)
                {
                    bestStrength = Math.Abs(response);
                    bestModel = "PeakEdge";
                }
            }

            if (bestModel != "None")
            {
                double t = (double)i / (responses.Length - 1);
                candidates.Add((new P2(start.X + t * dx, start.Y + t * dy), bestStrength, bestModel));
            }
        }
        return candidates;
    }

    private List<(P2 point, double score)> ScoreEdgeCandidates(
        List<(P2 point, double strength, string model)> candidates, double[] profile)
    {
        var scored = new List<(P2 point, double score)>();
        double threshold = _edgeThreshold.Value;
        double contrastScore = LocalContrast(profile);

        foreach (var (point, strength, model) in candidates)
        {
            double score = 0;
            score += Math.Min(1.0, strength / (threshold * 3)) * 0.4;    
            score += (model switch                                      
            {
                "SharpEdge" => 1.0,
                "StepEdge" => 0.8,
                "PeakEdge" => 0.6,
                _ => 0.0
            }) * 0.2;
            score += contrastScore * 0.2;                            
            score += 0.8 * 0.2;                                       
            scored.Add((point, score));
        }
        return scored;
    }

    private static double LocalContrast(double[] profile)
    {
        if (profile.Length < 3) return 0;
        double range = profile.Max() - profile.Min();
        return range > 0 ? Math.Min(1.0, range / 255.0) : 0;
    }

    private static double GetInterpolatedPixel(Mat image, double x, double y)
    {
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        int x1 = Math.Min(x0 + 1, image.Width - 1), y1 = Math.Min(y0 + 1, image.Height - 1);
        x0 = Math.Max(0, x0); y0 = Math.Max(0, y0);
        double fx = x - x0, fy = y - y0;
        double p00 = image.At<byte>(y0, x0), p01 = image.At<byte>(y1, x0);
        double p10 = image.At<byte>(y0, x1), p11 = image.At<byte>(y1, x1);
        return p00 * (1 - fx) * (1 - fy) + p10 * fx * (1 - fy) + p01 * (1 - fx) * fy + p11 * fx * fy;
    }
    
    private List<P2> RemoveOutliers(List<P2> edgePoints)
    {
        if (edgePoints.Count < _minEdgePoints.Value) return edgePoints;

        var bestInliers = new List<P2>();
        double bestScore = 0;
        int iterations = Math.Min(50, edgePoints.Count * edgePoints.Count);
        var random = new Random();

        for (int iter = 0; iter < iterations; iter++)
        {
            var sample = edgePoints.OrderBy(_ => random.Next()).Take(2).ToList();
            if (sample.Count < 2) continue;
            if (!FitLineLeastSquares(sample, out var candidate, out _)) continue;

            var inliers = edgePoints.Where(p => DistancePointToLine(p, candidate) <= _outlierThreshold.Value).ToList();
            double inlierRatio = (double)inliers.Count / edgePoints.Count;
            if (inliers.Count >= _minEdgePoints.Value && inlierRatio >= _minInlierRatio.Value)
            {
                double score = inlierRatio * inliers.Count;
                if (score > bestScore) { bestScore = score; bestInliers = inliers; }
            }
        }
        return bestInliers.Count > 0 ? bestInliers : edgePoints;
    }

    private static double DistancePointToLine(P2 point, LineSegment line)
    {
        double a = line.P2.Y - line.P1.Y;
        double b = line.P1.X - line.P2.X;
        double c = line.P2.X * line.P1.Y - line.P1.X * line.P2.Y;
        return Math.Abs(a * point.X + b * point.Y + c) / Math.Sqrt(a * a + b * b);
    }
    
    private bool FitLineLeastSquares(List<P2> points, out LineSegment segment, out double rmsError)
    {
        segment = default;
        rmsError = double.MaxValue;
        if (points.Count < 2) return false;

        if (!FitInfiniteLine(points, out double a, out double b, out double c)) return false;
        
        if (!ProjectLineToROI(a, b, c, out segment))
            segment = SegmentFromPointExtent(points, a, b, c);

        rmsError = CalculateRMSErrorToInfiniteLine(points, a, b, c);
        return true;
    }

    private static bool FitInfiniteLine(List<P2> points, out double a, out double b, out double c)
    {
        a = b = c = 0;
        if (points.Count < 2) return false;

        double cx = points.Average(p => p.X), cy = points.Average(p => p.Y);
        double sxx = 0, sxy = 0, syy = 0;
        foreach (var p in points)
        {
            double dx = p.X - cx, dy = p.Y - cy;
            sxx += dx * dx; sxy += dx * dy; syy += dy * dy;
        }
        sxx /= points.Count; sxy /= points.Count; syy /= points.Count;

        double theta = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
        a = Math.Sin(theta);
        b = -Math.Cos(theta);
        c = -(a * cx + b * cy);

        double norm = Math.Sqrt(a * a + b * b);
        if (norm < 1e-10) return false;
        a /= norm; b /= norm; c /= norm;
        return true;
    }

    private bool ProjectLineToROI(double a, double b, double c, out LineSegment segment)
    {
        segment = default;
        var r = _region.Value;
        double cx = r.Center.X, cy = r.Center.Y;
        double hw = r.Width / 2, hh = r.Height / 2;     
        double rad = r.AngleDeg * Math.PI / 180.0;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);

        var corners = new[]
        {
            new P2(cx + (-hw * cos - (-hh) * sin), cy + (-hw * sin + (-hh) * cos)), // TL
            new P2(cx + ( hw * cos - (-hh) * sin), cy + ( hw * sin + (-hh) * cos)), // TR
            new P2(cx + ( hw * cos -   hh  * sin), cy + ( hw * sin +   hh  * cos)), // BR
            new P2(cx + (-hw * cos -   hh  * sin), cy + (-hw * sin +   hh  * cos)), // BL
        };

        var intersections = new List<P2>();
        for (int i = 0; i < 4; i++)
            if (GetLineSegmentIntersection(a, b, c, corners[i], corners[(i + 1) % 4], out var p))
                intersections.Add(p);

        var unique = intersections
            .GroupBy(p => (X: Math.Round(p.X, 2), Y: Math.Round(p.Y, 2)))
            .Select(g => g.First())
            .ToList();
        if (unique.Count < 2) return false;

        double maxDist = 0;
        P2 p1 = unique[0], p2 = unique[1];
        for (int i = 0; i < unique.Count; i++)
            for (int j = i + 1; j < unique.Count; j++)
            {
                double d = Math.Sqrt(Math.Pow(unique[i].X - unique[j].X, 2) + Math.Pow(unique[i].Y - unique[j].Y, 2));
                if (d > maxDist) { maxDist = d; p1 = unique[i]; p2 = unique[j]; }
            }

        segment = new LineSegment(p1, p2);
        return true;
    }

    private static bool GetLineSegmentIntersection(double a, double b, double c, P2 p1, P2 p2, out P2 result)
    {
        result = default;
        double dx = p2.X - p1.X, dy = p2.Y - p1.Y;
        double denom = a * dx + b * dy;
        if (Math.Abs(denom) < 1e-10) return false;

        double t = -(a * p1.X + b * p1.Y + c) / denom;
        if (t < -1e-6 || t > 1 + 1e-6) return false;

        result = new P2(p1.X + t * dx, p1.Y + t * dy);
        return true;
    }
    
    private static LineSegment SegmentFromPointExtent(List<P2> points, double a, double b, double c)
    {
        double dirX = -b, dirY = a;             
        double cx = points.Average(p => p.X), cy = points.Average(p => p.Y);
        double dist = a * cx + b * cy + c;
        double fx = cx - a * dist, fy = cy - b * dist; 
        double tmin = double.MaxValue, tmax = double.MinValue;
        foreach (var p in points)
        {
            double t = (p.X - fx) * dirX + (p.Y - fy) * dirY;
            if (t < tmin) tmin = t;
            if (t > tmax) tmax = t;
        }
        return new LineSegment(
            new P2(fx + dirX * tmin, fy + dirY * tmin),
            new P2(fx + dirX * tmax, fy + dirY * tmax));
    }

    private static double CalculateRMSErrorToInfiniteLine(List<P2> points, double a, double b, double c)
    {
        double sumSq = 0;
        foreach (var p in points)
        {
            double d = Math.Abs(a * p.X + b * p.Y + c);  
            sumSq += d * d;
        }
        return Math.Sqrt(sumSq / points.Count);
    }
    
    private double CalculateLineScore(List<P2> points, double rmsError)
    {
        double rmsScore = Math.Max(0.0, 1.0 - rmsError / _maxRms.Value);
        double pointsScore = Math.Min(1.0, (double)points.Count / (_minEdgePoints.Value * 2));
        double consistency = CalculateEdgeConsistency(points);
        return rmsScore * 0.4 + pointsScore * 0.3 + consistency * 0.3;
    }

    private static double CalculateEdgeConsistency(List<P2> points)
    {
        if (points.Count < 3) return 1.0;
        var distances = new List<double>();
        for (int i = 1; i < points.Count; i++)
        {
            double dx = points[i].X - points[i - 1].X, dy = points[i].Y - points[i - 1].Y;
            distances.Add(Math.Sqrt(dx * dx + dy * dy));
        }
        if (distances.Count == 0) return 1.0;

        double avg = distances.Average();
        if (avg <= 0) return 1.0;
        double variance = distances.Select(d => Math.Pow(d - avg, 2)).Average();
        return Math.Max(0.0, 1.0 - Math.Sqrt(variance) / avg);
    }
    
    private static P2 RotatePoint(P2 p, double cx, double cy, double angle)
    {
        double cos = Math.Cos(angle), sin = Math.Sin(angle);
        double dx = p.X - cx, dy = p.Y - cy;
        return new P2(cx + dx * cos - dy * sin, cy + dx * sin + dy * cos);
    }
    
    private static void DrawCalipersOnImage(Mat img, List<(P2 start, P2 end)> calipers)
    {
        var body = new Scalar(51, 214, 51);   
        var head = new Scalar(0, 215, 255);    
        foreach (var (s, e) in calipers)
        {
            Cv2.Line(img, (int)s.X, (int)s.Y, (int)e.X, (int)e.Y, body, 1);
            double dx = e.X - s.X, dy = e.Y - s.Y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) continue;
            double ux = dx / len, uy = dy / len;
            const double h = 6;
            Cv2.Line(img, (int)e.X, (int)e.Y, (int)(e.X - h * (ux + 0.5 * uy)), (int)(e.Y - h * (uy - 0.5 * ux)), head, 1);
            Cv2.Line(img, (int)e.X, (int)e.Y, (int)(e.X - h * (ux - 0.5 * uy)), (int)(e.Y - h * (uy + 0.5 * ux)), head, 1);
        }
    }

    private void DrawRotatedRegion(Mat img, Scalar color)
    {
        var r = _region.Value;
        double cx = r.Center.X, cy = r.Center.Y, hw = r.Width / 2, hh = r.Height / 2;
        double rad = r.AngleDeg * Math.PI / 180.0, cos = Math.Cos(rad), sin = Math.Sin(rad);
        var corners = new[]
        {
            new P2(cx + (-hw * cos - (-hh) * sin), cy + (-hw * sin + (-hh) * cos)),
            new P2(cx + ( hw * cos - (-hh) * sin), cy + ( hw * sin + (-hh) * cos)),
            new P2(cx + ( hw * cos -   hh  * sin), cy + ( hw * sin +   hh  * cos)),
            new P2(cx + (-hw * cos -   hh  * sin), cy + (-hw * sin +   hh  * cos)),
        };
        for (int i = 0; i < 4; i++)
            Cv2.Line(img, (int)corners[i].X, (int)corners[i].Y,
                (int)corners[(i + 1) % 4].X, (int)corners[(i + 1) % 4].Y, color, 1);
    }
}