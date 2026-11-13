using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MunicipalServicesApp.Models;
using MunicipalServicesApp.Services;
using MunicipalServicesApp.UI;

namespace MunicipalServicesApp
{
    //resident to report municipal issue

    public partial class ReportIssuesForm : Form
    {
        private string _attachmentPath = string.Empty;

        private const long MaxAttachmentBytes = 10 * 1024 * 1024;

        public ReportIssuesForm()
        {
            InitializeComponent();
            ApplyTheme();
            PopulateCategories();
            WireUpEvents();
            UpdateEngagementProgress();
        }

        private void ApplyTheme()
        {
            BackColor = Theme.Surface;

            headerPanel.BackColor = Theme.Primary;
            lblFormTitle.Font = Theme.TitleFont;
            lblFormTitle.ForeColor = Theme.TextOnPrimary;
            lblFormSubtitle.Font = Theme.SubtitleFont;
            lblFormSubtitle.ForeColor = Theme.TextOnPrimary;

            progressLayout.BackColor = Color.White;
            lblEngagement.Font = Theme.SectionFont;
            lblEngagement.ForeColor = Theme.Accent;

            lblLocation.Font = Theme.BodyFont;
            lblCategory.Font = Theme.BodyFont;
            lblDescription.Font = Theme.BodyFont;
            lblAttachment.Font = Theme.BodyFont;
            lblLocation.ForeColor = Theme.TextPrimary;
            lblCategory.ForeColor = Theme.TextPrimary;
            lblDescription.ForeColor = Theme.TextPrimary;
            lblAttachment.ForeColor = Theme.TextPrimary;

            txtLocation.Font = Theme.BodyFont;
            cboCategory.Font = Theme.BodyFont;
            rtbDescription.Font = Theme.BodyFont;
            rtbDescription.BorderStyle = BorderStyle.FixedSingle;

            lblAttachmentName.Font = Theme.SmallFont;
            lblAttachmentName.ForeColor = Theme.TextMuted;

            Theme.StyleSecondaryButton(btnAttach);
            Theme.StyleSecondaryButton(btnBack);
            Theme.StyleSecondaryButton(btnClear);
            Theme.StyleActionButton(btnSubmit);
            btnSubmit.BackColor = Theme.Accent;
            btnSubmit.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
        }

        // Categories the resident can choose
        private void PopulateCategories()
        {
            cboCategory.Items.AddRange(new object[]
            {
                "Sanitation",
                "Roads and Potholes",
                "Water and Sewerage",
                "Electricity",
                "Refuse Removal",
                "Street Lighting",
                "Parks and Public Spaces",
                "Other"
            });
        }

        private void WireUpEvents()
        {
            txtLocation.TextChanged += ReportInput_Changed;
            cboCategory.SelectedIndexChanged += ReportInput_Changed;
            rtbDescription.TextChanged += ReportInput_Changed;

            btnAttach.Click += BtnAttach_Click;
            btnSubmit.Click += BtnSubmit_Click;
            btnClear.Click += BtnClear_Click;
            btnBack.Click += BtnBack_Click;
        }

        private void ReportInput_Changed(object sender, EventArgs e)
        {
            UpdateEngagementProgress();
        }

        private void UpdateEngagementProgress()
        {
            bool hasLocation = !string.IsNullOrWhiteSpace(txtLocation.Text);
            bool hasCategory = cboCategory.SelectedIndex >= 0;
            bool hasDescription = !string.IsNullOrWhiteSpace(rtbDescription.Text);
            bool hasAttachment = !string.IsNullOrEmpty(_attachmentPath);

            int completed = EngagementTracker.CountCompletedSteps(
                hasLocation, hasCategory, hasDescription, hasAttachment);

            prgReport.Value = EngagementTracker.CalculatePercentage(completed);
            lblEngagement.Text = EngagementTracker.GetProgressMessage(
                hasLocation, hasCategory, hasDescription, hasAttachment);
        }

        private void BtnAttach_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Select a photo or document to support your report";
                dialog.Filter =
                    "Images (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|" +
                    "Documents (*.pdf;*.docx;*.doc;*.txt)|*.pdf;*.docx;*.doc;*.txt|" +
                    "All supported files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.pdf;*.docx;*.doc;*.txt";
                dialog.FilterIndex = 3;
                dialog.Multiselect = false;

                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    FileInfo file = new FileInfo(dialog.FileName);

                    if (file.Length > MaxAttachmentBytes)
                    {
                        MessageBox.Show(
                            "That file is larger than 10 MB. Please choose a smaller file.",
                            "File too large",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    _attachmentPath = file.FullName;
                    lblAttachmentName.Text = "Attached: " + file.Name +
                                             " (" + FormatFileSize(file.Length) + ")";
                    lblAttachmentName.ForeColor = Theme.Accent;

                    UpdateEngagementProgress();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "The selected file could not be read: " + ex.Message,
                        "Attachment error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " bytes";
            if (bytes < 1024 * 1024) return (bytes / 1024) + " KB";

            return Math.Round(bytes / 1024d / 1024d, 1) + " MB";
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (!ValidateReport())
            {
                return;
            }

            Issue issue = new Issue(
                txtLocation.Text.Trim(),
                cboCategory.SelectedItem.ToString(),
                rtbDescription.Text.Trim(),
                _attachmentPath);

            string reference = IssueManager.AddIssue(issue);
            string acknowledgement =
                EngagementTracker.GetSubmissionAcknowledgement(IssueManager.Count);

            MessageBox.Show(
                "Your report has been submitted." + Environment.NewLine + Environment.NewLine +
                "Reference number: " + reference + Environment.NewLine +
                "Category: " + issue.Category + Environment.NewLine +
                "Location: " + issue.Location + Environment.NewLine + Environment.NewLine +
                acknowledgement + Environment.NewLine +
                "Keep this reference number to track your report on the Service Request Status page.",
                "Report submitted successfully",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ResetForm();
        }

        private bool ValidateReport()
        {
            if (string.IsNullOrWhiteSpace(txtLocation.Text))
            {
                ShowValidationMessage(
                    "Please enter the location of the issue so the team knows where to go.",
                    txtLocation);
                return false;
            }

            if (txtLocation.Text.Trim().Length < 3)
            {
                ShowValidationMessage(
                    "Please give a little more detail about the location, such as a street name.",
                    txtLocation);
                return false;
            }

            if (cboCategory.SelectedIndex < 0)
            {
                ShowValidationMessage(
                    "Please choose the category that best describes the issue.",
                    cboCategory);
                return false;
            }

            if (string.IsNullOrWhiteSpace(rtbDescription.Text))
            {
                ShowValidationMessage(
                    "Please describe the issue so the responsible department knows what to expect.",
                    rtbDescription);
                return false;
            }

            if (rtbDescription.Text.Trim().Length < 10)
            {
                ShowValidationMessage(
                    "Please add a little more detail to the description. A fuller report is easier to act on.",
                    rtbDescription);
                return false;
            }

            return true;
        }

        private static void ShowValidationMessage(string message, Control controlToFocus)
        {
            MessageBox.Show(
                message,
                "Incomplete report",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            controlToFocus.Focus();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            bool hasContent = !string.IsNullOrWhiteSpace(txtLocation.Text)
                              || cboCategory.SelectedIndex >= 0
                              || !string.IsNullOrWhiteSpace(rtbDescription.Text)
                              || !string.IsNullOrEmpty(_attachmentPath);

            if (!hasContent)
            {
                return;
            }

            DialogResult confirm = MessageBox.Show(
                "Clear everything you have entered on this form?",
                "Confirm clear",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                ResetForm();
            }
        }

        private void ResetForm()
        {
            txtLocation.Clear();
            cboCategory.SelectedIndex = -1;
            rtbDescription.Clear();

            _attachmentPath = string.Empty;
            lblAttachmentName.Text = "No file attached (optional).";
            lblAttachmentName.ForeColor = Theme.TextMuted;

            UpdateEngagementProgress();
            txtLocation.Focus();
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}