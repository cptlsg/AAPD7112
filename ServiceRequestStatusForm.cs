using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MunicipalServicesApp.Controls;
using MunicipalServicesApp.DataStructures;
using MunicipalServicesApp.Models;
using MunicipalServicesApp.Services;
using MunicipalServicesApp.UI;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Lets residents track their service requests by reference number and see a
    /// well-organised list of all requests with their status. Each part of the page
    /// is driven by one of the data structures in ServiceRequestManager:
    ///
    ///   Track box and list order   AVL tree, red-black tree, binary search tree
    ///   Department browser         general tree (pre-order and post-order traversals)
    ///   Urgent queue tab           min-heap
    ///   Related tab                directed graph (breadth-first and depth-first search)
    ///   Crew route tab             weighted graph (Dijkstra, Prim's MST, depth-first walk)
    ///   Under the hood tab         sizes, heights and self-checks for all of the above
    /// </summary>
    public partial class ServiceRequestStatusForm : Form
    {
        private const int UrgentListSize = 10;
        private const string AllDepartmentsOption = "All departments";

        private static readonly string[] SortOptions =
        {
            "Newest first (red-black tree)",
            "Oldest first (red-black tree)",
            "Reference number (AVL tree)"
        };

        private static readonly string[] StatusOptions =
        {
            "All statuses",
            "Open",
            "Waiting for a crew",
            "Blocked",
            "Resolved or closed"
        };

        private readonly ServiceRequestManager _manager;
        private readonly ToolTip _toolTip = new ToolTip();
        private readonly List<Label> _wrappingLabels = new List<Label>();

        private ServiceRequest? _selected;
        private GeneralTreeNode<CategoryTreeItem>? _branch;
        private bool _updatingTree;
        private bool _updatingList;
        private bool _updatingFilters;

        // ---- Header and filters ----
        private readonly Panel headerPanel = new Panel();
        private readonly Label lblFormTitle = new Label();
        private readonly Label lblFormSubtitle = new Label();
        private readonly Label lblTrack = new Label();
        private readonly TextBox txtTrack = new TextBox();
        private readonly Button btnTrack = new Button();
        private readonly Label lblTrackResult = new Label();
        private readonly Label lblSort = new Label();
        private readonly ComboBox cboSort = new ComboBox();
        private readonly Label lblStatusFilter = new Label();
        private readonly ComboBox cboStatusFilter = new ComboBox();
        private readonly Label lblSuburb = new Label();
        private readonly TextBox txtSuburb = new TextBox();
        private readonly Label lblFrom = new Label();
        private readonly DateTimePicker dtpFrom = new DateTimePicker();
        private readonly Label lblTo = new Label();
        private readonly DateTimePicker dtpTo = new DateTimePicker();
        private readonly Button btnClearFilters = new Button();

        // ---- Body ----
        private readonly Label lblTreeHeading = new Label();
        private readonly TreeView treeDepartments = new TreeView();
        private readonly Label lblListHeading = new Label();
        private readonly Label lblListCount = new Label();
        private readonly ListView lvRequests = new ListView();
        private readonly TabControl tabs = new TabControl();
        private readonly TabPage tabDetails = new TabPage("Details");
        private readonly TabPage tabUrgent = new TabPage("Urgent queue");
        private readonly TabPage tabRelated = new TabPage("Related");
        private readonly TabPage tabRoute = new TabPage("Crew route");
        private readonly TabPage tabInsights = new TabPage("Under the hood");

        // ---- Details tab ----
        private readonly Label lblDetailReference = new Label();
        private readonly Label lblDetailTitle = new Label();
        private readonly Label lblDetailMeta = new Label();
        private readonly Label lblDetailStatus = new Label();
        private readonly Label lblDetailBlocked = new Label();
        private readonly StatusTimeline timeline = new StatusTimeline();
        private readonly Label lblHistoryHeading = new Label();
        private readonly ListView lvHistory = new ListView();
        private readonly Button btnAdvance = new Button();
        private readonly Button btnShowRelated = new Button();

        // ---- Urgent queue tab ----
        private readonly Label lblUrgentIntro = new Label();
        private readonly ListView lvUrgent = new ListView();
        private readonly Button btnDispatch = new Button();
        private readonly Label lblDispatchResult = new Label();

        // ---- Related tab ----
        private readonly Label lblRelatedIntro = new Label();
        private readonly ListView lvRelated = new ListView();
        private readonly ListViewGroup grpWaitingOn = new ListViewGroup("Waiting on (breadth-first search)");
        private readonly ListViewGroup grpUnblocks = new ListViewGroup("Will be unblocked by this request (breadth-first search)");
        private readonly ListViewGroup grpOrder = new ListViewGroup("Order to resolve (depth-first search)");

        // ---- Crew route tab ----
        private readonly Label lblRouteDepartment = new Label();
        private readonly ComboBox cboRouteDepartment = new ComboBox();
        private readonly RouteMapPanel routeMap = new RouteMapPanel();
        private readonly TextBox txtRouteSummary = new TextBox();
        private readonly Button btnLargerMap = new Button();
        private CrewRoutePlan? _currentPlan;

        // ---- Under the hood tab ----
        private readonly Label lblInsightsIntro = new Label();
        private readonly Label lblLookupStats = new Label();
        private readonly ListView lvStructures = new ListView();
        private readonly ListView lvChecks = new ListView();
        private readonly Button btnRunChecks = new Button();

        // ---- Footer ----
        private readonly Label lblStatus = new Label();
        private readonly Button btnBackToMenu = new Button();

        public ServiceRequestStatusForm() : this(ServiceRequestManager.Shared)
        {
        }

        public ServiceRequestStatusForm(ServiceRequestManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            InitializeComponent();
            BuildLayout();
            ConfigureControls();
            WireUpEvents();

            routeMap.SetNetwork(_manager.Suburbs, _manager.Roads, _manager.Depot);
            RefreshAll();
        }

        /// <summary>
        /// Opens the page with one request already selected, e.g. straight after
        /// it has been reported.
        /// </summary>
        public void SelectReference(string reference)
        {
            ServiceRequest? request = _manager.Find(reference);
            if (request != null)
            {
                txtTrack.Text = reference;
                SelectRequest(request);
            }
        }

        // ------------------------------------------------------------------
        // Layout
        // ------------------------------------------------------------------

        /// <summary>
        /// Converts a size designed at 100% display scaling to the current scaling.
        /// </summary>
        private int S(int pixels)
        {
            return (int)Math.Round(pixels * DeviceDpi / 96f);
        }

        private void BuildLayout()
        {
            SuspendLayout();
            BackColor = Theme.Surface;

            Rectangle screen = Screen.FromPoint(Cursor.Position).WorkingArea;
            if (screen.Width < Width || screen.Height < Height)
            {
                WindowState = FormWindowState.Maximized;
            }

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, S(72)));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, S(54)));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildFilterBar(), 0, 1);
            root.Controls.Add(BuildBody(), 0, 2);
            root.Controls.Add(BuildFooter(), 0, 3);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Margin = Padding.Empty;
            headerPanel.BackColor = Theme.Primary;

            lblFormTitle.AutoSize = true;
            lblFormTitle.Text = "Service Request Status";
            lblFormTitle.Font = Theme.TitleFont;
            lblFormTitle.ForeColor = Theme.TextOnPrimary;
            lblFormTitle.Location = new Point(S(20), S(8));

            lblFormSubtitle.AutoSize = true;
            lblFormSubtitle.Text = "Track your reports with their reference number and follow every request from submission to completion.";
            lblFormSubtitle.Font = Theme.SubtitleFont;
            lblFormSubtitle.ForeColor = Theme.TextOnPrimary;
            lblFormSubtitle.Location = new Point(S(22), S(42));

            headerPanel.Controls.Add(lblFormTitle);
            headerPanel.Controls.Add(lblFormSubtitle);
            return headerPanel;
        }

        private Control BuildFilterBar()
        {
            TableLayoutPanel bar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = new Padding(S(14), S(8), S(14), S(6))
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            FlowLayoutPanel trackRow = NewFlowRow();
            StyleFieldLabel(lblTrack, "Track a request");
            lblTrack.Font = Theme.SectionFont;
            lblTrack.ForeColor = Theme.TextPrimary;
            lblTrack.Margin = new Padding(0, S(6), S(8), 0);

            txtTrack.Width = S(240);
            txtTrack.Font = Theme.BodyFont;
            txtTrack.PlaceholderText = "Reference number, e.g. MSA-2026-0011";
            txtTrack.Margin = new Padding(0, S(5), S(6), S(3));

            StyleButton(btnTrack, "Track", S(90), true);

            lblTrackResult.AutoSize = true;
            lblTrackResult.Font = Theme.BodyFont;
            lblTrackResult.ForeColor = Theme.TextMuted;
            lblTrackResult.UseMnemonic = false;
            lblTrackResult.Margin = new Padding(S(8), S(8), 0, 0);
            lblTrackResult.Text = "Enter the reference number from your confirmation message.";

            trackRow.Controls.AddRange(new Control[] { lblTrack, txtTrack, btnTrack, lblTrackResult });

            FlowLayoutPanel filterRow = NewFlowRow();
            StyleFieldLabel(lblSort, "Sort");
            StyleCombo(cboSort, S(215));
            StyleFieldLabel(lblStatusFilter, "Status");
            StyleCombo(cboStatusFilter, S(150));
            StyleFieldLabel(lblSuburb, "Suburb starts with");
            txtSuburb.Width = S(120);
            txtSuburb.Font = Theme.BodyFont;
            txtSuburb.Margin = new Padding(0, S(5), S(14), S(3));
            StyleFieldLabel(lblFrom, "Submitted from");
            StyleDatePicker(dtpFrom);
            StyleFieldLabel(lblTo, "to");
            StyleDatePicker(dtpTo);
            StyleButton(btnClearFilters, "Clear filters", S(110), false);

            filterRow.Controls.AddRange(new Control[]
            {
                lblSort, cboSort, lblStatusFilter, cboStatusFilter, lblSuburb, txtSuburb,
                lblFrom, dtpFrom, lblTo, dtpTo, btnClearFilters
            });

            bar.Controls.Add(trackRow, 0, 0);
            bar.Controls.Add(filterRow, 0, 1);
            return bar;
        }

        private Control BuildBody()
        {
            TableLayoutPanel body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(S(10), S(8), S(10), S(4))
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(250)));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(490)));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Left: department tree.
            TableLayoutPanel treeColumn = NewColumn(2);
            StyleHeading(lblTreeHeading, "Browse by department");
            treeDepartments.Dock = DockStyle.Fill;
            treeDepartments.Font = Theme.BodyFont;
            treeDepartments.HideSelection = false;
            treeDepartments.FullRowSelect = true;
            treeDepartments.ShowLines = true;
            treeDepartments.BorderStyle = BorderStyle.FixedSingle;
            treeColumn.Controls.Add(lblTreeHeading, 0, 0);
            treeColumn.Controls.Add(treeDepartments, 0, 1);

            // Middle: the request list.
            TableLayoutPanel listColumn = NewColumn(2);
            FlowLayoutPanel listHeading = NewFlowRow();
            listHeading.Margin = Padding.Empty;
            StyleHeading(lblListHeading, "Service requests");
            lblListCount.AutoSize = true;
            lblListCount.Font = Theme.SmallFont;
            lblListCount.ForeColor = Theme.TextMuted;
            lblListCount.UseMnemonic = false;
            lblListCount.Margin = new Padding(S(6), S(8), 0, 0);
            listHeading.Controls.Add(lblListHeading);
            listHeading.Controls.Add(lblListCount);

            StyleListView(lvRequests);
            lvRequests.Columns.Add("Reference", S(110));
            lvRequests.Columns.Add("Title", S(220));
            lvRequests.Columns.Add("Suburb", S(105));
            lvRequests.Columns.Add("Priority", S(70));
            lvRequests.Columns.Add("Status", S(140));
            lvRequests.Columns.Add("Submitted", S(95));
            listColumn.Controls.Add(listHeading, 0, 0);
            listColumn.Controls.Add(lvRequests, 0, 1);

            // Right: tabs.
            tabs.Dock = DockStyle.Fill;
            tabs.Font = Theme.BodyFont;
            tabs.Margin = new Padding(S(8), S(4), 0, 0);
            tabs.Padding = new Point(S(8), S(4));
            tabs.TabPages.AddRange(new[] { tabDetails, tabUrgent, tabRelated, tabRoute, tabInsights });

            foreach (TabPage page in tabs.TabPages)
            {
                page.BackColor = Color.White;
                page.Padding = new Padding(S(8));
            }

            tabDetails.Controls.Add(BuildDetailsTab());
            tabUrgent.Controls.Add(BuildUrgentTab());
            tabRelated.Controls.Add(BuildRelatedTab());
            tabRoute.Controls.Add(BuildRouteTab());
            tabInsights.Controls.Add(BuildInsightsTab());

            body.Controls.Add(treeColumn, 0, 0);
            body.Controls.Add(listColumn, 1, 0);
            body.Controls.Add(tabs, 2, 0);
            return body;
        }

        private Control BuildDetailsTab()
        {
            TableLayoutPanel layout = NewTabLayout(10);
            layout.RowStyles[6] = new RowStyle(SizeType.Absolute, S(74));
            layout.RowStyles[8] = new RowStyle(SizeType.Percent, 100);

            StyleWrappingLabel(lblDetailReference, Theme.SectionFont, Theme.Primary);
            StyleWrappingLabel(lblDetailTitle, Theme.CardTitleFont, Theme.TextPrimary);
            StyleWrappingLabel(lblDetailMeta, Theme.BodyFont, Theme.TextPrimary);
            StyleWrappingLabel(lblDetailStatus, Theme.SmallBoldFont, Theme.TextPrimary);
            StyleWrappingLabel(lblDetailBlocked, Theme.SmallBoldFont, Theme.Danger);

            timeline.Dock = DockStyle.Fill;
            timeline.Margin = new Padding(0, S(6), 0, S(2));

            StyleHeading(lblHistoryHeading, "Progress history");

            StyleListView(lvHistory);
            lvHistory.Columns.Add("Date", S(120));
            lvHistory.Columns.Add("Stage", S(95));
            lvHistory.Columns.Add("Note", S(220));

            FlowLayoutPanel buttons = NewFlowRow();
            StyleButton(btnAdvance, "Advance to next stage", S(185), false);
            StyleButton(btnShowRelated, "Show related requests", S(185), false);
            buttons.Controls.Add(btnAdvance);
            buttons.Controls.Add(btnShowRelated);

            layout.Controls.Add(lblDetailReference, 0, 0);
            layout.Controls.Add(lblDetailTitle, 0, 1);
            layout.Controls.Add(lblDetailMeta, 0, 2);
            layout.Controls.Add(lblDetailStatus, 0, 3);
            layout.Controls.Add(lblDetailBlocked, 0, 4);
            layout.Controls.Add(new Label { Height = S(2), Margin = Padding.Empty }, 0, 5);
            layout.Controls.Add(timeline, 0, 6);
            layout.Controls.Add(lblHistoryHeading, 0, 7);
            layout.Controls.Add(lvHistory, 0, 8);
            layout.Controls.Add(buttons, 0, 9);
            return layout;
        }

        private Control BuildUrgentTab()
        {
            TableLayoutPanel layout = NewTabLayout(4);
            layout.RowStyles[1] = new RowStyle(SizeType.Percent, 100);

            StyleWrappingLabel(lblUrgentIntro, Theme.BodyFont, Theme.TextMuted);
            lblUrgentIntro.Text =
                "Requests waiting for a crew, most urgent first: Critical before High, then the request that " +
                "has waited longest. A min-heap keeps the most urgent request at the top, so the next job is " +
                "found instantly and taken off in O(log n). Double-click a request to open it.";

            StyleListView(lvUrgent);
            lvUrgent.Columns.Add("#", S(32));
            lvUrgent.Columns.Add("Reference", S(110));
            lvUrgent.Columns.Add("Title", S(170));
            lvUrgent.Columns.Add("Priority", S(70));
            lvUrgent.Columns.Add("Waiting", S(70));

            FlowLayoutPanel buttons = NewFlowRow();
            StyleButton(btnDispatch, "Dispatch next job", S(160), true);
            btnDispatch.BackColor = Theme.Accent;
            btnDispatch.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            buttons.Controls.Add(btnDispatch);

            StyleWrappingLabel(lblDispatchResult, Theme.BodyFont, Theme.TextPrimary);
            lblDispatchResult.Text =
                "Dispatching takes the top request off the heap and assigns it to its department's team. " +
                "Blocked requests are skipped and keep their place in line.";

            layout.Controls.Add(lblUrgentIntro, 0, 0);
            layout.Controls.Add(lvUrgent, 0, 1);
            layout.Controls.Add(buttons, 0, 2);
            layout.Controls.Add(lblDispatchResult, 0, 3);
            return layout;
        }

        private Control BuildRelatedTab()
        {
            TableLayoutPanel layout = NewTabLayout(2);
            layout.RowStyles[1] = new RowStyle(SizeType.Percent, 100);

            StyleWrappingLabel(lblRelatedIntro, Theme.BodyFont, Theme.TextMuted);

            StyleListView(lvRelated);
            lvRelated.ShowGroups = true;
            lvRelated.Columns.Add("Reference", S(110));
            lvRelated.Columns.Add("Title", S(170));
            lvRelated.Columns.Add("Status", S(100));
            lvRelated.Columns.Add("Link", S(80));
            lvRelated.Groups.AddRange(new[] { grpWaitingOn, grpUnblocks, grpOrder });

            layout.Controls.Add(lblRelatedIntro, 0, 0);
            layout.Controls.Add(lvRelated, 0, 1);
            return layout;
        }

        private Control BuildRouteTab()
        {
            TableLayoutPanel layout = NewTabLayout(3);
            layout.RowStyles[1] = new RowStyle(SizeType.Percent, 70);
            layout.RowStyles[2] = new RowStyle(SizeType.Percent, 30);

            FlowLayoutPanel picker = NewFlowRow();
            StyleFieldLabel(lblRouteDepartment, "Plan a route for");
            StyleCombo(cboRouteDepartment, S(200));
            cboRouteDepartment.Margin = new Padding(0, S(5), S(8), S(3));
            StyleButton(btnLargerMap, "Open larger map", S(135), false);
            picker.Controls.Add(lblRouteDepartment);
            picker.Controls.Add(cboRouteDepartment);
            picker.Controls.Add(btnLargerMap);

            routeMap.Dock = DockStyle.Fill;
            routeMap.Margin = new Padding(0, S(4), 0, S(4));

            txtRouteSummary.Dock = DockStyle.Fill;
            txtRouteSummary.Multiline = true;
            txtRouteSummary.ReadOnly = true;
            txtRouteSummary.ScrollBars = ScrollBars.Vertical;
            txtRouteSummary.BorderStyle = BorderStyle.None;
            txtRouteSummary.BackColor = Color.White;
            txtRouteSummary.Font = Theme.SmallFont;
            txtRouteSummary.ForeColor = Theme.TextPrimary;

            layout.Controls.Add(picker, 0, 0);
            layout.Controls.Add(routeMap, 0, 1);
            layout.Controls.Add(txtRouteSummary, 0, 2);
            return layout;
        }

        private Control BuildInsightsTab()
        {
            TableLayoutPanel layout = NewTabLayout(7);
            layout.RowStyles[3] = new RowStyle(SizeType.Percent, 55);
            layout.RowStyles[5] = new RowStyle(SizeType.Percent, 45);

            StyleWrappingLabel(lblInsightsIntro, Theme.BodyFont, Theme.TextMuted);
            lblInsightsIntro.Text =
                "The data structures behind this page, how big they are and what shape they are in. " +
                "Hover over a row to read it in full.";

            StyleWrappingLabel(lblLookupStats, Theme.SmallBoldFont, Theme.Primary);
            lblLookupStats.Text = "Track a request to compare the AVL tree with an unbalanced binary search tree.";

            Label structuresHeading = new Label();
            StyleHeading(structuresHeading, "Data structures");
            StyleListView(lvStructures);
            lvStructures.ShowItemToolTips = true;
            lvStructures.Columns.Add("Structure", S(130));
            lvStructures.Columns.Add("Used for", S(160));
            lvStructures.Columns.Add("Size", S(85));
            lvStructures.Columns.Add("Shape", S(240));

            Label checksHeading = new Label();
            StyleHeading(checksHeading, "Self-checks");
            StyleListView(lvChecks);
            lvChecks.ShowItemToolTips = true;
            lvChecks.Columns.Add("Check", S(210));
            lvChecks.Columns.Add("Result", S(55));
            lvChecks.Columns.Add("Detail", S(300));

            FlowLayoutPanel buttons = NewFlowRow();
            StyleButton(btnRunChecks, "Run checks again", S(150), false);
            buttons.Controls.Add(btnRunChecks);

            layout.Controls.Add(lblInsightsIntro, 0, 0);
            layout.Controls.Add(lblLookupStats, 0, 1);
            layout.Controls.Add(structuresHeading, 0, 2);
            layout.Controls.Add(lvStructures, 0, 3);
            layout.Controls.Add(checksHeading, 0, 4);
            layout.Controls.Add(lvChecks, 0, 5);
            layout.Controls.Add(buttons, 0, 6);
            return layout;
        }

        private Control BuildFooter()
        {
            TableLayoutPanel footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = new Padding(S(14), S(8), S(14), S(8))
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(180)));
            footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Font = Theme.SmallFont;
            lblStatus.ForeColor = Theme.TextMuted;
            lblStatus.UseMnemonic = false;

            btnBackToMenu.Dock = DockStyle.Fill;
            btnBackToMenu.Text = "Back to Main Menu";
            Theme.StyleSecondaryButton(btnBackToMenu);

            footer.Controls.Add(lblStatus, 0, 0);
            footer.Controls.Add(btnBackToMenu, 1, 0);
            return footer;
        }

        // ---- Small layout helpers ----

        private FlowLayoutPanel NewFlowRow()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, S(4))
            };
        }

        private TableLayoutPanel NewColumn(int rows)
        {
            TableLayoutPanel column = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = rows,
                Margin = new Padding(0, 0, S(6), 0),
                Padding = Padding.Empty
            };
            column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < rows; i++)
            {
                column.RowStyles.Add(new RowStyle(i == rows - 1 ? SizeType.Percent : SizeType.AutoSize, 100));
            }
            return column;
        }

        /// <summary>
        /// A one-column layout with rows that fit their content. Callers change
        /// the rows that should stretch. Wrapping labels inside it are re-measured
        /// whenever it is resized.
        /// </summary>
        private TableLayoutPanel NewTabLayout(int rows)
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = rows,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < rows; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            layout.SizeChanged += (sender, e) => FitWrappingLabels(layout);
            return layout;
        }

        private void FitWrappingLabels(TableLayoutPanel layout)
        {
            int width = Math.Max(S(100), layout.ClientSize.Width - S(6));
            foreach (Control control in layout.Controls)
            {
                if (control is Label label && _wrappingLabels.Contains(label))
                {
                    label.MaximumSize = new Size(width, 0);
                }
            }
        }

        private void StyleWrappingLabel(Label label, Font font, Color colour)
        {
            label.AutoSize = true;
            label.MaximumSize = new Size(S(440), 0);
            label.Font = font;
            label.ForeColor = colour;
            label.UseMnemonic = false;
            label.Margin = new Padding(0, S(2), 0, S(4));
            _wrappingLabels.Add(label);
        }

        private void StyleHeading(Label label, string text)
        {
            label.AutoSize = true;
            label.Text = text;
            label.Font = Theme.SectionFont;
            label.ForeColor = Theme.TextPrimary;
            label.UseMnemonic = false;
            label.Margin = new Padding(0, S(4), 0, S(6));
        }

        private void StyleFieldLabel(Label label, string text)
        {
            label.AutoSize = true;
            label.Text = text;
            label.Font = Theme.SmallFont;
            label.ForeColor = Theme.TextMuted;
            label.Margin = new Padding(0, S(9), S(5), 0);
        }

        private void StyleCombo(ComboBox combo, int width)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Width = width;
            combo.Font = Theme.BodyFont;
            combo.Margin = new Padding(0, S(5), S(14), S(3));
        }

        private void StyleDatePicker(DateTimePicker picker)
        {
            picker.Format = DateTimePickerFormat.Short;
            picker.ShowCheckBox = true;
            picker.Width = S(125);
            picker.Font = Theme.BodyFont;
            picker.Margin = new Padding(0, S(5), S(8), S(3));
        }

        private void StyleButton(Button button, string text, int width, bool primary)
        {
            button.Text = text;
            button.Size = new Size(width, S(32));
            button.Margin = new Padding(0, S(2), S(8), S(2));

            if (primary)
            {
                Theme.StyleActionButton(button);
            }
            else
            {
                Theme.StyleSecondaryButton(button);
            }
        }

        private void StyleListView(ListView list)
        {
            list.Dock = DockStyle.Fill;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.MultiSelect = false;
            list.HideSelection = false;
            list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            list.Font = Theme.BodyFont;
            list.BorderStyle = BorderStyle.FixedSingle;
            list.Margin = new Padding(0, 0, 0, S(4));

            // The last column stretches to fill the width.
            list.ClientSizeChanged += (sender, e) => StretchLastColumn(list);
        }

        private static void StretchLastColumn(ListView list)
        {
            if (list.Columns.Count == 0) return;

            int used = 0;
            for (int i = 0; i < list.Columns.Count - 1; i++)
            {
                used += list.Columns[i].Width;
            }

            int remaining = list.ClientSize.Width - used - 4;
            ColumnHeader last = list.Columns[list.Columns.Count - 1];
            last.Width = Math.Max(remaining, 60);
        }

        // ------------------------------------------------------------------
        // Setup
        // ------------------------------------------------------------------

        private void ConfigureControls()
        {
            _updatingFilters = true;

            cboSort.Items.AddRange(SortOptions);
            cboSort.SelectedIndex = 0;

            cboStatusFilter.Items.AddRange(StatusOptions);
            cboStatusFilter.SelectedIndex = 0;

            // Value is set before Checked, because setting Value ticks the box.
            DateTime today = DateTime.Today;
            dtpFrom.Value = today.AddMonths(-1);
            dtpFrom.Checked = false;
            dtpTo.Value = today;
            dtpTo.Checked = false;

            cboRouteDepartment.Items.Add(AllDepartmentsOption);
            foreach (string department in ServiceDepartments.Departments)
            {
                cboRouteDepartment.Items.Add(department);
            }
            cboRouteDepartment.SelectedIndex = 0;

            _updatingFilters = false;

            _toolTip.SetToolTip(txtTrack, "Type a full reference such as MSA-2026-0011, or just the number (11) for this year.");
            _toolTip.SetToolTip(cboSort, "Requests are listed straight from a tree traversal, so no sorting step is needed.");
            _toolTip.SetToolTip(txtSuburb, "Finds suburbs with a range query on a binary search tree, e.g. \"S\" finds Silverton and Sunnyside.");
            _toolTip.SetToolTip(dtpFrom, "Tick the box to only show requests submitted on or after this date.");
            _toolTip.SetToolTip(dtpTo, "Tick the box to only show requests submitted on or before this date.");
            _toolTip.SetToolTip(btnAdvance, "Staff demonstration: moves the selected request to its next stage.");
            _toolTip.SetToolTip(btnDispatch, "Staff demonstration: assigns the most urgent request that can start to a team.");
            _toolTip.SetToolTip(treeDepartments, "Choose a department or category to list only its requests.");
        }

        private void WireUpEvents()
        {
            btnTrack.Click += (sender, e) => TrackRequest();
            txtTrack.KeyDown += TxtTrack_KeyDown;

            cboSort.SelectedIndexChanged += Filter_Changed;
            cboStatusFilter.SelectedIndexChanged += Filter_Changed;
            txtSuburb.TextChanged += Filter_Changed;
            dtpFrom.ValueChanged += Filter_Changed;
            dtpTo.ValueChanged += Filter_Changed;
            btnClearFilters.Click += BtnClearFilters_Click;

            treeDepartments.AfterSelect += TreeDepartments_AfterSelect;
            lvRequests.SelectedIndexChanged += LvRequests_SelectedIndexChanged;

            btnAdvance.Click += BtnAdvance_Click;
            btnShowRelated.Click += (sender, e) => tabs.SelectedTab = tabRelated;
            btnDispatch.Click += BtnDispatch_Click;
            btnRunChecks.Click += (sender, e) => RefreshInsights();
            cboRouteDepartment.SelectedIndexChanged += (sender, e) => RefreshRoute();
            btnLargerMap.Click += BtnLargerMap_Click;

            lvUrgent.DoubleClick += OpenTaggedRequest;
            lvRelated.DoubleClick += OpenTaggedRequest;

            btnBackToMenu.Click += (sender, e) => Close();
            CancelButton = btnBackToMenu;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _toolTip.Dispose();
            base.OnFormClosed(e);
        }

        // ------------------------------------------------------------------
        // Event handlers
        // ------------------------------------------------------------------

        private void TxtTrack_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                TrackRequest();
            }
        }

        private void Filter_Changed(object? sender, EventArgs e)
        {
            if (_updatingFilters) return;
            ReloadList();
        }

        private void BtnClearFilters_Click(object? sender, EventArgs e)
        {
            ResetFilters();
            ReloadList();
        }

        private void TreeDepartments_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (_updatingTree || e.Node == null) return;

            if (!(e.Node.Tag is GeneralTreeNode<CategoryTreeItem> node)) return;

            if (node.Value.Request != null)
            {
                // A request leaf: list its category and open the request.
                _branch = node.Parent;
                ReloadList();
                SelectRequest(node.Value.Request);
            }
            else
            {
                _branch = node.IsRoot ? null : node;
                ReloadList();
            }
        }

        private void LvRequests_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_updatingList || lvRequests.SelectedItems.Count == 0) return;

            if (lvRequests.SelectedItems[0].Tag is ServiceRequest request)
            {
                ShowRequest(request);
            }
        }

        private void BtnAdvance_Click(object? sender, EventArgs e)
        {
            if (_selected == null) return;

            StatusChangeResult result = _manager.AdvanceStatus(_selected);

            if (!result.Succeeded)
            {
                MessageBox.Show(result.Message, "Cannot move to the next stage",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RefreshAll();
            SetStatusMessage(result.Message);
        }

        private void BtnDispatch_Click(object? sender, EventArgs e)
        {
            ServiceRequest? dispatched = _manager.DispatchNextJob();

            if (dispatched == null)
            {
                lblDispatchResult.Text = "No request that can start is waiting for a crew.";
                lblDispatchResult.ForeColor = Theme.TextMuted;
                return;
            }

            RefreshAll();

            lblDispatchResult.Text = string.Format(
                "Dispatched {0} ({1}, {2} priority) to the {3}.",
                dispatched.ReferenceNumber, dispatched.Title, dispatched.Priority, dispatched.AssignedTeam);
            lblDispatchResult.ForeColor = Theme.Accent;
            SetStatusMessage(lblDispatchResult.Text);
        }

        private void OpenTaggedRequest(object? sender, EventArgs e)
        {
            if (sender is ListView list && list.SelectedItems.Count > 0 &&
                list.SelectedItems[0].Tag is ServiceRequest request)
            {
                SelectRequest(request);
                tabs.SelectedTab = tabDetails;
            }
        }

        // ------------------------------------------------------------------
        // Tracking (AVL tree lookup)
        // ------------------------------------------------------------------

        private void TrackRequest()
        {
            string input = txtTrack.Text.Trim();

            if (input.Length == 0)
            {
                lblTrackResult.Text = "Please enter a reference number, for example MSA-2026-0011.";
                lblTrackResult.ForeColor = Theme.Danger;
                txtTrack.Focus();
                return;
            }

            TrackingResult result = _manager.Track(input);
            ShowLookupStats(result);

            if (result.Request == null)
            {
                lblTrackResult.Text = string.Format(
                    "No request found with reference {0}. Check the number on your confirmation message.",
                    result.NormalisedReference);
                lblTrackResult.ForeColor = Theme.Danger;
                txtTrack.SelectAll();
                txtTrack.Focus();
                return;
            }

            ServiceRequest request = result.Request;
            txtTrack.Text = request.ReferenceNumber;
            lblTrackResult.Text = string.Format("{0}: {1}{2}",
                request.ReferenceNumber,
                RequestStatusInfo.GetDisplayName(request.Status),
                _manager.IsBlocked(request) ? " (waiting on another request)" : string.Empty);
            lblTrackResult.ForeColor = Theme.Accent;

            SelectRequest(request);
            tabs.SelectedTab = tabDetails;
        }

        private void ShowLookupStats(TrackingResult result)
        {
            lblLookupStats.Text = string.Format(
                "Last lookup ({0}): the AVL tree needed {1} comparisons (height {2}). " +
                "The same {3} requests in an unbalanced binary search tree needed {4} comparisons (height {5}).",
                result.NormalisedReference,
                result.AvlComparisons, result.AvlHeight,
                _manager.Count,
                result.PlainBstComparisons, result.PlainBstHeight);
        }

        // ------------------------------------------------------------------
        // Refreshing
        // ------------------------------------------------------------------

        private void RefreshAll()
        {
            RebuildTree();
            ReloadList();
            RefreshUrgent();
            ShowRequest(_selected);
            RefreshRoute();
            RefreshInsights();
            SetStatusMessage(null);
        }

        private void SetStatusMessage(string? message)
        {
            string summary = string.Format(
                "{0} requests  |  {1} open  |  {2} waiting for a crew  |  {3} blocked",
                _manager.Count, _manager.CountOpen(), _manager.CountWaitingForCrew(), _manager.CountBlocked());

            lblStatus.Text = string.IsNullOrEmpty(message) ? summary : message + "    " + summary;
        }

        private void ResetFilters()
        {
            _updatingFilters = true;
            cboStatusFilter.SelectedIndex = 0;
            txtSuburb.Text = string.Empty;
            dtpFrom.Checked = false;
            dtpTo.Checked = false;
            _updatingFilters = false;

            _branch = null;
            SelectTreeNodeFor(null);
        }

        /// <summary>
        /// Rebuilds the TreeView from the department tree, keeping which branches
        /// were expanded and which one was selected.
        /// </summary>
        private void RebuildTree()
        {
            HashSet<GeneralTreeNode<CategoryTreeItem>> expanded = new HashSet<GeneralTreeNode<CategoryTreeItem>>();
            bool firstBuild = treeDepartments.Nodes.Count == 0;

            foreach (TreeNode node in AllTreeNodes(treeDepartments.Nodes))
            {
                if (node.IsExpanded && node.Tag is GeneralTreeNode<CategoryTreeItem> source)
                {
                    expanded.Add(source);
                }
            }

            _updatingTree = true;
            treeDepartments.BeginUpdate();
            treeDepartments.Nodes.Clear();

            TreeNode root = AddTreeNode(_manager.DepartmentTree.Root, treeDepartments.Nodes);

            foreach (TreeNode node in AllTreeNodes(treeDepartments.Nodes))
            {
                GeneralTreeNode<CategoryTreeItem> source = (GeneralTreeNode<CategoryTreeItem>)node.Tag;
                bool expandByDefault = firstBuild && source.Value.Level == CategoryTreeLevel.Municipality;

                if (expanded.Contains(source) || expandByDefault)
                {
                    node.Expand();
                }
            }

            treeDepartments.EndUpdate();
            SelectTreeNodeFor(_branch);
            if (treeDepartments.SelectedNode == null) treeDepartments.SelectedNode = root;
            _updatingTree = false;
        }

        /// <summary>
        /// Copies one node of the department tree, and everything below it, into the
        /// TreeView (a pre-order walk: each node is added before its children).
        /// </summary>
        private TreeNode AddTreeNode(GeneralTreeNode<CategoryTreeItem> source, TreeNodeCollection target)
        {
            CategoryTreeItem item = source.Value;
            TreeNode node;

            if (item.Request != null)
            {
                node = new TreeNode(item.Label);
                if (!item.Request.IsOpen)
                {
                    node.ForeColor = Theme.TextMuted;
                }
                else if (_manager.IsBlocked(item.Request))
                {
                    node.ForeColor = Theme.Danger;
                }
            }
            else
            {
                node = new TreeNode(string.Format("{0} ({1} open / {2})", item.Label, item.OpenCount, item.TotalCount));
                if (item.TotalCount == 0) node.ForeColor = Theme.TextMuted;
            }

            node.Tag = source;
            target.Add(node);

            foreach (GeneralTreeNode<CategoryTreeItem> child in source.Children)
            {
                AddTreeNode(child, node.Nodes);
            }

            return node;
        }

        private static IEnumerable<TreeNode> AllTreeNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                yield return node;
                foreach (TreeNode child in AllTreeNodes(node.Nodes))
                {
                    yield return child;
                }
            }
        }

        private void SelectTreeNodeFor(GeneralTreeNode<CategoryTreeItem>? branch)
        {
            bool wasUpdating = _updatingTree;
            _updatingTree = true;

            TreeNode? match = null;
            foreach (TreeNode node in AllTreeNodes(treeDepartments.Nodes))
            {
                GeneralTreeNode<CategoryTreeItem> source = (GeneralTreeNode<CategoryTreeItem>)node.Tag;
                if (branch == null ? source.IsRoot : source == branch)
                {
                    match = node;
                    break;
                }
            }

            if (match != null)
            {
                treeDepartments.SelectedNode = match;
            }

            _updatingTree = wasUpdating;
        }

        private RequestQuery? ReadQuery()
        {
            if (dtpFrom.Checked && dtpTo.Checked && dtpFrom.Value.Date > dtpTo.Value.Date)
            {
                SetStatusMessage("The \"from\" date is after the \"to\" date. Please adjust the dates.");
                return null;
            }

            return new RequestQuery
            {
                SortOrder = (RequestSortOrder)Math.Max(0, cboSort.SelectedIndex),
                StatusFilter = (RequestStatusFilter)Math.Max(0, cboStatusFilter.SelectedIndex),
                SuburbPrefix = txtSuburb.Text,
                SubmittedFrom = dtpFrom.Checked ? dtpFrom.Value.Date : (DateTime?)null,
                SubmittedTo = dtpTo.Checked ? dtpTo.Value.Date : (DateTime?)null,
                TreeNode = _branch
            };
        }

        private void ReloadList()
        {
            RequestQuery? query = ReadQuery();
            if (query == null) return;

            List<ServiceRequest> requests = _manager.GetRequests(query);

            _updatingList = true;
            lvRequests.BeginUpdate();
            lvRequests.Items.Clear();

            foreach (ServiceRequest request in requests)
            {
                bool blocked = _manager.IsBlocked(request);

                ListViewItem item = new ListViewItem(request.ReferenceNumber)
                {
                    Tag = request,
                    UseItemStyleForSubItems = false
                };

                item.SubItems.Add(request.Title);
                item.SubItems.Add(request.Suburb);
                ListViewItem.ListViewSubItem priority = item.SubItems.Add(request.Priority.ToString());
                ListViewItem.ListViewSubItem status = item.SubItems.Add(DescribeStatus(request, blocked));
                item.SubItems.Add(request.SubmittedOn.ToString("d MMM yyyy"));

                priority.ForeColor = Theme.GetRequestPriorityColour(request.Priority);
                status.ForeColor = blocked ? Theme.Danger : Theme.GetStatusColour(request.Status);

                if (!request.IsOpen)
                {
                    item.ForeColor = Theme.TextMuted;
                    item.SubItems[1].ForeColor = Theme.TextMuted;
                    item.SubItems[2].ForeColor = Theme.TextMuted;
                }

                if (request.ReportedThisSession)
                {
                    item.Text = request.ReferenceNumber + " *";
                    item.ToolTipText = "Reported by you in this session.";
                }

                lvRequests.Items.Add(item);
            }

            lvRequests.EndUpdate();

            if (_selected != null)
            {
                SelectInList(_selected);
            }

            _updatingList = false;

            string scope = _branch == null ? "all departments" : _branch.Value.Label;
            lblListCount.Text = string.Format("{0} of {1} in {2}", requests.Count, _manager.Count, scope);

            if (requests.Count == 0)
            {
                SetStatusMessage("No requests match these filters. Try clearing them.");
            }
        }

        private static string DescribeStatus(ServiceRequest request, bool blocked)
        {
            string status = RequestStatusInfo.GetDisplayName(request.Status);
            return blocked ? status + " (blocked)" : status;
        }

        /// <summary>
        /// Selects a request in the list if it is there. Returns false if the
        /// current filters hide it.
        /// </summary>
        private bool SelectInList(ServiceRequest request)
        {
            foreach (ListViewItem item in lvRequests.Items)
            {
                if (item.Tag == request)
                {
                    bool wasUpdating = _updatingList;
                    _updatingList = true;
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    _updatingList = wasUpdating;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Shows a request in the list and the tabs, clearing filters if they hide it.
        /// </summary>
        private void SelectRequest(ServiceRequest request)
        {
            _selected = request;

            if (!SelectInList(request))
            {
                ResetFilters();
                ReloadList();
                SelectInList(request);
            }

            ShowRequest(request);
        }

        // ------------------------------------------------------------------
        // Details tab
        // ------------------------------------------------------------------

        private void ShowRequest(ServiceRequest? request)
        {
            _selected = request;

            lvHistory.BeginUpdate();
            lvHistory.Items.Clear();

            if (request == null)
            {
                lblDetailReference.Text = "No request selected";
                lblDetailTitle.Text = "Track a request by its reference number, or choose one from the list.";
                lblDetailMeta.Text = string.Empty;
                lblDetailStatus.Text = string.Empty;
                lblDetailBlocked.Visible = false;
                timeline.ShowRequest(null, false);
                btnAdvance.Enabled = false;
                btnShowRelated.Enabled = false;
                lvHistory.EndUpdate();
                ShowRelated(null);
                return;
            }

            bool blocked = _manager.IsBlocked(request);
            DateTime now = DateTime.Now;

            lblDetailReference.Text = request.ReferenceNumber + (request.ReportedThisSession ? "  (reported by you)" : string.Empty);
            lblDetailTitle.Text = request.Title;

            StringBuilder meta = new StringBuilder();
            meta.AppendLine(request.Department + "  >  " + request.Category);
            meta.AppendLine("Location: " + request.Location);
            meta.AppendLine(string.Format("Priority: {0}   |   Submitted {1:d MMM yyyy, HH:mm} ({2})",
                request.Priority, request.SubmittedOn, DescribeAge(request.SubmittedOn, now)));
            if (!string.IsNullOrEmpty(request.AssignedTeam))
            {
                meta.AppendLine("Team: " + request.AssignedTeam);
            }
            if (!string.IsNullOrEmpty(request.Description))
            {
                meta.Append("Description: " + request.Description);
            }
            lblDetailMeta.Text = meta.ToString().TrimEnd();

            lblDetailStatus.Text = RequestStatusInfo.GetDisplayName(request.Status) + ": " +
                                   RequestStatusInfo.GetExplanation(request.Status);
            lblDetailStatus.ForeColor = Theme.GetStatusColour(request.Status);

            if (blocked)
            {
                List<string> waitingFor = new List<string>();
                foreach (ServiceRequest prerequisite in _manager.GetOpenPrerequisites(request))
                {
                    waitingFor.Add(prerequisite.ReferenceNumber + " (" + prerequisite.Title + ")");
                }

                lblDetailBlocked.Text = "Blocked: work cannot start until " + string.Join(" and ", waitingFor) +
                                        " is resolved.";
                lblDetailBlocked.Visible = true;
            }
            else
            {
                lblDetailBlocked.Visible = false;
            }

            timeline.ShowRequest(request, blocked);

            // Newest update first.
            for (int i = request.History.Count - 1; i >= 0; i--)
            {
                StatusUpdate update = request.History[i];
                ListViewItem item = new ListViewItem(update.Timestamp.ToString("d MMM yyyy HH:mm"));
                item.SubItems.Add(RequestStatusInfo.GetDisplayName(update.Status));
                item.SubItems.Add(update.Note);
                item.ToolTipText = update.Note;
                lvHistory.Items.Add(item);
            }
            lvHistory.ShowItemToolTips = true;
            lvHistory.EndUpdate();

            btnAdvance.Enabled = request.Status != RequestStatus.Closed;
            btnShowRelated.Enabled = true;

            ShowRelated(request);
        }

        private static string DescribeAge(DateTime when, DateTime now)
        {
            int days = (int)(now.Date - when.Date).TotalDays;
            if (days <= 0) return "today";
            if (days == 1) return "yesterday";
            return days + " days ago";
        }

        // ------------------------------------------------------------------
        // Urgent queue tab (min-heap)
        // ------------------------------------------------------------------

        private void RefreshUrgent()
        {
            DateTime now = DateTime.Now;
            List<ServiceRequest> urgent = _manager.GetUrgentQueue(UrgentListSize);

            lvUrgent.BeginUpdate();
            lvUrgent.Items.Clear();

            int position = 1;
            foreach (ServiceRequest request in urgent)
            {
                bool blocked = _manager.IsBlocked(request);

                ListViewItem item = new ListViewItem(position.ToString())
                {
                    Tag = request,
                    UseItemStyleForSubItems = false
                };
                item.SubItems.Add(request.ReferenceNumber);
                item.SubItems.Add(blocked ? request.Title + " (blocked)" : request.Title);
                ListViewItem.ListViewSubItem priority = item.SubItems.Add(request.Priority.ToString());
                int days = request.DaysOpen(now);
                item.SubItems.Add(days == 1 ? "1 day" : days + " days");

                priority.ForeColor = Theme.GetRequestPriorityColour(request.Priority);
                if (blocked)
                {
                    item.SubItems[2].ForeColor = Theme.Danger;
                }

                lvUrgent.Items.Add(item);
                position++;
            }

            lvUrgent.EndUpdate();
            btnDispatch.Enabled = urgent.Count > 0;
        }

        // ------------------------------------------------------------------
        // Related tab (directed graph)
        // ------------------------------------------------------------------

        private void ShowRelated(ServiceRequest? request)
        {
            lvRelated.BeginUpdate();
            lvRelated.Items.Clear();

            if (request == null)
            {
                lblRelatedIntro.Text = "Select a request to see which requests it is waiting on and which are waiting on it.";
                lvRelated.EndUpdate();
                return;
            }

            RelatedRequestsReport report = _manager.GetRelatedRequests(request);

            if (!report.HasRelationships)
            {
                lblRelatedIntro.Text = request.ReferenceNumber +
                    " does not depend on any other request, and no request is waiting for it.";
            }
            else
            {
                lblRelatedIntro.Text = string.Format(
                    "Some jobs must wait for others, e.g. a road cannot be resurfaced until the burst pipe under it is fixed. " +
                    "These links form a directed graph. Breadth-first search finds every request linked to {0}, " +
                    "level by level; depth-first search puts the open ones in the order they must be resolved.",
                    request.ReferenceNumber);
            }

            foreach (RelatedRequest related in report.WaitingOn)
            {
                AddRelatedItem(related.Request, grpWaitingOn, DescribeLevel(related));
            }
            if (report.WaitingOn.Count == 0) AddPlaceholder(grpWaitingOn, "Not waiting on any other request.");

            foreach (RelatedRequest related in report.Unblocks)
            {
                AddRelatedItem(related.Request, grpUnblocks, DescribeLevel(related));
            }
            if (report.Unblocks.Count == 0) AddPlaceholder(grpUnblocks, "No other request is waiting for this one.");

            if (report.WaitingOn.Count > 0 && report.ResolutionOrder.Count > 0)
            {
                int step = 1;
                foreach (ServiceRequest item in report.ResolutionOrder)
                {
                    AddRelatedItem(item, grpOrder, "Step " + step);
                    step++;
                }
            }
            else
            {
                AddPlaceholder(grpOrder, report.IsBlocked ? "-" : "Nothing needs to be resolved first.");
            }

            lvRelated.EndUpdate();
        }

        private static string DescribeLevel(RelatedRequest related)
        {
            return related.Level == 1 ? "Direct" : "Level " + related.Level;
        }

        private void AddRelatedItem(ServiceRequest request, ListViewGroup group, string link)
        {
            bool blocked = _manager.IsBlocked(request);

            ListViewItem item = new ListViewItem(request.ReferenceNumber, group)
            {
                Tag = request,
                UseItemStyleForSubItems = false
            };
            item.SubItems.Add(request.Title);
            ListViewItem.ListViewSubItem status = item.SubItems.Add(DescribeStatus(request, blocked));
            item.SubItems.Add(link);
            status.ForeColor = blocked ? Theme.Danger : Theme.GetStatusColour(request.Status);

            lvRelated.Items.Add(item);
        }

        private void AddPlaceholder(ListViewGroup group, string text)
        {
            ListViewItem item = new ListViewItem(string.Empty, group) { ForeColor = Theme.TextMuted };
            item.SubItems.Add(text);
            lvRelated.Items.Add(item);
        }

        // ------------------------------------------------------------------
        // Crew route tab (weighted graph)
        // ------------------------------------------------------------------

        private void RefreshRoute()
        {
            string? department = cboRouteDepartment.SelectedIndex <= 0
                ? null
                : cboRouteDepartment.SelectedItem as string;

            CrewRoutePlan plan = _manager.PlanCrewRoute(department);
            _currentPlan = plan;
            routeMap.ShowPlan(plan);
            txtRouteSummary.Text = DescribeRoute(plan);
        }

        /// <summary>
        /// Shows the current crew route in a separate, resizable window so the map
        /// can be read at full size.
        /// </summary>
        private void BtnLargerMap_Click(object? sender, EventArgs e)
        {
            if (_currentPlan == null) return;

            using (Form mapWindow = new Form())
            {
                mapWindow.Text = "Crew route - " + _currentPlan.Department;
                mapWindow.StartPosition = FormStartPosition.CenterParent;
                mapWindow.Size = new Size(S(1100), S(780));
                mapWindow.MinimumSize = new Size(S(600), S(450));
                mapWindow.BackColor = Color.White;
                mapWindow.ShowInTaskbar = false;
                mapWindow.KeyPreview = true;
                mapWindow.KeyDown += (s, args) =>
                {
                    if (args.KeyCode == Keys.Escape) mapWindow.Close();
                };

                TableLayoutPanel layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Padding = new Padding(S(10))
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(320)));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                RouteMapPanel largeMap = new RouteMapPanel { Dock = DockStyle.Fill };
                largeMap.SetNetwork(_manager.Suburbs, _manager.Roads, _manager.Depot);
                largeMap.ShowPlan(_currentPlan);

                TextBox summary = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.White,
                    Font = Theme.BodyFont,
                    ForeColor = Theme.TextPrimary,
                    Text = DescribeRoute(_currentPlan),
                    Margin = new Padding(S(10), 0, 0, 0)
                };

                layout.Controls.Add(largeMap, 0, 0);
                layout.Controls.Add(summary, 1, 0);
                mapWindow.Controls.Add(layout);

                mapWindow.ShowDialog(this);
            }
        }

        private static string DescribeRoute(CrewRoutePlan plan)
        {
            string newLine = Environment.NewLine;
            StringBuilder text = new StringBuilder();

            if (plan.Sites.Count == 0)
            {
                text.Append("There are no open requests for ").Append(plan.Department)
                    .Append(" on the road network, so no route is needed.");
                return text.ToString();
            }

            int openRequests = 0;
            foreach (int count in plan.OpenRequestsBySuburb.Values) openRequests += count;

            text.Append("Crew route: ").Append(plan.Department).Append(newLine);
            text.AppendFormat("{0} open requests in {1} suburbs, starting from the depot in {2}.",
                openRequests, plan.Sites.Count, plan.Depot).Append(newLine).Append(newLine);

            text.AppendFormat("Minimum spanning tree: {0:0.0} km in {1} links. Dijkstra's algorithm found the " +
                              "shortest road distance between each pair of sites; Prim's algorithm then chose the " +
                              "cheapest set of links that connects them all.",
                plan.NetworkKm, plan.Legs.Count).Append(newLine).Append(newLine);

            text.Append("Suggested visiting order (depth-first walk of the tree), about ")
                .AppendFormat("{0:0.0} km in total:", plan.TourKm).Append(newLine);
            List<string> stops = new List<string>();
            int stopNumber = 0;
            foreach (string stop in plan.VisitOrder)
            {
                if (string.Equals(stop, plan.Depot, StringComparison.OrdinalIgnoreCase))
                {
                    stops.Add(plan.Depot);
                    continue;
                }

                stopNumber++;
                plan.OpenRequestsBySuburb.TryGetValue(stop, out int open);
                stops.Add(string.Format("{0}. {1} ({2} open)", stopNumber, stop, open));
            }
            text.Append("  ").Append(string.Join(" > ", stops)).Append(newLine).Append(newLine);

            text.Append("Links in the tree:").Append(newLine);
            foreach (RouteLeg leg in plan.Legs)
            {
                text.AppendFormat("  {0} to {1}: {2:0.0} km", leg.From, leg.To, leg.DistanceKm);
                if (leg.RoadPath.Count > 2)
                {
                    List<string> via = leg.RoadPath.GetRange(1, leg.RoadPath.Count - 2);
                    text.Append(" (via ").Append(string.Join(", ", via)).Append(')');
                }
                text.Append(newLine);
            }

            return text.ToString().TrimEnd();
        }

        // ------------------------------------------------------------------
        // Under the hood tab
        // ------------------------------------------------------------------

        private void RefreshInsights()
        {
            lvStructures.BeginUpdate();
            lvStructures.Items.Clear();

            foreach (StructureSummary summary in _manager.GetStructureSummaries())
            {
                ListViewItem item = new ListViewItem(summary.Structure);
                item.SubItems.Add(summary.Purpose);
                item.SubItems.Add(summary.Size);
                item.SubItems.Add(summary.Shape);
                item.ToolTipText = summary.Structure + ": " + summary.Purpose + ". " + summary.Shape + ".";
                lvStructures.Items.Add(item);
            }

            lvStructures.EndUpdate();

            lvChecks.BeginUpdate();
            lvChecks.Items.Clear();

            int passed = 0;
            List<SelfCheckResult> results = _manager.RunSelfChecks();

            foreach (SelfCheckResult result in results)
            {
                ListViewItem item = new ListViewItem(result.Check) { UseItemStyleForSubItems = false };
                ListViewItem.ListViewSubItem outcome = item.SubItems.Add(result.Passed ? "Pass" : "FAIL");
                item.SubItems.Add(result.Detail);
                item.ToolTipText = result.Check + ": " + result.Detail;
                outcome.ForeColor = result.Passed ? Theme.Accent : Theme.Danger;
                outcome.Font = Theme.SmallBoldFont;
                lvChecks.Items.Add(item);

                if (result.Passed) passed++;
            }

            lvChecks.EndUpdate();
            tabInsights.Text = passed == results.Count ? "Under the hood" : "Under the hood (!)";
        }
    }
}
