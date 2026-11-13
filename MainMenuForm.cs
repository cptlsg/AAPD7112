using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MunicipalServicesApp.Models;
using MunicipalServicesApp.Services;
using MunicipalServicesApp.UI;

namespace MunicipalServicesApp
{
    //application's main menu
    public partial class MainMenuForm : Form
    {
        public MainMenuForm()
        {
            InitializeComponent();
            ApplyTheme();
            WireUpEvents();
            RefreshIssueCount();
            RefreshEventsSummary();
            RefreshServiceStatusSummary();
        }

        private void ApplyTheme()
        {
            BackColor = Theme.Surface;

            headerPanel.BackColor = Theme.Primary;
            lblTitle.Font = Theme.TitleFont;
            lblTitle.ForeColor = Theme.TextOnPrimary;
            lblSubtitle.Font = Theme.SubtitleFont;
            lblSubtitle.ForeColor = Theme.TextOnPrimary;

            lblInstruction.Font = Theme.SectionFont;
            lblInstruction.ForeColor = Theme.TextPrimary;

            Theme.StyleActionButton(btnReportIssues);
            Theme.StyleActionButton(btnLocalEvents);
            btnLocalEvents.Enabled = true;
            Theme.StyleActionButton(btnServiceStatus);
            btnServiceStatus.Enabled = true;
            Theme.StyleSecondaryButton(btnExit);

            lblReportIssuesInfo.Font = Theme.BodyFont;
            lblReportIssuesInfo.ForeColor = Theme.TextPrimary;
            lblLocalEventsInfo.Font = Theme.BodyFont;
            lblLocalEventsInfo.ForeColor = Theme.TextPrimary;
            lblLocalEventsInfo.UseMnemonic = false;
            lblServiceStatusInfo.Font = Theme.BodyFont;
            lblServiceStatusInfo.ForeColor = Theme.TextPrimary;
            lblServiceStatusInfo.UseMnemonic = false;

            lblIssueCount.Font = Theme.SmallFont;
            lblIssueCount.ForeColor = Theme.TextMuted;
        }

        private void WireUpEvents()
        {
            btnReportIssues.Click += BtnReportIssues_Click;
            btnLocalEvents.Click += BtnLocalEvents_Click;
            btnServiceStatus.Click += BtnServiceStatus_Click;
            btnExit.Click += BtnExit_Click;
            Activated += MainMenuForm_Activated;
        }

        private void BtnReportIssues_Click(object sender, EventArgs e)
        {
            using (ReportIssuesForm reportForm = new ReportIssuesForm())
            {
                Hide();
                reportForm.ShowDialog(this);
                Show();
            }
        }

        private void BtnLocalEvents_Click(object? sender, EventArgs e)
        {
            using (LocalEventsForm eventsForm = new LocalEventsForm())
            {
                Hide();
                try
                {
                    eventsForm.ShowDialog(this);
                }
                finally
                {
                    // Always bring the menu back, even if something goes wrong on the events page.
                    Show();
                }
            }
        }

        private void BtnServiceStatus_Click(object? sender, EventArgs e)
        {
            using (ServiceRequestStatusForm statusForm = new ServiceRequestStatusForm())
            {
                Hide();
                try
                {
                    statusForm.ShowDialog(this);
                }
                finally
                {
                    Show();
                }
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "Are you sure you want to close the application?",
                "Confirm exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void MainMenuForm_Activated(object sender, EventArgs e)
        {
            RefreshIssueCount();
            RefreshEventsSummary();
            RefreshServiceStatusSummary();
        }

        // Summarises the service requests and names the most urgent one waiting
        // for a crew (the top of ServiceRequestManager's min-heap).
        private void RefreshServiceStatusSummary()
        {
            ServiceRequestManager requests = ServiceRequestManager.Shared;

            string summary = string.Format(
                "Track a report with its reference number and follow its progress. " +
                "{0} requests are on record and {1} are still open.",
                requests.Count, requests.CountOpen());

            List<ServiceRequest> next = requests.GetUrgentQueue(1);
            if (next.Count > 0 && next[0].Priority == RequestPriority.Critical)
            {
                summary += Environment.NewLine + Environment.NewLine +
                           "Most urgent: " + next[0].Title + " (" + next[0].Suburb + ")";
            }

            lblServiceStatusInfo.Text = summary;
        }

        // Describes the Local Events service and, if there is one, names the most
        // urgent notice (the front of EventManager's announcement priority queue).
        private void RefreshEventsSummary()
        {
            EventManager events = EventManager.Shared;

            string summary = string.Format(
                "Browse {0} upcoming community events and the latest service notices. " +
                "Search by keyword, category or date, and get suggestions based on what you look for.",
                events.EventCount);

            Announcement? urgent = events.GetMostUrgentAnnouncement(DateTime.Today);
            if (urgent != null && urgent.Priority == AnnouncementPriority.Critical)
            {
                summary += Environment.NewLine + Environment.NewLine +
                           "Urgent: " + urgent.Title + " (" + urgent.Area + ")";
            }

            lblLocalEventsInfo.Text = summary;
        }

        private void RefreshIssueCount()
        {
            int count = IssueManager.Count;

            if (count == 0)
            {
                lblIssueCount.Text = "No issues reported yet. Your reports will be listed here.";
            }
            else if (count == 1)
            {
                lblIssueCount.Text = "1 issue reported in this session. Thank you for contributing.";
            }
            else
            {
                lblIssueCount.Text = count + " issues reported in this session. Thank you for contributing.";
            }
        }
    }
}