using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MunicipalServicesApp.DataStructures;
using MunicipalServicesApp.Models;
using MunicipalServicesApp.UI;

namespace MunicipalServicesApp.Controls
{
    /// <summary>
    /// A schematic map of the suburb road network. Every road is drawn as a thin grey
    /// line; the crew route (the minimum spanning tree) is drawn on top in green,
    /// following the roads it uses. Stops with open requests are orange circles
    /// numbered in visiting order; the depot is a blue square. Hovering over a
    /// suburb shows its name and how many open requests it has.
    ///
    /// The central suburbs are only a few kilometres apart while the outlying ones
    /// are 15 to 20 km away, so a true-scale map squeezes the centre into a corner.
    /// The map is therefore drawn "fish-eye" style: each suburb keeps its direction
    /// from the middle of the network, but its distance is replaced by the square
    /// root of the distance, which spreads the centre out and pulls the edges in.
    /// </summary>
    public class RouteMapPanel : Control
    {
        /// <summary>
        /// Exponent applied to each suburb's distance from the centre: 1 is true
        /// scale, smaller values enlarge the centre.
        /// </summary>
        private const double CentreEmphasis = 0.5;

        private readonly List<SuburbLocation> _suburbs = new List<SuburbLocation>();
        private readonly List<GraphEdge<string>> _roads = new List<GraphEdge<string>>();
        private readonly Dictionary<string, PointF> _points =
            new Dictionary<string, PointF>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _stopNumbers =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly ToolTip _toolTip = new ToolTip();

        private string _depot = string.Empty;
        private string _hovered = string.Empty;
        private CrewRoutePlan? _plan;

        public RouteMapPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw, true);

            BackColor = Color.White;
            Font = Theme.SmallFont;
        }

        public void SetNetwork(IEnumerable<SuburbLocation> suburbs, IEnumerable<GraphEdge<string>> roads, string depot)
        {
            _suburbs.Clear();
            _suburbs.AddRange(suburbs);
            _roads.Clear();
            _roads.AddRange(roads);
            _depot = depot ?? string.Empty;
            Invalidate();
        }

        public void ShowPlan(CrewRoutePlan? plan)
        {
            _plan = plan;

            // Number each stop in the order the crew visits it.
            _stopNumbers.Clear();
            if (plan != null)
            {
                int number = 1;
                foreach (string stop in plan.VisitOrder)
                {
                    if (string.Equals(stop, _depot, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!_stopNumbers.ContainsKey(stop)) _stopNumbers[stop] = number++;
                }
            }

            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }

        // ------------------------------------------------------------------
        // Painting
        // ------------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (_suburbs.Count == 0) return;

            float scale = DeviceDpi / 96f;
            int legendHeight = (int)(22 * scale);
            int margin = (int)(16 * scale);
            Rectangle mapArea = new Rectangle(
                margin, margin,
                Math.Max(1, Width - margin * 2),
                Math.Max(1, Height - margin * 2 - legendHeight));

            ProjectPoints(mapArea);

            // All roads.
            using (Pen roadPen = new Pen(Theme.Border, 1.5f * scale))
            {
                foreach (GraphEdge<string> road in _roads)
                {
                    if (_points.TryGetValue(road.From, out PointF a) && _points.TryGetValue(road.To, out PointF b))
                    {
                        graphics.DrawLine(roadPen, a, b);
                    }
                }
            }

            // The crew route, along the roads each link uses.
            if (_plan != null)
            {
                using (Pen routePen = new Pen(Theme.Accent, 4 * scale))
                {
                    routePen.StartCap = LineCap.Round;
                    routePen.EndCap = LineCap.Round;
                    routePen.LineJoin = LineJoin.Round;

                    foreach (RouteLeg leg in _plan.Legs)
                    {
                        List<PointF> path = new List<PointF>();
                        foreach (string suburb in leg.RoadPath)
                        {
                            if (_points.TryGetValue(suburb, out PointF point)) path.Add(point);
                        }

                        if (path.Count >= 2) graphics.DrawLines(routePen, path.ToArray());
                    }
                }
            }

            List<RectangleF> taken = DrawMarkers(graphics, scale);
            DrawLabels(graphics, scale, taken);
            DrawLegend(graphics, scale, legendHeight);

            if (_plan != null && _plan.Sites.Count == 0)
            {
                Rectangle message = new Rectangle(0, (int)(4 * scale), Width, (int)(20 * scale));
                TextRenderer.DrawText(graphics, "No open requests for this department.", Theme.SmallBoldFont,
                    message, Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>
        /// Converts latitude and longitude to points inside the map area. Positions
        /// are first worked out in kilometres from the centre of the network; each
        /// distance is then raised to CentreEmphasis (the fish-eye effect) and the
        /// result is scaled to fit, using one scale for both axes.
        /// </summary>
        private void ProjectPoints(Rectangle area)
        {
            _points.Clear();

            double centreLat = 0;
            double centreLon = 0;
            foreach (SuburbLocation suburb in _suburbs)
            {
                centreLat += suburb.Latitude;
                centreLon += suburb.Longitude;
            }
            centreLat /= _suburbs.Count;
            centreLon /= _suburbs.Count;

            const double KmPerDegree = 111.32;
            double lonFactor = Math.Cos(centreLat * Math.PI / 180);

            Dictionary<string, PointF> local = new Dictionary<string, PointF>(StringComparer.OrdinalIgnoreCase);
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            foreach (SuburbLocation suburb in _suburbs)
            {
                double x = (suburb.Longitude - centreLon) * lonFactor * KmPerDegree;
                double y = (centreLat - suburb.Latitude) * KmPerDegree; // north is up
                double distance = Math.Sqrt(x * x + y * y);

                if (distance > 0)
                {
                    double factor = Math.Pow(distance, CentreEmphasis) / distance;
                    x *= factor;
                    y *= factor;
                }

                local[suburb.Name] = new PointF((float)x, (float)y);
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

            double spanX = Math.Max(0.001, maxX - minX);
            double spanY = Math.Max(0.001, maxY - minY);
            double pixelsPerUnit = Math.Min(area.Width / spanX, area.Height / spanY);

            double offsetX = area.X + (area.Width - spanX * pixelsPerUnit) / 2;
            double offsetY = area.Y + (area.Height - spanY * pixelsPerUnit) / 2;

            foreach (KeyValuePair<string, PointF> entry in local)
            {
                _points[entry.Key] = new PointF(
                    (float)(offsetX + (entry.Value.X - minX) * pixelsPerUnit),
                    (float)(offsetY + (entry.Value.Y - minY) * pixelsPerUnit));
            }
        }

        /// <summary>
        /// Draws the depot, the numbered stops and the other suburbs, and returns
        /// the space each marker covers so labels can avoid it.
        /// </summary>
        private List<RectangleF> DrawMarkers(Graphics graphics, float scale)
        {
            List<RectangleF> taken = new List<RectangleF>();

            foreach (SuburbLocation suburb in _suburbs)
            {
                PointF point = _points[suburb.Name];

                if (IsDepot(suburb.Name))
                {
                    float half = 7 * scale;
                    using (SolidBrush brush = new SolidBrush(Theme.Primary))
                    {
                        graphics.FillRectangle(brush, point.X - half, point.Y - half, half * 2, half * 2);
                    }
                    taken.Add(new RectangleF(point.X - half, point.Y - half, half * 2, half * 2));
                }
                else if (_stopNumbers.TryGetValue(suburb.Name, out int number))
                {
                    float radius = 10 * scale;
                    RectangleF circle = new RectangleF(point.X - radius, point.Y - radius, radius * 2, radius * 2);

                    using (SolidBrush brush = new SolidBrush(Theme.Warning))
                    {
                        graphics.FillEllipse(brush, circle);
                    }

                    TextRenderer.DrawText(graphics, number.ToString(), Theme.SmallBoldFont, Rectangle.Round(circle),
                        Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                     TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                    taken.Add(circle);
                }
                else
                {
                    float radius = 4 * scale;
                    using (SolidBrush brush = new SolidBrush(Theme.TextMuted))
                    {
                        graphics.FillEllipse(brush, point.X - radius, point.Y - radius, radius * 2, radius * 2);
                    }
                    taken.Add(new RectangleF(point.X - radius, point.Y - radius, radius * 2, radius * 2));
                }
            }

            return taken;
        }

        /// <summary>
        /// Labels the depot and the stops first, then the other suburbs. Each label
        /// tries eight positions around its marker and takes the one that overlaps
        /// the least. Stops and the depot are always labelled; another suburb's
        /// label is left out if there is no free space for it (hover to see it).
        /// </summary>
        private void DrawLabels(Graphics graphics, float scale, List<RectangleF> taken)
        {
            List<SuburbLocation> ordered = new List<SuburbLocation>();
            foreach (SuburbLocation suburb in _suburbs)
            {
                if (IsDepot(suburb.Name) || _stopNumbers.ContainsKey(suburb.Name)) ordered.Add(suburb);
            }
            foreach (SuburbLocation suburb in _suburbs)
            {
                if (!IsDepot(suburb.Name) && !_stopNumbers.ContainsKey(suburb.Name)) ordered.Add(suburb);
            }

            RectangleF bounds = new RectangleF(0, 0, Width, Height);

            foreach (SuburbLocation suburb in ordered)
            {
                bool isDepot = IsDepot(suburb.Name);
                bool isStop = _stopNumbers.ContainsKey(suburb.Name);
                bool important = isDepot || isStop;

                string text = isDepot ? "Depot" : suburb.Name;
                Font font = important ? Theme.SmallBoldFont : Font;
                Color colour = isDepot ? Theme.Primary : (isStop ? Theme.TextPrimary : Theme.TextMuted);

                Size size = TextRenderer.MeasureText(graphics, text, font, Size.Empty, TextFormatFlags.NoPadding);
                PointF point = _points[suburb.Name];
                float gap = (isStop ? 13 : 8) * scale;
                float diagonal = gap * 0.75f;
                float w = size.Width;
                float h = size.Height;

                RectangleF[] candidates =
                {
                    new RectangleF(point.X + gap, point.Y - h / 2, w, h),                 // right
                    new RectangleF(point.X - gap - w, point.Y - h / 2, w, h),             // left
                    new RectangleF(point.X - w / 2, point.Y - gap - h, w, h),             // above
                    new RectangleF(point.X - w / 2, point.Y + gap, w, h),                 // below
                    new RectangleF(point.X + diagonal, point.Y - diagonal - h, w, h),     // above right
                    new RectangleF(point.X - diagonal - w, point.Y - diagonal - h, w, h), // above left
                    new RectangleF(point.X + diagonal, point.Y + diagonal, w, h),         // below right
                    new RectangleF(point.X - diagonal - w, point.Y + diagonal, w, h)      // below left
                };

                RectangleF? best = null;
                float bestOverlap = float.MaxValue;

                foreach (RectangleF candidate in candidates)
                {
                    if (!bounds.Contains(candidate)) continue;

                    float overlap = 0;
                    foreach (RectangleF used in taken)
                    {
                        RectangleF shared = RectangleF.Intersect(used, candidate);
                        if (!shared.IsEmpty) overlap += shared.Width * shared.Height;
                    }

                    if (overlap < bestOverlap)
                    {
                        bestOverlap = overlap;
                        best = candidate;
                    }
                }

                if (best == null) continue;
                if (!important && bestOverlap > 0) continue;

                taken.Add(best.Value);
                TextRenderer.DrawText(graphics, text, font, Point.Round(best.Value.Location), colour,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
        }

        private void DrawLegend(Graphics graphics, float scale, int legendHeight)
        {
            int y = Height - legendHeight + (int)(4 * scale);
            int x = (int)(12 * scale);
            int box = (int)(10 * scale);

            using (SolidBrush depot = new SolidBrush(Theme.Primary))
            {
                graphics.FillRectangle(depot, x, y + 2, box, box);
            }
            x += box + (int)(4 * scale);
            x += DrawLegendText(graphics, "Depot", x, y) + (int)(12 * scale);

            using (SolidBrush site = new SolidBrush(Theme.Warning))
            {
                graphics.FillEllipse(site, x, y + 2, box, box);
            }
            x += box + (int)(4 * scale);
            x += DrawLegendText(graphics, "Stop", x, y) + (int)(12 * scale);

            using (Pen route = new Pen(Theme.Accent, 4 * scale))
            {
                graphics.DrawLine(route, x, y + 7 * scale, x + 18 * scale, y + 7 * scale);
            }
            x += (int)(22 * scale);
            x += DrawLegendText(graphics, "Crew route (MST)", x, y) + (int)(12 * scale);

            using (Pen road = new Pen(Theme.Border, 1.5f * scale))
            {
                graphics.DrawLine(road, x, y + 7 * scale, x + 18 * scale, y + 7 * scale);
            }
            x += (int)(22 * scale);
            DrawLegendText(graphics, "Road  (centre enlarged)", x, y);
        }

        private int DrawLegendText(Graphics graphics, string text, int x, int y)
        {
            Size size = TextRenderer.MeasureText(graphics, text, Font, Size.Empty, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(graphics, text, Font, new Point(x, y), Theme.TextMuted,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            return size.Width;
        }

        private bool IsDepot(string name)
        {
            return string.Equals(name, _depot, StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------
        // Hover details
        // ------------------------------------------------------------------

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            float reach = 12 * DeviceDpi / 96f;
            string nearest = string.Empty;
            float nearestDistance = reach;

            foreach (KeyValuePair<string, PointF> entry in _points)
            {
                float dx = entry.Value.X - e.X;
                float dy = entry.Value.Y - e.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = entry.Key;
                }
            }

            if (nearest == _hovered) return;
            _hovered = nearest;

            if (nearest.Length == 0)
            {
                _toolTip.Hide(this);
                return;
            }

            _toolTip.Show(DescribeSuburb(nearest), this, e.X + 14, e.Y + 14);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = string.Empty;
            _toolTip.Hide(this);
        }

        private string DescribeSuburb(string name)
        {
            if (IsDepot(name))
            {
                return name + ": depot, where the crew starts and finishes";
            }

            int open = 0;
            if (_plan != null)
            {
                _plan.OpenRequestsBySuburb.TryGetValue(name, out open);
            }

            if (_stopNumbers.TryGetValue(name, out int number))
            {
                return string.Format("Stop {0}: {1}, {2} open request{3}", number, name, open, open == 1 ? "" : "s");
            }

            return name + ": no open requests for this route";
        }
    }
}
