    // Quantower runs on Windows only – suppress cross-platform warnings
    #pragma warning disable CA1416

    // ============================================================
    //  Session Background Zones – AMP Quantower Indicator  v7.0
    //
    //  HOW SESSION TIMES WORK (Eastern Time - ET):
    //    All times are evaluated in Eastern Standard Time (EST/EDT)
    //    Tokyo    20:00 – 02:00 ET
    //    London   03:00 – 09:30 ET
    //    New York 09:30 – 16:00 ET
    //
    //  HOW X & Y COORDINATES WORK:
    //    Now using Quantower's native `IChartWindowCoordinatesConverter`
    //    GetChartX(time) and GetChartY(price).
    //    This guarantees pixel-perfect attachment to candlesticks
    //    during vertical panning, zooming, and horizontal scrolling!
    // ============================================================

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using TradingPlatform.BusinessLayer;

    namespace SessionMarkerInd
    {
        // ─── Data holder ─────────────────────────────────────────

        internal class Segment
        {
            public string Name;
            public Color  FillColor;
            public Color  BorderColor;
            public int    StartBarOffset;   // offset from newest bar (0 = newest)
            public int    EndBarOffset;     // offset from newest bar
            public double High;
            public double Low;
        }

        // ═══════════════════════════════════════════════════════════
        //  Indicator
        // ═══════════════════════════════════════════════════════════

        public class SessionMarkerInd : Indicator
        {
            // ── Tokyo ────────────────────────────────────────────
            [InputParameter("Tokyo Start (ET HH:MM)", 10)]
            public string TokyoStart = "20:00";

            [InputParameter("Tokyo End (ET HH:MM)", 11)]
            public string TokyoEnd   = "02:00";

            [InputParameter("Tokyo Box Color", 12)]
            public Color TokyoColor  = Color.MediumPurple;

            [InputParameter("Tokyo Box Opacity (0-1)", 13)]
            public double TokyoOpacity = 0.18;

            // ── London ───────────────────────────────────────────
            [InputParameter("London Start (ET HH:MM)", 20)]
            public string LondonStart = "03:00";

            [InputParameter("London End (ET HH:MM)", 21)]
            public string LondonEnd   = "09:30";

            [InputParameter("London Box Color", 22)]
            public Color LondonColor  = Color.CadetBlue;

            [InputParameter("London Box Opacity (0-1)", 23)]
            public double LondonOpacity = 0.18;

            // ── New York ─────────────────────────────────────────
            [InputParameter("New York Start (ET HH:MM)", 30)]
            public string NewYorkStart = "09:30";

            [InputParameter("New York End (ET HH:MM)", 31)]
            public string NewYorkEnd   = "16:00";

            [InputParameter("New York Box Color", 32)]
            public Color NewYorkColor  = Color.OrangeRed;

            [InputParameter("New York Box Opacity (0-1)", 33)]
            public double NewYorkOpacity = 0.18;

            // ── Style ────────────────────────────────────────────
            [InputParameter("Border Width (px)", 40)]
            public int BorderWidth = 1;

            [InputParameter("Label Font Size", 41)]
            public int LabelFontSize = 11;

            [InputParameter("Show Labels", 42)]
            public bool ShowLabels = true;

            [InputParameter("Show H/L Dashed Lines", 43)]
            public bool ShowHLLines = true;

            // ── Internals ────────────────────────────────────────
            private List<Segment> _segments = new List<Segment>();
            private Font          _font;
            private static readonly TimeZoneInfo _easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

            // ════════════════════════════════════════════════════

            public SessionMarkerInd()
            {
                Name           = "Session Background Zones";
                Description    = "H/L boxes for Tokyo, London, New York sessions. Anchored to Eastern Time (ET).";
                SeparateWindow = false;
                AllowFitAuto   = false;
            }

            protected override void OnInit()
            {
                _segments = new List<Segment>();
                RebuildFont();
            }

            protected override void OnClear()
            {
                _segments.Clear();
                DisposeFont();
            }

            // ════════════════════════════════════════════════════
            //  PER-BAR: build / update all session segments
            // ════════════════════════════════════════════════════

            protected override void OnUpdate(UpdateArgs args)
            {
                if (args.Reason == UpdateReason.NewTick && Count > 1)
                    return;

                _segments.Clear();

                ScanSession("Tokyo",    TokyoStart,   TokyoEnd,   TokyoColor,   TokyoOpacity);
                ScanSession("London",   LondonStart,  LondonEnd,  LondonColor,  LondonOpacity);
                ScanSession("New York", NewYorkStart, NewYorkEnd, NewYorkColor, NewYorkOpacity);
            }

            private void ScanSession(string name, string startStr, string endStr,
                                    Color color, double opacity)
            {
                if (!TimeSpan.TryParse(startStr, out TimeSpan tStart) ||
                    !TimeSpan.TryParse(endStr,   out TimeSpan tEnd))
                    return;

                int    segStartOffset = -1;
                double segHigh  = double.MinValue;
                double segLow   = double.MaxValue;

                // Iterate from oldest (Count-1) to newest (0)
                for (int offset = Count - 1; offset >= 0; offset--)
                {
                    DateTime barTimeUTC;
                    double hi, lo;
                    try
                    {
                        barTimeUTC = Time(offset);
                        if (barTimeUTC.Kind == DateTimeKind.Unspecified)
                            barTimeUTC = DateTime.SpecifyKind(barTimeUTC, DateTimeKind.Utc);
                            
                        hi = High(offset);
                        lo = Low(offset);
                    }
                    catch { continue; }

                    // Convert to Eastern Time
                    DateTime etTime = TimeZoneInfo.ConvertTimeFromUtc(barTimeUTC, _easternZone);
                    bool inside = IsInSession(etTime.TimeOfDay, tStart, tEnd);

                    if (inside)
                    {
                        if (segStartOffset == -1) 
                        { 
                            segStartOffset = offset; 
                            segHigh = hi; 
                            segLow = lo; 
                        }
                        else 
                        { 
                            if (hi > segHigh) segHigh = hi; 
                            if (lo < segLow) segLow = lo; 
                        }
                    }
                    else if (segStartOffset != -1)
                    {
                        CommitSegment(name, color, opacity, segStartOffset, offset + 1, segHigh, segLow);
                        segStartOffset = -1; segHigh = double.MinValue; segLow = double.MaxValue;
                    }
                }
                if (segStartOffset != -1)
                    CommitSegment(name, color, opacity, segStartOffset, 0, segHigh, segLow);
            }

            private void CommitSegment(string name, Color color, double opacity,
                                    int startOffset, int endOffset, double high, double low)
            {
                double op    = Math.Max(0.0, Math.Min(1.0, double.IsNaN(opacity) ? 0.18 : opacity));
                int    alpha = (int)Math.Round(op * 255.0);
                _segments.Add(new Segment
                {
                    Name           = name,
                    FillColor      = Color.FromArgb(alpha, color.R, color.G, color.B),
                    BorderColor    = Color.FromArgb(Math.Min(255, alpha + 80), color.R, color.G, color.B),
                    StartBarOffset = startOffset, // Older bar (higher index)
                    EndBarOffset   = endOffset,   // Newer bar (lower index)
                    High           = high,
                    Low            = low
                });
            }

            // ════════════════════════════════════════════════════
            //  PAINT
            // ════════════════════════════════════════════════════

            public override void OnPaintChart(PaintChartEventArgs args)
            {
                try
                {
                    var gr       = args.Graphics;
                    var rect     = args.Rectangle;
                    
                    if (gr == null || Count == 0 || _segments == null || CurrentChart == null)
                        return;

                    if (_font == null) RebuildFont();

                    // ── USE NATIVE QUANTOWER COORDINATE CONVERTER ────────
                    var converter = CurrentChart.MainWindow.CoordinatesConverter;
                    if (converter == null) return;
                    
                    // Get the bar width in pixels to correctly pad the right side of the box
                    double x1_test = converter.GetChartX(Time(Count > 1 ? 1 : 0));
                    double x0_test = converter.GetChartX(Time(0));
                    double barWidth = Math.Max(1.0, Math.Abs(x0_test - x1_test));

                    // ── Draw each session box ────────────────────────────
                    foreach (var seg in _segments)
                    {
                        // Skip if segment is completely outside the visible time range
                        DateTime startTime = Time(seg.StartBarOffset);
                        DateTime endTime   = Time(seg.EndBarOffset);
                        
                        double x1 = converter.GetChartX(startTime);
                        double x2 = converter.GetChartX(endTime) + barWidth; // extend to cover the last candle
                        
                        // If the box is entirely off-screen horizontally, skip drawing
                        if (Math.Max(x1, x2) < rect.Left || Math.Min(x1, x2) > rect.Right)
                            continue;

                        float w = (float)Math.Abs(x2 - x1);
                        float drawX = (float)Math.Min(x1, x2);

                        // Y – native convert H and L to pixel rows
                        float yHi = (float)converter.GetChartY(seg.High);
                        float yLo = (float)converter.GetChartY(seg.Low);
                        if (yHi > yLo) { float t = yHi; yHi = yLo; yLo = t; } // Ensure yHi is top of screen (lower Y value)

                        float h = Math.Max(1f, yLo - yHi);

                        // Fill
                        using (var brush = new SolidBrush(seg.FillColor))
                            gr.FillRectangle(brush, drawX, yHi, w, h);

                        // Border
                        if (BorderWidth > 0)
                            using (var pen = new Pen(seg.BorderColor, BorderWidth))
                                gr.DrawRectangle(pen, drawX, yHi, w, h);

                        // H/L dashed lines
                        if (ShowHLLines)
                            using (var pen = new Pen(seg.BorderColor, 1) { DashStyle = DashStyle.Dash })
                            {
                                gr.DrawLine(pen, drawX, yHi, drawX + w, yHi);
                                gr.DrawLine(pen, drawX, yLo, drawX + w, yLo);
                            }

                        // Label at top-left inside box
                        if (ShowLabels && _font != null)
                        {
                            const float PAD = 5f;
                            SizeF sz = gr.MeasureString(seg.Name, _font);
                            float tx  = drawX + PAD;
                            float ty  = yHi + PAD;
                            if (ty + sz.Height > yLo)  ty = yLo - sz.Height - PAD;
                            if (ty < rect.Top + 2)     ty = rect.Top + 2f;

                            using (var sh = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
                                gr.DrawString(seg.Name, _font, sh, tx + 1f, ty + 1f);
                            using (var tb = new SolidBrush(Color.White))
                                gr.DrawString(seg.Name, _font, tb, tx, ty);
                        }
                    }
                }
                catch { /* swallow drawing exceptions to not crash the chart */ }
            }

            // ════════════════════════════════════════════════════
            //  Helpers
            // ════════════════════════════════════════════════════

            private static bool IsInSession(TimeSpan tod, TimeSpan start, TimeSpan end)
            {
                if (start < end) return tod >= start && tod < end;   // normal hours
                return tod >= start || tod < end;                    // cross-midnight
            }

            private void RebuildFont()
            {
                DisposeFont();
                _font = new Font("Segoe UI", Math.Max(8, LabelFontSize),
                                FontStyle.Bold, GraphicsUnit.Pixel);
            }

            private void DisposeFont()
            {
                _font?.Dispose();
                _font = null;
            }
        }
    }
