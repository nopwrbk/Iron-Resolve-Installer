namespace IronResolve_Installer_V0._9._6
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            System.ComponentModel.ComponentResourceManager resources =
                new System.ComponentModel.ComponentResourceManager(typeof(Form1));

            picBackground = new PictureBox();
            guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            titleIronResolveLauncher = new Guna.UI2.WinForms.Guna2HtmlLabel();
            labeltitleServerVersion = new Guna.UI2.WinForms.Guna2HtmlLabel();
            labeltitleInstalledVersion = new Guna.UI2.WinForms.Guna2HtmlLabel();
            labeltitleStatus = new Guna.UI2.WinForms.Guna2HtmlLabel();
            lblServerVersion = new Guna.UI2.WinForms.Guna2HtmlLabel();
            lblInstalledVersion = new Guna.UI2.WinForms.Guna2HtmlLabel();
            lblStatus = new Guna.UI2.WinForms.Guna2HtmlLabel();
            guna2ProgressBar1 = new Guna.UI2.WinForms.Guna2ProgressBar();
            lblProgress = new Guna.UI2.WinForms.Guna2HtmlLabel();
            btnInstall = new Guna.UI2.WinForms.Guna2Button();
            btnRepair = new Guna.UI2.WinForms.Guna2Button();
            lblactivlog = new Guna.UI2.WinForms.Guna2HtmlLabel();
            rtbChangelog = new RichTextBox();
            rtbLog = new RichTextBox();

            ((System.ComponentModel.ISupportInitialize)picBackground).BeginInit();
            guna2Panel1.SuspendLayout();
            SuspendLayout();

            // picBackground
            picBackground.Dock = DockStyle.Fill;
            picBackground.Image = Properties.Resources._33fb201a_3e5d_4117_9c4b_307d1da8da01;
            picBackground.Location = new Point(0, 0);
            picBackground.Name = "picBackground";
            picBackground.Size = new Size(1100, 650);
            picBackground.SizeMode = PictureBoxSizeMode.StretchImage;
            picBackground.TabIndex = 0;
            picBackground.TabStop = false;

            // guna2Panel1
            guna2Panel1.BackColor = Color.Transparent;
            guna2Panel1.Controls.Add(titleIronResolveLauncher);
            guna2Panel1.CustomizableEdges = customizableEdges1;
            guna2Panel1.Dock = DockStyle.Top;
            guna2Panel1.FillColor = Color.Transparent;
            guna2Panel1.BorderThickness = 0;
            guna2Panel1.ShadowDecoration.Enabled = false;
            guna2Panel1.Location = new Point(0, 0);
            guna2Panel1.Name = "guna2Panel1";
            guna2Panel1.ShadowDecoration.CustomizableEdges = customizableEdges2;
            guna2Panel1.Size = new Size(1100, 88);
            guna2Panel1.TabIndex = 1;

            // titleIronResolveLauncher
            titleIronResolveLauncher.BackColor = Color.Transparent;
            titleIronResolveLauncher.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
            titleIronResolveLauncher.ForeColor = Color.White;
            titleIronResolveLauncher.Location = new Point(22, 24);
            titleIronResolveLauncher.Name = "titleIronResolveLauncher";
            titleIronResolveLauncher.Size = new Size(342, 39);
            titleIronResolveLauncher.TabIndex = 0;
            titleIronResolveLauncher.Text = "IRON RESOLVE LAUNCHER";

            // labeltitleServerVersion
            labeltitleServerVersion.BackColor = Color.Transparent;
            labeltitleServerVersion.ForeColor = Color.White;
            labeltitleServerVersion.Location = new Point(24, 100);
            labeltitleServerVersion.Name = "labeltitleServerVersion";
            labeltitleServerVersion.Size = new Size(86, 17);
            labeltitleServerVersion.TabIndex = 2;
            labeltitleServerVersion.Text = "Server Version :";

            // labeltitleInstalledVersion
            labeltitleInstalledVersion.BackColor = Color.Transparent;
            labeltitleInstalledVersion.ForeColor = Color.White;
            labeltitleInstalledVersion.Location = new Point(24, 126);
            labeltitleInstalledVersion.Name = "labeltitleInstalledVersion";
            labeltitleInstalledVersion.Size = new Size(95, 17);
            labeltitleInstalledVersion.TabIndex = 3;
            labeltitleInstalledVersion.Text = "Installed Version :";

            // labeltitleStatus
            labeltitleStatus.BackColor = Color.Transparent;
            labeltitleStatus.ForeColor = Color.White;
            labeltitleStatus.Location = new Point(24, 152);
            labeltitleStatus.Name = "labeltitleStatus";
            labeltitleStatus.Size = new Size(39, 17);
            labeltitleStatus.TabIndex = 4;
            labeltitleStatus.Text = "Status :";

            // lblServerVersion
            lblServerVersion.BackColor = Color.Transparent;
            lblServerVersion.ForeColor = Color.White;
            lblServerVersion.Location = new Point(160, 100);
            lblServerVersion.Name = "lblServerVersion";
            lblServerVersion.Size = new Size(27, 17);
            lblServerVersion.TabIndex = 5;
            lblServerVersion.Text = "0.9.7";

            // lblInstalledVersion
            lblInstalledVersion.BackColor = Color.Transparent;
            lblInstalledVersion.ForeColor = Color.White;
            lblInstalledVersion.Location = new Point(160, 126);
            lblInstalledVersion.Name = "lblInstalledVersion";
            lblInstalledVersion.Size = new Size(75, 17);
            lblInstalledVersion.TabIndex = 6;
            lblInstalledVersion.Text = "Not Installed";

            // lblStatus
            lblStatus.BackColor = Color.Transparent;
            lblStatus.ForeColor = Color.White;
            lblStatus.Location = new Point(160, 152);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(35, 17);
            lblStatus.TabIndex = 7;
            lblStatus.Text = "Ready";

            // guna2ProgressBar1
            guna2ProgressBar1.CustomizableEdges = customizableEdges3;
            guna2ProgressBar1.FillColor = Color.FromArgb(220, 225, 230);
            guna2ProgressBar1.Location = new Point(24, 200);
            guna2ProgressBar1.Name = "guna2ProgressBar1";
            guna2ProgressBar1.ProgressColor = Color.RoyalBlue;
            guna2ProgressBar1.ProgressColor2 = Color.CornflowerBlue;
            guna2ProgressBar1.ShadowDecoration.CustomizableEdges = customizableEdges4;
            guna2ProgressBar1.Size = new Size(1052, 24);
            guna2ProgressBar1.TabIndex = 8;
            guna2ProgressBar1.TextRenderingHint =
                System.Drawing.Text.TextRenderingHint.SystemDefault;
            guna2ProgressBar1.ValueChanged += guna2ProgressBar1_ValueChanged;

            // lblProgress
            lblProgress.BackColor = Color.Transparent;
            lblProgress.ForeColor = Color.White;
            lblProgress.Location = new Point(24, 230);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(19, 17);
            lblProgress.TabIndex = 9;
            lblProgress.Text = "0%";

            // btnInstall
            btnInstall.CustomizableEdges = customizableEdges5;
            btnInstall.DisabledState.BorderColor = Color.DarkGray;
            btnInstall.DisabledState.CustomBorderColor = Color.DarkGray;
            btnInstall.DisabledState.FillColor = Color.FromArgb(185, 185, 185);
            btnInstall.DisabledState.ForeColor = Color.FromArgb(130, 130, 130);
            btnInstall.FillColor = Color.FromArgb(72, 110, 230);
            btnInstall.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnInstall.ForeColor = Color.White;
            btnInstall.Location = new Point(24, 270);
            btnInstall.Name = "btnInstall";
            btnInstall.ShadowDecoration.CustomizableEdges = customizableEdges6;
            btnInstall.Size = new Size(230, 52);
            btnInstall.TabIndex = 10;
            btnInstall.Text = "INSTALL / UPDATE";
            btnInstall.Click += btnInstall_Click;

            // btnRepair
            btnRepair.CustomizableEdges = customizableEdges7;
            btnRepair.DisabledState.BorderColor = Color.DarkGray;
            btnRepair.DisabledState.CustomBorderColor = Color.DarkGray;
            btnRepair.DisabledState.FillColor = Color.FromArgb(185, 185, 185);
            btnRepair.DisabledState.ForeColor = Color.FromArgb(130, 130, 130);
            btnRepair.FillColor = Color.FromArgb(72, 110, 230);
            btnRepair.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnRepair.ForeColor = Color.White;
            btnRepair.Location = new Point(266, 270);
            btnRepair.Name = "btnRepair";
            btnRepair.ShadowDecoration.CustomizableEdges = customizableEdges8;
            btnRepair.Size = new Size(230, 52);
            btnRepair.TabIndex = 11;
            btnRepair.Text = "REPAIR INSTALLATION";
            btnRepair.Click += btnRepair_Click;

            // lblactivlog
            lblactivlog.BackColor = Color.Transparent;
            lblactivlog.ForeColor = Color.White;
            lblactivlog.Location = new Point(24, 350);
            lblactivlog.Name = "lblactivlog";
            lblactivlog.Size = new Size(79, 17);
            lblactivlog.TabIndex = 12;
            lblactivlog.Text = "ACTIVITY LOG";

            // rtbChangelog
            rtbChangelog.BackColor = Color.FromArgb(18, 18, 18);
            rtbChangelog.BorderStyle = BorderStyle.None;
            rtbChangelog.Font = new Font("Consolas", 10F);
            rtbChangelog.ForeColor = Color.WhiteSmoke;
            rtbChangelog.Location = new Point(24, 385);
            rtbChangelog.Name = "rtbChangelog";
            rtbChangelog.ReadOnly = true;
            rtbChangelog.Size = new Size(700, 150);
            rtbChangelog.TabIndex = 13;
            rtbChangelog.Text = "Loading update notes...";

            // rtbLog
            rtbLog.BackColor = Color.FromArgb(18, 18, 18);
            rtbLog.BorderStyle = BorderStyle.None;
            rtbLog.Font = new Font("Consolas", 10F);
            rtbLog.ForeColor = Color.WhiteSmoke;
            rtbLog.Location = new Point(24, 555);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(800, 70);
            rtbLog.TabIndex = 14;
            rtbLog.Text = "";
            rtbLog.TextChanged += rtbLog_TextChanged;
            // Form1
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(18, 18, 18);
            ClientSize = new Size(1100, 650);
            Controls.Add(guna2Panel1);
            Controls.Add(labeltitleServerVersion);
            Controls.Add(labeltitleInstalledVersion);
            Controls.Add(labeltitleStatus);
            Controls.Add(lblServerVersion);
            Controls.Add(lblInstalledVersion);
            Controls.Add(lblStatus);
            Controls.Add(guna2ProgressBar1);
            Controls.Add(lblProgress);
            Controls.Add(btnInstall);
            Controls.Add(btnRepair);
            Controls.Add(lblactivlog);
            Controls.Add(rtbChangelog);
            Controls.Add(rtbLog);
            Controls.Add(picBackground);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimumSize = new Size(1116, 689);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Iron Resolve Installer";

            ((System.ComponentModel.ISupportInitialize)picBackground).EndInit();
            guna2Panel1.ResumeLayout(false);
            guna2Panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox picBackground;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        private Guna.UI2.WinForms.Guna2HtmlLabel titleIronResolveLauncher;
        private Guna.UI2.WinForms.Guna2HtmlLabel labeltitleServerVersion;
        private Guna.UI2.WinForms.Guna2HtmlLabel labeltitleInstalledVersion;
        private Guna.UI2.WinForms.Guna2HtmlLabel labeltitleStatus;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblServerVersion;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblInstalledVersion;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblStatus;
        private Guna.UI2.WinForms.Guna2ProgressBar guna2ProgressBar1;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblProgress;
        private Guna.UI2.WinForms.Guna2Button btnInstall;
        private Guna.UI2.WinForms.Guna2Button btnRepair;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblactivlog;
        private RichTextBox rtbChangelog;
        private RichTextBox rtbLog;
    }
}