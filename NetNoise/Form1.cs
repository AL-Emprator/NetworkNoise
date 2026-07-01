using CaptureLib.Parsers.DNS;
using CaptureLib.Parsers.HTTP;
using CaptureLib.Parsers.TCP;
using CoreLib.Modules;
using DetectionLib.DnsTunnelDetection;
using DetectionLib.HttpCredentialDetection;
using DetectionLib.PortscanDetection;
using SharpPcap;
using System.Reflection.Emit;
using Label = System.Windows.Forms.Label;

namespace NetNoise;


public partial class Form1 : Form
{

    //Fields 
    private bool _capturing;
    private int _packetsThisSec;
    private int _peakPps;

    private int _countTcp;
    private int _countHttp;
    private int _countDns;
    private int _countUdp;
    private int _countOther;
    private int _totalPackets;

    private ICaptureDevice? _device;

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
    private static readonly Font F_MONO_MD = new("Consolas", 10f);

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

    //Events State 

    // Packet counter reset timer (fires every second)
    private readonly System.Windows.Forms.Timer _ppsTimer = new() { Interval = 1000 };
    private readonly List<PacketRow> _allPackets = new(500);


    //Parsers
    private readonly TcpParser _tcpParser = new();
    private readonly HttpParser _httpParser = new();
    private readonly DnsParser _dnsParser = new();


    //Detection

    private readonly PortScanDetector _portScanDetector = new();
    private readonly HttpCredentialDetector _httpCredentialDetector = new();
    private readonly DnsTunnelDetector _dnsTunnelDetector = new();


    // ── Constructor
    public Form1()
    {
        InitializeComponent();
        BuildLayout();
        WireEvents();
        PopulateDevices();
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

        _gridPackets.SelectionChanged += OnPacketSelected;  //<-------------------------------------------------------- handler for when user selects a packet row
        _gridPackets.CellDoubleClick += OnPacketDoubleClick;


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
            ApplyFilter(); //<-------------------------------------------------------- handler for when user types in the filter box
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
                ApplyFilter();
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



    // ── Filter logic ──────────────────────────────────────────────────────────

    private void ApplyFilter()
    {
        _gridPackets.SuspendLayout();
        _gridPackets.Rows.Clear();

        foreach (var p in _allPackets.Where(MatchesFilter).Reverse())
            InsertRow(p);

        _gridPackets.ResumeLayout();
    }

    private bool MatchesFilter(PacketRow p)
    {
        bool passFilter = _activeFilter switch
        {
            "TCP" => p.Protocol == "TCP",
            "HTTP" => p.Protocol == "HTTP",
            "SYN" => p.IsSyn,
            "DNS" => p.SourcePort == 53 || p.DestinationPort == 53,
            _ => true
        };

        if (!passFilter)
            return false;

        if (string.IsNullOrWhiteSpace(_searchText))
            return true;

        var s = _searchText;

        return p.SourceIp.Contains(s, StringComparison.OrdinalIgnoreCase)
            || p.DestinationIp.Contains(s, StringComparison.OrdinalIgnoreCase)
            || p.SourcePort.ToString().Contains(s)
            || p.DestinationPort.ToString().Contains(s)
            || p.Protocol.Contains(s, StringComparison.OrdinalIgnoreCase)
            || p.Info.Contains(s, StringComparison.OrdinalIgnoreCase)
            || p.PayloadPreview.Contains(s, StringComparison.OrdinalIgnoreCase)
            || p.FullPayload.Contains(s, StringComparison.OrdinalIgnoreCase);
    }


    // ── Alerts DataGridView ───────────────────────────────────────────────
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
                "NezNoise v0.1.0  ·  .NET 8  ·  SharpPcap  ·  PacketDotNet"
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



    // ═════════════════════════════════════════════════════════════════════════
    // EVENTS & CAPTURE LOGIC TCP
    // ═════════════════════════════════════════════════════════════════════════

    // WireEventArgs is the event args type used by SharpPcap for packet capture events. It contains the captured packet and metadata.
    private void WireEvents()
    {
        _btnRefresh.Click += (_, _) => PopulateDevices();
        _btnStartStop.Click += OnStartStop;
        _ppsTimer.Tick += OnPpsTick;
    }


    //  PopulateDevices to list available network interfaces in the combo box 
    private void PopulateDevices() {
        _cboDevices.Items.Clear();
        foreach (var dev in CaptureDeviceList.Instance)
            _cboDevices.Items.Add($"{dev.Description}");

        if (_cboDevices.Items.Count > 0)
            _cboDevices.SelectedIndex = 0;
        else
            _cboDevices.Items.Add("(no devices — run as Administrator)");

    }

    private void OnStartStop(object? sender, EventArgs e)
    {
        if (_capturing) StopCapture();
        else StartCapture();
    }

    private void StartCapture()
    {
        if (_cboDevices.SelectedIndex < 0) return;

        var devices = CaptureDeviceList.Instance;
        if (devices.Count == 0 || _cboDevices.SelectedIndex >= devices.Count) return;

        _device = devices[_cboDevices.SelectedIndex];

        var deviceName = _device.Description
        .Replace("Adapter for ", "")
        .Replace("capture", "")
        .Trim();


        if (deviceName.Length > 32)
            deviceName = deviceName[..32] + "...";

        _lblFooter.Text =
            $"Device: {deviceName}   Dropped: 0   Buffer: 128 / 1000   Threads: capture + UI   NetworkSecurityMonitor v0.1.0 · .NET 8";


        _device.OnPacketArrival += OnPacketArrival; // <------------------------------------------------------------ handler for packet arrival events 
        _device.Open(DeviceModes.Promiscuous, 1000);
        _device.StartCapture();

        _capturing = true;
        _ppsTimer.Start();

        _btnStartStop.Text = "⏹  Stop";
        _btnStartStop.BackColor = Color.FromArgb(139, 0, 0);
        _statusDot.BackColor = C_GREEN;
        _lblStatus.ForeColor = C_GREEN;
        _lblStatus.Text = $"LIVE  —  {_device.Description[..Math.Min(40, _device.Description.Length)]}";
    }


    private void StopCapture()
    {
        _ppsTimer.Stop();

        if (_device is not null)
        {
            _device.StopCapture();
            _device.Close();
            _device.OnPacketArrival -= OnPacketArrival; // <------------------------------------------------------------ detach packet arrival handler
            _device = null;
        }

        _capturing = false;
        _btnStartStop.Text = "▶  Start";
        _btnStartStop.BackColor = Color.FromArgb(35, 134, 54);
        _statusDot.BackColor = C_TEXT_MUT;
        _lblStatus.ForeColor = C_TEXT_MUT;
        _lblStatus.Text = "Stopped";
    }




    // ── UI updates (UI thread) ────────────────────────────────────────────




    // ── Packet selection → payload preview ───────────────────────────────
    private void OnPacketSelected(object? sender, EventArgs e)
    {
        if (_gridPackets.SelectedRows.Count == 0) return;

        var row = _gridPackets.SelectedRows[0];

        if (row.Tag is PacketRow packet)
        {
            _txtPayload.Text = string.IsNullOrWhiteSpace(packet.FullPayload)
                ? "  (no payload)"
                : $"  {packet.FullPayload}";
        }
    }

    private void AddPacketRow(PacketRow p)
    {
        _allPackets.Insert(0, p);

        while (_allPackets.Count > 500)
            _allPackets.RemoveAt(_allPackets.Count - 1);

        if (!MatchesFilter(p))
            return;

        InsertRow(p);

        while (_gridPackets.Rows.Count > 500)
            _gridPackets.Rows.RemoveAt(_gridPackets.Rows.Count - 1);

        _lblTotal.Text = _totalPackets.ToString("N0");
    }


    private void OnPpsTick(object? sender, EventArgs e)
    {

        var pps = Interlocked.Exchange(ref _packetsThisSec, 0);

        if (pps > _peakPps)
            _peakPps = pps;

        _lblPps.Text = pps.ToString();
        _lblPpsPeak.Text = $"↑ peak {_peakPps:N0}";

        var tcp = _countTcp;
        var http = _countHttp;
        var dns = _countDns;
        var udp = _countUdp;
        var other = _countOther;

        var total = tcp + http + dns + udp + other;

        if (total == 0)
        {
            _lblProto.Text = "TCP 0%  UDP 0%  Other 0%";
            return;
        }

        int tcpPct = (int)Math.Round((tcp + http) * 100.0 / total);
        int udpPct = (int)Math.Round((udp + dns) * 100.0 / total);
        int otherPct = Math.Max(0, 100 - tcpPct - udpPct);

        _lblProto.Text = $"TCP {tcpPct}%  UDP {udpPct}%  Other {otherPct}%";
    }




    /// <summary>Inserts one TcpPacketInfo as the top row in the grid.</summary>
    private void InsertRow(PacketRow p)
    {
        _gridPackets.Rows.Insert(0,
            p.Timestamp.ToString("HH:mm:ss.fff"),
            p.Protocol,
            p.SourceIp,
            p.SourcePort.ToString(),
            p.DestinationIp,
            p.DestinationPort.ToString(),
            p.Info,
            p.Length,
            p.PayloadPreview);

        var row = _gridPackets.Rows[0];
        row.Tag = p;


        row.Cells[1].Style.ForeColor = p.Protocol switch
        {
            "HTTP" => C_AMBER,
            "DNS" => C_GREEN,
            _ => C_BLUE
        };

        row.Cells[2].Style.ForeColor = C_BLUE;
        row.Cells[6].Style.ForeColor = p.IsHttp ? C_GREEN
                                  : p.IsSyn ? C_BLUE
                                  : C_TEXT_PRI;
    }



    //Alerts Row Formatting

    // ── Alert row formatting ──────────────────────────────────────────────

    public void AddAlert(string severity, string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AddAlert(severity, message));
            return;
        }

        var time = DateTime.Now.ToString("HH:mm:ss");

        _gridAlerts.Rows.Insert(0, severity, message, time);

        var row = _gridAlerts.Rows[0];

        row.Height = 34;

        // ── Severity Badge ─────────────────────────────

        var sev = row.Cells[0];

        sev.Style.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        sev.Style.Font =
            new Font("Consolas", 8.5f, FontStyle.Bold);

        sev.Style.ForeColor = Color.White;

        sev.Style.SelectionBackColor =
            sev.Style.BackColor;

        sev.Style.BackColor = severity switch
        {
            "HIGH" => Color.FromArgb(110, 25, 25),
            "MED" => Color.FromArgb(100, 75, 20),
            _ => Color.FromArgb(25, 90, 45)
        };

        // ── Message ───────────────────────────────────

        var msg = row.Cells[1];

        msg.Style.ForeColor = C_TEXT_PRI;

        msg.Style.Font = new Font("Consolas", 9f);

        // ── Time ──────────────────────────────────────

        var tm = row.Cells[2];

        tm.Style.ForeColor = C_TEXT_MUT;

        tm.Style.Alignment =
            DataGridViewContentAlignment.MiddleRight;

        tm.Style.Font =
            new Font("Consolas", 8f);

        // ── Row separator line ────────────────────────

        row.DefaultCellStyle.BackColor =
            Color.FromArgb(10, 14, 20);

        // ── Max alerts ────────────────────────────────

        while (_gridAlerts.Rows.Count > 100)
            _gridAlerts.Rows.RemoveAt(
                _gridAlerts.Rows.Count - 1);


        int high = _gridAlerts.Rows.Cast<DataGridViewRow>().Count(r => r.Cells[0].Value?.ToString() == "HIGH");

        int med = _gridAlerts.Rows
            .Cast<DataGridViewRow>()
            .Count(r => r.Cells[0].Value?.ToString() == "MED");

        _lblAlerts.Text = _gridAlerts.Rows.Count.ToString();
        _lblAlertsSub.Text = $"{high} HIGH · {med} MED";
    }

    private void OnAlertCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex != 0 || e.Value is null) return;
        e.CellStyle.ForeColor = e.Value.ToString() switch
        {
            "HIGH" => C_RED,
            "MED" => C_AMBER,
            _ => C_GREEN,
        };
        e.CellStyle.Font = new Font("Consolas", 9f, FontStyle.Bold);

    }

    // ── Packet arrival (capture thread) ─────────────────────────────────

    // Called from the capture thread when a new packet arrives.
    private void OnPacketArrival(object sender, PacketCapture e) {

        var raw = e.GetPacket();

        // 1. Erst TCP prüfen, damit PortScan sofort erkannt wird
        var tcpInfo = _tcpParser.Parse(raw);

        if (tcpInfo != null)
        {
            var alert = _portScanDetector.Process(tcpInfo);
            if (alert is not null) {

                BeginInvoke(new Action(() =>
                {
                    AddAlert(
                    alert.Severity,
                    $"{alert.Title} | " +
                    $"Src {alert.SourceIp} → {alert.DestinationIp} | " +
                    $"{alert.PortsScanned} ports | " +
                    $"{alert.PortsPerSecond:0.0} p/s | " +
                    $"{alert.ScanType} | " +
                    $"{alert.MitreTechnique}");
                }));
            }
            // HTTP als Enrichment
            var httpInfo = _httpParser.Parse(raw);
            if (httpInfo is not null)
            {
                Interlocked.Increment(ref _countHttp);

                // ── HTTP credential detection ─────────────────────
                var httpAlert = _httpCredentialDetector.Process(httpInfo);

                if (httpAlert is not null)
                {
                    BeginInvoke(new Action(() =>
                    {
                        AddAlert(httpAlert.Severity, httpAlert.Message);
                    }));
                }


                BeginInvoke(new Action(() =>
                {
                    AddPacketRow(ToRow(httpInfo));
                }));

                return;
            }

            Interlocked.Increment(ref _countTcp);

            BeginInvoke(new Action(() =>
            {
                AddPacketRow(ToRow(tcpInfo));
            }));


            return;
        }




        // 2. DNS prüfen

        var dnsInfo = _dnsParser.Parse(raw);
        if (dnsInfo is not null)
        {

      

            // DNS Tunnel Detection
            var dnsTunnelAlert = _dnsTunnelDetector.Process(dnsInfo);
            if (dnsTunnelAlert is not null)
            {
                BeginInvoke(new Action(() =>
                {
                    AddAlert(
                        dnsTunnelAlert.Severity,
                        dnsTunnelAlert.Message);
                }));
            }


            Interlocked.Increment(ref _totalPackets);
            Interlocked.Increment(ref _packetsThisSec);
            Interlocked.Increment(ref _countDns);
            Interlocked.Increment(ref _countUdp);


            BeginInvoke(new Action(() => AddPacketRow(ToRow(dnsInfo))));
            return;

        }


        // 3. Andere Pakete zählen
        Interlocked.Increment(ref _totalPackets);
        Interlocked.Increment(ref _packetsThisSec);
        Interlocked.Increment(ref _countOther);

    }



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


    private static string BuildFlagString(TcpPacketInfo p)
    {
        var parts = new List<string>(4);
        if (p.IsSyn) parts.Add("SYN");
        if (p.IsAck) parts.Add("ACK");
        if (p.IsFin) parts.Add("FIN");
        if (p.IsRst) parts.Add("RST");
        if (p.IsPsh) parts.Add("PSH");
        return parts.Count > 0 ? string.Join("|", parts) : "—";
    }

    // ToRow methods convert packet info objects into DataGridViewRow objects for display in the grid.
    // TCP, HTTP, and DNS packet info are handled separately.
    private static PacketRow ToRow(TcpPacketInfo p)
    {
        var payloadText = p.Payload.Length > 0 ? p.PayloadAsText : "";

        return new PacketRow
        {
            Timestamp = p.Timestamp,
            Protocol = "TCP",
            SourceIp = p.SourceIp,
            SourcePort = p.SourcePort,
            DestinationIp = p.DestinationIp,
            DestinationPort = p.DestinationPort,
            Info = BuildFlagString(p),
            Length = $"{p.Length} B",
            PayloadPreview = MakePreview(payloadText),
            FullPayload = payloadText,
            IsSyn = p.IsSyn,
            IsHttp = false
        };
    }


    private static PacketRow ToRow(HttpPacketInfo p)
    {
        var info = p.IsRequest
            ? p.Method ?? "HTTP"
            : p.IsResponse
                ? "HTTP Response"
                : "HTTP";

        return new PacketRow
        {
            Timestamp = p.Timestamp,
            Protocol = "HTTP",
            SourceIp = p.SourceIp,
            SourcePort = p.SourcePort,
            DestinationIp = p.DestinationIp,
            DestinationPort = p.DestinationPort,
            Info = info,
            Length = $"{p.Length} B",
            PayloadPreview = MakePreview($"{p.Host} {p.Url} {p.RawHttpText}"),
            FullPayload = p.RawHttpText,
            IsSyn = false,
            IsHttp = true
        };
    }



    private static PacketRow ToRow(DnsPacketInfo p)
    {
        return new PacketRow
        {
            Timestamp = p.Timestamp,
            Protocol = "DNS",
            SourceIp = p.SourceIp,
            SourcePort = p.SourcePort,
            DestinationIp = p.DestinationIp,
            DestinationPort = p.DestinationPort,
            Info = p.IsResponse ? "Response" : "Query",
            Length = $"{p.Length} B",
            PayloadPreview = $"{p.QueryName} {p.QueryType}",
            FullPayload = $"{p.QueryName} Type={p.QueryType} TransactionId={p.TransactionId}",
            IsSyn = false,
            IsHttp = false
        };
    }


    private static string MakePreview(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "—";

        text = text.Replace("\r", "").Replace("\n", "↵");

        return text[..Math.Min(120, text.Length)];
    }


    // OnPacketDoubleClick ist called when a packet row in the DataGridView is double-clicked.
    // It opens a new window showing the full payload of the packet.
    private void OnPacketDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        var row = _gridPackets.Rows[e.RowIndex];

        if (row.Tag is not PacketRow packet)
            return;

        ShowPayloadWindow(packet);
    }

    //ShowPayloadWindow opens a new form displaying the full payload of the selected packet.
    private void ShowPayloadWindow(PacketRow packet)
    {
        var form = new Form
        {
            Text = $"{packet.Protocol} Payload — {packet.SourceIp}:{packet.SourcePort} → {packet.DestinationIp}:{packet.DestinationPort}",
            Size = new Size(900, 650),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = C_BG_DEEP,
            ForeColor = C_TEXT_PRI,
            Font = F_MONO_SM
        };

        var box = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = C_BG_DEEP,
            ForeColor = C_TEXT_PRI,
            Font = F_MONO_MD,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            Text = string.IsNullOrWhiteSpace(packet.FullPayload)
                ? "(no payload)"
                : packet.FullPayload
        };

        form.Controls.Add(box);
        form.Show(this);
    }


    private void Form1_Load(object? sender, EventArgs e)
    {
        // Layout and events are already initialized in constructor.
    }

}
