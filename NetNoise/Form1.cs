namespace NetNoise
{
    public partial class Form1 : Form
    {

        // UI colors and fonts 
        private static readonly Color C_BG_DEEP = Color.FromArgb(13, 17, 23);   // #0D1117
        private static readonly Color C_BG_PANEL = Color.FromArgb(22, 27, 34);   // #161B22
        private static readonly Color C_BORDER = Color.FromArgb(33, 38, 45);   // #21262D
        private static readonly Color C_BLUE = Color.FromArgb(88, 166, 255);  // #58A6FF
        private static readonly Color C_TEXT_MUT = Color.FromArgb(110, 118, 129);  // #6E7681
        private static readonly Color C_GREEN = Color.FromArgb(63, 185, 80);   // #3FB950
        private static readonly Color C_TEXT_PRI = Color.FromArgb(201, 209, 217);  // #C9D1D9
        
        private static readonly Font F_MONO_SM = new("Consolas", 9f);
        private static readonly Font F_TITLE = new("Consolas", 10f, FontStyle.Bold);



        // Controls (will be created in the Designer or at runtime in EnsureControls)
        private ComboBox _cboDevices = null!;
        private Button _btnRefresh = null!;
        private Button _btnStartStop = null!;
        private Panel _statusDot = null!;
        private Label _lblStatus = null!;

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







        // ═════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════════

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



        private static void MakeRound(Panel p)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, p.Width, p.Height);
            p.Region = new Region(path);
        }

    }
}
