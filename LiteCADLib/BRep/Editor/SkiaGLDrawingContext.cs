﻿using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LiteCAD.BRep.Editor
{
    public class SkiaGLDrawingContext : AbstractDrawingContext
    {
        public override void DrawPolygon(Pen p, PointF[] pointFs)
        {

        }

        public override void FillCircle(Brush brush, float v1, float v2, int rad)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {
                var clr = (brush as SolidBrush).Color;
                paint.Color = new SKColor(clr.R, clr.G, clr.B);
                paint.IsAntialias = true;
                //paint.StrokeWidth = pen.Width;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawCircle(v1, v2, rad, paint);
            }
        }



        public override void DrawLineTransformed(Pen black, PointF point, PointF point2)
        {
            var canvas = Surface.Canvas;
            var pp = Transform(point);
            var pp2 = Transform(point2);
            DrawLine(black, pp, pp2);
        }

        public override void DrawCircle(Pen pen, float v1, float v2, float rad)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {
                var clr = pen.Color;
                paint.Color = new SKColor(clr.R, clr.G, clr.B);
                paint.IsAntialias = true;
                paint.StrokeWidth = pen.Width;
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawCircle(v1, v2, rad, paint);
            }
        }

        public override void FillRoundRectangle(Brush blue, SKRoundRect rr)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {
                var clr = (blue as SolidBrush).Color;
                paint.Color = new SKColor(clr.R, clr.G, clr.B);
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRoundRect(rr, paint);
            }
        }

        public override void DrawArrowedLine(Pen p, PointF tr0, PointF tr1, int v)
        {
            DrawLine(p, tr0, tr1);
            var canvas = Surface.Canvas;
            var sk0 = new OpenTK.Vector2d(tr0.X, tr0.Y);
            var sk1 = new OpenTK.Vector2d(tr1.X, tr1.Y);
            var dir = (sk0 - sk1).Normalized();


            var atan2 = (float)Math.Atan2(dir.Y, dir.X);
            SKPath path2 = new SKPath();
            path2.RMoveTo(-2 * v, 0);
            path2.RLineTo(0, v);
            path2.RLineTo(2 * v, -v);
            path2.RLineTo(-2 * v, -v);
            path2.RLineTo(0, v);
            path2.Close();
            var mtr = canvas.TotalMatrix;
            canvas.Translate(tr0.X, tr0.Y);
            canvas.RotateRadians(atan2);

            FillPath(new SolidBrush(p.Color), path2);
            canvas.SetMatrix(mtr);
            canvas.Translate(tr1.X, tr1.Y);
            canvas.RotateRadians(atan2);
            canvas.RotateDegrees(180);


            FillPath(new SolidBrush(p.Color), path2);
            canvas.SetMatrix(mtr);

        }

        public override void DrawRoundRectangle(Pen pen, SKRoundRect rect)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {
                var clr = pen.Color;
                paint.Color = new SKColor(clr.R, clr.G, clr.B);
                paint.IsAntialias = true;
                paint.StrokeWidth = pen.Width;
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawRoundRect(rect, paint);
            }
        }

        public override SizeF MeasureString(string text, Font font)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {

                paint.IsAntialias = true;
                //paint.StrokeWidth = pen.Width;
                paint.Style = SKPaintStyle.Fill;
                paint.TextSize = font.GetHeight();
                using (var font1 = new SKFont(SKTypeface.FromFamilyName(font.FontFamily.Name)))
                {
                    return new SizeF(paint.MeasureText(text), paint.TextSize);
                }
            }
        }

        public override void DrawLine(Pen black, PointF pp, PointF pp2)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {
                var clr = black.Color;
                paint.Color = new SKColor(clr.R, clr.G, clr.B);
                paint.IsAntialias = true;
                paint.StrokeWidth = black.Width;
                if (black.DashStyle != DashStyle.Solid)
                {
                    paint.PathEffect = SKPathEffect.CreateDash(black.DashPattern, 0);
                }
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawLine(pp.X, pp.Y, pp2.X, pp2.Y, paint);
            }
        }

        public override void DrawString(string text, Font font, Brush brush, PointF position)
        {
            DrawString(text, font, brush, position.X, position.Y);
        }

        public void FillPath(Brush red, SKPath path)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {
                var clr = (red as SolidBrush).Color;
                paint.Color = new SKColor(clr.R, clr.G, clr.B);
                paint.IsAntialias = true;
                //paint.StrokeWidth = pen.Width;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawPath(path, paint);
            }
        }

        public void DrawPath(Pen pen, SKPath path)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {
                var clr = pen.Color;
                paint.Color = new SKColor(clr.R, clr.G, clr.B);
                paint.IsAntialias = true;
                paint.StrokeWidth = pen.Width;
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawPath(path, paint);
            }
        }

        public override void FillRectangle(Brush blue, float v1, float v2, float v3, float v4)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {
                var clr = (blue as SolidBrush).Color;
                paint.Color = new SKColor(clr.R, clr.G, clr.B);
                paint.IsAntialias = true;
                //paint.StrokeWidth = pen.Width;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(v1, v2, v3, v4, paint);
            }
        }

        public override void DrawLine(Pen black, float x0, float y0, float x1, float y1)
        {
            var canvas = Surface.Canvas;
            DrawLine(black, new PointF(x0, y0), new PointF(x1, y1));
        }






        /*internal void FillEllipse(Brush black, float v1, float v2, float v3, float v4)
        {
            var pp = Transform(new PointF(v1, v2));
            gr.FillEllipse(black, pp.X, pp.Y, v3 * scale, v4 * scale);
        }*/



        public override void DrawString(string text, Font font, Brush brush, float x, float y)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {
                var clr = (brush as SolidBrush).Color;
                paint.Color = new SKColor(clr.R, clr.G, clr.B);
                paint.IsAntialias = true;
                //paint.StrokeWidth = pen.Width;
                paint.Style = SKPaintStyle.Fill;
                paint.TextSize = font.GetHeight();

                using (var font1 = new SKFont(SKTypeface.FromFamilyName(font.FontFamily.Name)))
                {
                    canvas.DrawText(text, x, y + paint.TextSize, font1, paint);
                }
            }
        }
        /*public override void DrawRectangle(SKPaint paint, float rxm, float rym, float rdx, float rdy)
        {
            var canvas = Surface.Canvas;
            canvas.DrawRect(rxm, rym, rdx, rdy, paint);
        }*/
        public override void DrawRectangle(Pen pen, float rxm, float rym, float rdx, float rdy)
        {
            var canvas = Surface.Canvas;
            using (SKPaint paint = new SKPaint())
            {
                var clr = pen.Color;
                paint.Color = clr.ToSKColor();
                paint.IsAntialias = true;
                paint.StrokeWidth = pen.Width;
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawRect(rxm, rym, rdx, rdy, paint);
            }
        }

        public override void InitGraphics()
        {

        }

        SKCanvas Canvas => Surface.Canvas;
        public override void Clear(Color white)
        {
            Canvas.Clear(white.ToSKColor());
        }

        public static bool GlSupport = true;
        public override Control GenerateRenderControl()
        {
            Control co = null;
            if (GlSupport)
            {
                co = new SKGLControl();
                ((SKGLControl)co).PaintSurface += Co_PaintSurface;
            }
            else
            {
                co = new SKControl();
                ((SKControl)co).PaintSurface += Co_PaintSurface1;
            }
            return co;
        }

        private void Co_PaintSurface1(object sender, SKPaintSurfaceEventArgs e)
        {
            Surface = e.Surface;
            PaintAction?.Invoke();
        }

        private void Co_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            Surface = e.Surface;
            PaintAction?.Invoke();
        }
    }
}
