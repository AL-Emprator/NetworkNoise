using System.Reflection.Emit;
using Label = System.Windows.Forms.Label;

namespace NetNoise
{
    public partial class Form1 : Form
    {

        // UI colors and fonts 
        private static readonly Color C_BG_DEEP = Color.FromArgb(13, 17, 23);   // #0D1117
        private static readonly Color C_BG_PANEL = Color.FromArgb(22, 27, 34);   // #161B22
        private static readonly Color C_BORDER = Color.FromArgb(33, 38, 45);   // #21262D
        private static readonly Color C_BLUE = Color.FromArgb(88, 166, 255);  // #58A6FF
        private static readonly Color C_RED = Color.FromArgb(248, 81, 73);   // #F85149
        private static readonly Color C_TEXT_MUT = Color.FromArgb(110, 118, 129);  // #6E7681
        private static readonly Color C_AMBER = Color.FromArgb(210, 153, 34);   // #D29922
        private static readonly Color C_GREEN = Color.FromArgb(63, 185, 80);   // #3FB950
        private static readonly Color C_TEXT_PRI = Color.FromArgb(201, 209, 217);  // #C9D1D9
        private static readonly Color C_SEL = Color.FromArgb(31, 51, 88);
        private static readonly Color C_ROW_ALT = Color.FromArgb(22, 27, 34);



        private static readonly Font F_MONO_SM = new("Consolas", 9f);
        private static readonly Font F_TITLE = new("Consolas", 10f, FontStyle.Bold);
        private static readonly Font F_LABEL = new("Consolas", 8f);


        // Controls (will be created in the Designer or at runtime in EnsureControls)
        private ComboBox _cboDevices = null!;
        private Button _btnRefresh = null!;
        private Button _btnStartStop = null!;
        private Panel _statusDot = null!;

        private Label _lblPpsPeak = null!;
        private Label _lblTotal = null!;
        private Label _lblProto = null!;
        private Label _lblAlerts = null!;
        private Label _lblStatus = null!;
        private Label _lblPps = null!;
        private Label _lblAlertsSub = null!;
        private Label _lblFooter = null!;

        private DataGridView _gridPackets = null!;
        private RichTextBox _txtPayload = null!;
        private DataGridView _gridAlerts = null!;

        //Filter State
        private string _searchText = "";
        private string _activeFilter = "All";


        // ── Constructor
        public Form1()
        {
            InitializeComponent();

            BuildLayout();
        }

        private void BuildLayout()
        {

            Text = "NetNoise Live Packet Capture";
            Size = new Size(1300, 820);
            MinimumSize = new Size(900, 600);
            BackColor = C_BG_DEEP;
            ForeColor = C_TEXT_PRI;
            Font = F_MONO_SM;
            StartPosition = FormStartPosition.CenterScreen;


            // ── Main vertical stack ───────────────────────────────────────────
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                BackColor = C_BG_DEEP,
                Padding = new Padding(0),

            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // top bar
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));   // stats
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 60));   // packet table
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));   // alerts
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));   // footer
            Controls.Add(root);



            root.Controls.Add(BuildTopBar(), 0, 0);
            root.Controls.Add(BuildStatsRow(), 0, 1);
            root.Controls.Add(BuildPacketGrid(), 0, 2);
            root.Controls.Add(BuildAlertsGrid(), 0, 3);
            root.Controls.Add(BuildFooter(), 0, 4);


        }


        private Panel BuildTopBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG_PANEL,
                Padding = new Padding(10, 0, 10, 0),
            };
            bar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_BORDER), 0, bar.Height - 1, bar.Width, bar.Height - 1);

            // Title label
            var title = new Label
            {
                Text = "⬡ NetNoise",
                Font = F_TITLE,
                ForeColor = C_BLUE,
                AutoSize = true,
                Location = new Point(12, 14),
            };

            // Device ComboBox
            _cboDevices = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = C_BG_DEEP,
                ForeColor = C_TEXT_PRI,
                FlatStyle = FlatStyle.Flat,
                Font = F_MONO_SM,
                Width = 460,
                Location = new Point(240, 12),
                Height = 24,
            };

            // Refresh button
            _btnRefresh = MakeButton("↻  Refresh", C_BG_DEEP, C_TEXT_MUT, C_BORDER);
            _btnRefresh.Location = new Point(710, 12);
            _btnRefresh.Width = 88;


            // Start / Stop
            _btnStartStop = MakeButton("▶  Start", C_GREEN, Color.White, Color.Transparent);
            _btnStartStop.Location = new Point(806, 12);
            _btnStartStop.Width = 96;
            _btnStartStop.BackColor = Color.FromArgb(35, 134, 54);


            // Status dot (green circle)
            _statusDot = new Panel
            {
                Width = 8,
                Height = 8,
                BackColor = C_TEXT_MUT,
                Location = new Point(912, 19),
            };

            MakeRound(_statusDot);

            // Status text
            _lblStatus = new Label
            {
                Text = "Idle",
                ForeColor = C_TEXT_MUT,
                Font = F_MONO_SM,
                AutoSize = true,
                Location = new Point(926, 16),
            };

            bar.Controls.AddRange(new Control[]
             { title, _cboDevices, _btnRefresh, _btnStartStop, _statusDot, _lblStatus });
            return bar;

        }


        private Panel BuildStatsRow()
        {
            var row = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG_DEEP,
                Padding = new Padding(10, 8, 10, 8),
            };

            int x = 10;
            foreach (var (label, valRef, peakRef, color) in new (string, string, string, Color)[] {

                ("Packets / sec",     "0",      "peak 0",    C_GREEN),
            ("Total captured",    "0",      "this session", C_BLUE),
            ("Protocol split",    "TCP 0%", "UDP 0% / Other 0%", C_AMBER),
            ("Alerts",            "0",      "0 HIGH · 0 MED",    C_RED),
        })
            {
                var card = BuildStatCard(label, valRef, peakRef, color, out var valLbl, out var subLbl);
                card.Left = x;
                card.Top = 0;
                row.Controls.Add(card);

                switch (label)
                {
                    case "Packets / sec": _lblPps = valLbl; _lblPpsPeak = subLbl; break;
                    case "Total captured": _lblTotal = valLbl; break;
                    case "Protocol split": _lblProto = valLbl; break;
                    case "Alerts":
                        _lblAlerts = valLbl;
                        _lblAlertsSub = subLbl;
                        break;
                }
                x += card.Width + 10;
            }
            row.Resize += (_, _) =>
            {
                int w = (row.Width - 50) / 4;
                int xi = 10;
                foreach (Control c in row.Controls)
                {
                    c.Width = w;
                    c.Left = xi;
                    c.Height = row.Height - 16;
                    xi += w + 10;
                }
            };

            return row;
        }



        private Panel BuildStatCard(string label, string val, string sub, Color accent,
                               out Label valLbl, out Label subLbl)
        {
            var card = new Panel
            {
                BackColor = C_BG_PANEL,
                Width = 260,
                Height = 68,
                Padding = new Padding(12, 10, 12, 10),
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var pen = new Pen(C_BORDER);
                var rc = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                g.DrawRectangle(pen, rc);
                // left accent stripe
                using var accentBrush = new SolidBrush(accent);
                g.FillRectangle(accentBrush, 0, 0, 3, card.Height);
            };

            var lbl = new Label
            {
                Text = label.ToUpper(),
                Font = F_LABEL,
                ForeColor = C_TEXT_MUT,
                AutoSize = false,
                Bounds = new Rectangle(14, 10, 220, 14),
            };

            valLbl = new Label
            {
                Text = val,
                Font = new Font("Consolas", 18f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = false,
                Bounds = new Rectangle(14, 26, 220, 24),
            };

            subLbl = new Label
            {
                Text = sub,
                Font = F_LABEL,
                ForeColor = C_TEXT_MUT,
                AutoSize = false,
                Bounds = new Rectangle(14, 52, 220, 13),
            };

            card.Controls.AddRange(new Control[] { lbl, valLbl, subLbl });
            return card;
        }




        // ── Packet DataGridView ───────────────────────────────────────────────

        private Panel BuildPacketGrid()
        {
            var container = new Panel { Dock = DockStyle.Fill, BackColor = C_BG_DEEP };



            _gridPackets = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = C_BG_DEEP,
                GridColor = C_BORDER,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                ColumnHeadersVisible = true,
                // ★ KEY FIX: disable OS visual styles so our custom colours are respected
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 26,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowTemplate = { Height = 22 },
                ShowCellToolTips = true,
                ScrollBars = ScrollBars.Vertical,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = C_BG_DEEP,
                    ForeColor = C_TEXT_PRI,
                    Font = F_MONO_SM,
                    SelectionBackColor = C_SEL,
                    SelectionForeColor = Color.White,
                    Padding = new Padding(4, 2, 4, 2),
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = C_BG_PANEL,
                    ForeColor = C_TEXT_MUT,
                    Font = new Font("Consolas", 8f, FontStyle.Bold),
                    SelectionBackColor = C_BG_PANEL,
                    SelectionForeColor = C_TEXT_MUT,
                    Padding = new Padding(4, 0, 0, 0),
                },

            };

            _gridPackets.Columns.AddRange(
                Col("Time", 92),
                Col("Proto", 64),
                Col("Src IP", 130),
                Col("Src Port", 68),
                Col("Dst IP", 130),
                Col("Dst Port", 68),
                Col("Info", 150),
                Col("Len", 56),
                Col("Payload preview", 0));

            _gridPackets.Columns[8].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _gridPackets.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = C_ROW_ALT,
                ForeColor = C_TEXT_PRI,
                SelectionBackColor = C_SEL,
                SelectionForeColor = Color.White,
            };

            // _gridPackets.SelectionChanged += OnPacketSelected;  <-------------------------------------------------------- handler for when user selects a packet row

            // Payload detail box (bottom of packet section)
            _txtPayload = new RichTextBox
            {
                Height = 52,
                BackColor = C_BG_PANEL,
                ForeColor = C_AMBER,
                Font = F_MONO_SM,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(10, 6, 10, 6),
                Text = "  Select a packet to inspect its payload…",
                Dock = DockStyle.Bottom,
            };

            var payloadDivider = new Panel
            { Height = 1, BackColor = C_BORDER, Dock = DockStyle.Bottom };

            // ── Filter row ────────────────────────────────────────────────────────
            var filterRow = new Panel
            {
                Height = 34,
                BackColor = C_BG_DEEP,
                Padding = new Padding(10, 5, 10, 5),
                Dock = DockStyle.Top,
            };

            var searchBox = new TextBox
            {
                Width = 200,
                Height = 22,
                BackColor = Color.FromArgb(22, 27, 34),
                ForeColor = C_TEXT_MUT,
                BorderStyle = BorderStyle.FixedSingle,
                Font = F_MONO_SM,
                PlaceholderText = "Filter by IP, port, payload…",
            };

            searchBox.TextChanged += (_, _) =>
            {
                _searchText = searchBox.Text.Trim().ToLowerInvariant();
                // ApplyFilter(); <-------------------------------------------------------- handler for when user types in the filter box
            };

            var filterButtons = new[] { "All", "TCP", "SYN", "HTTP", "DNS" };
            int bx = 0;
            foreach (var name in filterButtons)
            {
                var n = name; // capture
                var btn = MakeFilterButton(n, n == "All");
                btn.Location = new Point(bx, 0);
                btn.Click += (_, _) =>
                {
                    _activeFilter = n;
                    // Update button states
                    foreach (Control c in filterRow.Controls)
                        if (c is Button fb)
                            StyleFilterButton(fb, fb.Text == n);
                    //ApplyFilter();
                };
                filterRow.Controls.Add(btn);
                bx += btn.Width + 6;
            }

            searchBox.Location = new Point(bx + 4, 1);
            filterRow.Controls.Add(searchBox);


            // ── Dock order: Top controls added first (header → filter)
            //               Bottom controls added first (payload → divider)
            //               Fill last ─────────────────────────────────────────────

            var header = SectionHeader("Live packet feed", "newest first · max 500");
            header.Dock = DockStyle.Top;

            // Bottom first (stacks bottom-up)
            container.Controls.Add(_txtPayload);
            container.Controls.Add(payloadDivider);
            // Fill
            container.Controls.Add(_gridPackets);
            // Top last (stacks top-down — last added = bottom of top strip)
            container.Controls.Add(filterRow);
            container.Controls.Add(header);

            return container;
        }





        private Panel BuildAlertsGrid()
        {
            var container = new Panel { Dock = DockStyle.Fill, BackColor = C_BG_DEEP };

            _gridAlerts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = C_BG_DEEP,
                BorderStyle = BorderStyle.None,
                GridColor = C_BG_DEEP,
                RowHeadersVisible = false,
                ColumnHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ScrollBars = ScrollBars.Vertical,
                EnableHeadersVisualStyles = false,
                CellBorderStyle = DataGridViewCellBorderStyle.None,

                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = C_BG_DEEP,
                    ForeColor = C_TEXT_PRI,
                    SelectionBackColor = Color.FromArgb(20, 25, 35),
                    SelectionForeColor = Color.White,
                    Font = F_MONO_SM,
                    Padding = new Padding(6, 4, 6, 4)
                },

                RowTemplate = { Height = 34 }
            };

            _gridAlerts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Width = 82,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            _gridAlerts.Columns.Add(new DataGridViewTextBoxColumn
            {
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            _gridAlerts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Width = 90,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            container.Controls.Add(_gridAlerts);

            var header = SectionHeader("Alerts", "0 active");
            header.Dock = DockStyle.Top;
            container.Controls.Add(header);

            return container;
        }


        // ── Footer ────────────────────────────────────────────────────────────

        private Panel BuildFooter()
        {
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG_PANEL,
                Height = 26,
                Padding = new Padding(6, 0, 6, 0)
            };

            footer.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(C_BORDER),
                    0,
                    0,
                    footer.Width,
                    0);

            // ── Footer text ───────────────────────────────────────────────

            _lblFooter = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = C_TEXT_MUT,
                Font = F_LABEL,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),

                Text =
                    "NetworkSecurityMonitor v0.1.0  ·  .NET 8  ·  SharpPcap  ·  PacketDotNet"
            };

            // ── Export button ─────────────────────────────────────────────

            var btnExport = new Button
            {
                Text = "Export Alerts",
                Dock = DockStyle.Right,
                Width = 120,

                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(18, 22, 30),
                ForeColor = Color.White,

                Font = F_LABEL,
                Cursor = Cursors.Hand
            };

            btnExport.FlatAppearance.BorderSize = 1;
            btnExport.FlatAppearance.BorderColor = C_BORDER;

            // btnExport.Click += (_, _) => ExportAlertsAsJson(); <------------------------------------------------------------ handler for export button   

            // ── Import button ─────────────────────────────────────────────

            var btnImport = new Button
            {
                Text = "Import Rules",
                Dock = DockStyle.Right,
                Width = 120,

                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(18, 22, 30),
                ForeColor = Color.White,

                Font = F_LABEL,
                Cursor = Cursors.Hand
            };

            btnImport.FlatAppearance.BorderSize = 1;
            btnImport.FlatAppearance.BorderColor = C_BORDER;

            //btnImport.Click += (_, _) => ImportRules(); <------------------------------------------------------------------------------------------ handler for import button

            // ── Order matters ─────────────────────────────────────────────

            footer.Controls.Add(btnExport);
            footer.Controls.Add(btnImport);
            footer.Controls.Add(_lblFooter);

            return footer;
        }



        // ── UI updates (UI thread) ────────────────────────────────────────────
        // ── Packet selection → payload preview ───────────────────────────────


        // ═════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════════

        private static Panel SectionHeader(string title, string right)
        {
            var bar = new Panel { Height = 26, BackColor = C_BG_PANEL };
            bar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.DrawLine(new Pen(C_BORDER), 0, 0, bar.Width, 0);
                g.DrawLine(new Pen(C_BORDER), 0, bar.Height - 1, bar.Width, bar.Height - 1);
                using var accentBrush = new SolidBrush(C_BLUE);
                g.FillRectangle(accentBrush, 10, 7, 3, 12);
                using var fnt = new Font("Consolas", 8.5f, FontStyle.Bold);
                using var fntR = new Font("Consolas", 8f);
                using var brush = new SolidBrush(C_TEXT_MUT);
                using var brushR = new SolidBrush(C_TEXT_MUT);
                g.DrawString(title.ToUpper(), fnt, brush, 20, 7);
                if (!string.IsNullOrEmpty(right))
                    g.DrawString(right, fntR, brushR, bar.Width - 200, 8);
            };
            return bar;
        }


        private static Button MakeButton(string text, Color bg, Color fg, Color border)
        {
            var b = new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 9f),
                Height = 24,
                UseVisualStyleBackColor = false,
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        private static DataGridViewTextBoxColumn Col(string name, int width) => new() { HeaderText = name, Width = width, SortMode = DataGridViewColumnSortMode.NotSortable };

        private static void MakeRound(Panel p)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, p.Width, p.Height);
            p.Region = new Region(path);
        }

        private static Button MakeFilterButton(string text, bool active)
        {
            var b = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 9f),
                Height = 24,
                Width = text.Length * 8 + 20,
                UseVisualStyleBackColor = false,
            };
            StyleFilterButton(b, active);
            return b;
        }

        private static void StyleFilterButton(Button b, bool active)
        {
            b.BackColor = active ? Color.FromArgb(31, 51, 88) : Color.FromArgb(22, 27, 34);
            b.ForeColor = active ? C_BLUE : C_TEXT_MUT;
            b.FlatAppearance.BorderColor = active ? C_BLUE : C_BORDER;
            b.FlatAppearance.BorderSize = 1;

        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            // Layout and events are already initialized in constructor.
        }

    }
}
