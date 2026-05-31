#nullable enable
using NEA.Models;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace NEA.Rendering
{
    public class Renderer
    {
        public void DrawBodies(DrawingContext dc, Body[] bodies, Camera camera, double cx, double cy)
        {
            foreach (var b in bodies)
            {
                (double x, double y) = camera.WorldToScreen(b.position, cx, cy);

                double radius = Math.Max(4, Math.Log10(b.mass + 2) * 2.2);

                var brush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(b.hexColour)
                );

                dc.DrawEllipse(brush, null, new Point(x, y), radius, radius);
            }
        }

        public void DrawTrails(DrawingContext dc, Body[] bodies, Vector3[][] trailBuffers,
                               int[] trailCounts, int[] trailHeads, int trailLength,
                               Camera camera, double cx, double cy)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                if (trailCounts[i] < 5) continue;

                var color = (Color)ColorConverter.ConvertFromString(bodies[i].hexColour);
                var pen = new Pen(new SolidColorBrush(color), 1.4);

                var geom = new StreamGeometry();
                using (var ctx = geom.Open())
                {
                    int head = trailHeads[i];
                    int count = trailCounts[i];
                    int start = (head - count + trailLength) % trailLength;
                    bool first = true;

                    for (int k = 0; k < count; k++)
                    {
                        var p = trailBuffers[i][(start + k) % trailLength];
                        (double x, double y) = camera.WorldToScreen(p, cx, cy);

                        if (first)
                        {
                            ctx.BeginFigure(new Point(x, y), false, false);
                            first = false;
                        }
                        else
                        {
                            ctx.LineTo(new Point(x, y), true, false);
                        }
                    }
                }

                geom.Freeze();
                dc.DrawGeometry(null, pen, geom);
            }
        }
    }
}
