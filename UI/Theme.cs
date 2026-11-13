using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.UI
{
    //Central definition of the application's colour scheme and typography.
    public static class Theme
    {
        // Colour palette
        public static readonly Color Primary = Color.FromArgb(0, 87, 146);
        public static readonly Color PrimaryHover = Color.FromArgb(0, 105, 173);
        public static readonly Color Accent = Color.FromArgb(0, 133, 103);
        public static readonly Color AccentHover = Color.FromArgb(0, 156, 121);
        public static readonly Color Surface = Color.FromArgb(245, 247, 250);
        public static readonly Color Border = Color.FromArgb(214, 220, 229);
        public static readonly Color TextPrimary = Color.FromArgb(33, 37, 41);
        public static readonly Color TextMuted = Color.FromArgb(108, 117, 125);
        public static readonly Color TextOnPrimary = Color.White;
        public static readonly Color DisabledFill = Color.FromArgb(233, 236, 239);
        public static readonly Color DisabledText = Color.FromArgb(140, 148, 158);

        // Local Events page
        public static readonly Color CardHover = Color.FromArgb(238, 244, 250);
        public static readonly Color Danger = Color.FromArgb(192, 32, 38);
        public static readonly Color Warning = Color.FromArgb(190, 112, 0);
        public static readonly Color CriticalTint = Color.FromArgb(253, 240, 240);

        // Typography
        public static readonly Font TitleFont = new Font("Segoe UI Semibold", 16F);
        public static readonly Font SubtitleFont = new Font("Segoe UI", 9.75F);
        public static readonly Font BodyFont = new Font("Segoe UI", 9.75F);
        public static readonly Font ButtonFont = new Font("Segoe UI Semibold", 10.5F);
        public static readonly Font SectionFont = new Font("Segoe UI Semibold", 11.25F);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.75F);
        public static readonly Font SmallBoldFont = new Font("Segoe UI Semibold", 8.75F);
        public static readonly Font CardTitleFont = new Font("Segoe UI Semibold", 10.5F);

        // One colour per category, used for the stripe and label on event cards.
        // Declared after the palette above because static fields are initialised in order.
        private static readonly Dictionary<string, Color> CategoryColours =
            new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                { EventCategories.CommunityMeetings, Color.FromArgb(0, 87, 146) },
                { EventCategories.WaterAndSanitation, Color.FromArgb(0, 132, 176) },
                { EventCategories.Electricity, Color.FromArgb(190, 112, 0) },
                { EventCategories.HealthAndWellness, Color.FromArgb(186, 45, 90) },
                { EventCategories.SportsAndRecreation, Color.FromArgb(0, 133, 103) },
                { EventCategories.ArtsAndCulture, Color.FromArgb(124, 70, 160) },
                { EventCategories.EnvironmentAndWaste, Color.FromArgb(84, 124, 36) },
                { EventCategories.PublicSafety, Color.FromArgb(196, 64, 32) },
                { EventCategories.RoadsAndTransport, Color.FromArgb(84, 94, 110) },
                { EventCategories.GeneralNotices, Color.FromArgb(108, 117, 125) }
            };

        public static Color GetCategoryColour(string category)
        {
            if (string.IsNullOrEmpty(category)) return Primary;

            return CategoryColours.TryGetValue(category, out Color colour) ? colour : Primary;
        }

        public static Color GetPriorityColour(AnnouncementPriority priority)
        {
            switch (priority)
            {
                case AnnouncementPriority.Critical: return Danger;
                case AnnouncementPriority.High: return Warning;
                default: return Primary;
            }
        }

        // Service Request Status page: one colour per stage of a request.
        public static Color GetStatusColour(RequestStatus status)
        {
            switch (status)
            {
                case RequestStatus.Submitted: return TextMuted;
                case RequestStatus.Acknowledged: return Primary;
                case RequestStatus.Assigned: return Color.FromArgb(124, 70, 160);
                case RequestStatus.InProgress: return Warning;
                case RequestStatus.Resolved: return Accent;
                default: return Color.FromArgb(84, 94, 110);
            }
        }

        public static Color GetRequestPriorityColour(RequestPriority priority)
        {
            switch (priority)
            {
                case RequestPriority.Critical: return Danger;
                case RequestPriority.High: return Warning;
                case RequestPriority.Medium: return Primary;
                default: return TextMuted;
            }
        }

        public static void StyleActionButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = PrimaryHover;
            button.BackColor = Primary;
            button.ForeColor = TextOnPrimary;
            button.Font = ButtonFont;
            button.Cursor = Cursors.Hand;
            button.TextAlign = ContentAlignment.MiddleCenter;
        }

        public static void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = DisabledFill;
            button.BackColor = Color.White;
            button.ForeColor = TextPrimary;
            button.Font = ButtonFont;
            button.Cursor = Cursors.Hand;
        }

        public static void StyleDisabledButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            button.BackColor = DisabledFill;
            button.ForeColor = DisabledText;
            button.Font = ButtonFont;
            button.Enabled = false;
            button.Cursor = Cursors.Default;
        }
    }
}