using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MunicipalServicesApp.Models;
using MunicipalServicesApp.UI;

namespace MunicipalServicesApp.Controls
{
    /// <summary>
    /// Draws a request's progress as a row of stages joined by a line:
    /// Submitted, Acknowledged, Assigned, In progress, Resolved, Closed.
    /// Stages already reached are filled in with the status colour and show the
    /// date they were reached; the current stage has a ring around it. If the
    /// request is blocked, the next stage is outlined in red.
    /// </summary>
    public class StatusTimeline : Control
    {
        private const TextFormatFlags CentredText =
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPrefix |
            TextFormatFlags.SingleLine;

        private ServiceRequest? _request;
        private bool _isBlocked;

        public StatusTimeline()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw, true);

            BackColor = Color.White;
            Font = Theme.SmallFont;
        }

        public void ShowRequest(ServiceRequest? request, bool isBlocked)
        {
            _request = request;
            _isBlocked = isBlocked;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (_request == null)
            {
                TextRenderer.DrawText(graphics, "Select a request to see its progress.", Font, ClientRectangle,
                    Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            float scale = DeviceDpi / 96f;
            int radius = (int)(8 * scale);
            int sideMargin = (int)(38 * scale);
            int lineY = (int)(16 * scale);

            RequestStatus[] stages = RequestStatusInfo.Stages;
            int current = (int)_request.Status;
            Color statusColour = Theme.GetStatusColour(_request.Status);

            float left = sideMargin;
            float right = Math.Max(left + 1, Width - sideMargin);
            float spacing = (right - left) / (stages.Length - 1);

            // Background line, then the part already travelled.
            using (Pen track = new Pen(Theme.Border, 4 * scale))
            {
                graphics.DrawLine(track, left, lineY, right, lineY);
            }

            using (Pen travelled = new Pen(statusColour, 4 * scale))
            {
                graphics.DrawLine(travelled, left, lineY, left + spacing * current, lineY);
            }

            for (int i = 0; i < stages.Length; i++)
            {
                float x = left + spacing * i;
                bool reached = i <= current;
                RectangleF dot = new RectangleF(x - radius, lineY - radius, radius * 2, radius * 2);

                if (i == current)
                {
                    float ring = radius + 4 * scale;
                    using (SolidBrush halo = new SolidBrush(Color.FromArgb(50, statusColour)))
                    {
                        graphics.FillEllipse(halo, x - ring, lineY - ring, ring * 2, ring * 2);
                    }
                }

                if (reached)
                {
                    using (SolidBrush fill = new SolidBrush(statusColour))
                    {
                        graphics.FillEllipse(fill, dot);
                    }
                }
                else
                {
                    bool blockedNext = _isBlocked && i == current + 1;
                    graphics.FillEllipse(Brushes.White, dot);
                    using (Pen outline = new Pen(blockedNext ? Theme.Danger : Theme.Border, 2 * scale))
                    {
                        if (blockedNext) outline.DashStyle = DashStyle.Dash;
                        graphics.DrawEllipse(outline, dot);
                    }
                }

                // Stage name, and the date it was reached.
                int labelWidth = (int)spacing + (int)(20 * scale);
                Rectangle nameBounds = new Rectangle(
                    (int)(x - labelWidth / 2f), lineY + radius + (int)(6 * scale), labelWidth, Font.Height + 2);

                TextRenderer.DrawText(graphics, RequestStatusInfo.GetDisplayName(stages[i]),
                    i == current ? Theme.SmallBoldFont : Font, nameBounds,
                    reached ? Theme.TextPrimary : Theme.TextMuted, CentredText);

                DateTime? reachedOn = _request.GetStageDate(stages[i]);
                if (reachedOn.HasValue)
                {
                    Rectangle dateBounds = nameBounds;
                    dateBounds.Offset(0, Font.Height + 1);
                    TextRenderer.DrawText(graphics, reachedOn.Value.ToString("d MMM"), Font, dateBounds,
                        Theme.TextMuted, CentredText);
                }
            }
        }
    }
}
