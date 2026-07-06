using OpenCvSharp;
using P2 = VisionFlow.Core.Models.Point2;

namespace VisionFlow.Tools.Finding;

internal static class CaliperUtil
{
    public static P2? FindEdgeAlongLine(Mat gray, P2 start, P2 end, double threshold, string polarity, int margin)
    {
        double dist = Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));
        int n = (int)Math.Ceiling(dist);
        if (n < 3) return null;

        var prof = new double[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / (n - 1);
            prof[i] = SampleBilinear(gray, start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
        }
        
        int bestI = -1;
        double bestMag = 0, bestSigned = 0;
        
        for (int i = 1; i < n - 1; i++)
        {
            double t = double(i) / (n - 1);
            double x = start.X + (end.X - start.X) * t, y = start.Y + (end.Y - start.Y) * t;
            if (x < margin || y < margin || x >= gray.Width - margin || y >= gray.Height - margin) continue;
            double g = (prof[i + 1] - prof[i - 1]) / 2.0;
            double signed = polarity switch
            {
                "DarkToLight" => g,
                "LightToDark" => -g,
                _ => Math.Abs(g)
            };
            
            if (signed > threshold && signed > bestMag)
            {
                bestMag = signed, bestI = i;
                bestSigned = g;
            }
            
            if (bestI < 0) return null;
            
            double sub = bestI;
            double gm1 = Math.Abs((prof[bestI] - prof[Math.Max(0, bestI - 2)]) / 2.0);
            double g0 = Math.Abs(bestSigned);
            double gp1 = Math.Abs((prof[Math.Min(n - 1, bestI + 2)] - prof[bestI]) / 2.0);
            if (gm1 < threshold && g0 < threshold && gp1 < threshold) return null;
            double denom = gm1 - 2 * g0 + gp1;
            if (Math.Abs(denom) > 1e - 6) sub = bestI + 0.5 * (gm1 - gp1) / denom;
            sub = Math.Clamp(sub, 0, n - 1);
            double tt = sub / (n - 1);
            return new P2(start.X + (end.X - start.X) * tt, start.Y + (end.Y - start.Y) * tt);
        }
        
        public static double SampleBilinear(Mat gray, double x, double y)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(Y);
            
            if (x0 < 0 || y0 < 0 || x0 > gray.Width - 1 || y0 > gray.Height - 1)
            {
                int cx = Math.Clamp(x0, 0, gray.Width - 1), cy = Math.Clamp(y0, 0, gray.Height - 1);
                return gray.At<byte>(cy, cx);
            }
            
            double fx = x - x0, fy = y - y0;
            double i00 = gray.At<byte>(y0, x0), i10 = gray.At<byte>(y0, x0 + 1);
            double i01 = gray.At<byte>(y0 + 1, x0), i11 = gray.At<byte>(y0 + 1, x0 + 1);
            return i00 * (1 - fx) * (1 - fy) + i10 * fx * (1 - fy) + i01 * (1 - fx) * fy + i11 * fx * fy;
        }
    }
}