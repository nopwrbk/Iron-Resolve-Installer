using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IronResolve_Installer_V0._9._6;

public partial class Form1 : Form
{
    private const string ManifestUrl =
        "https://pub-af2a69b4b5e04b7dbcf11251bf55cdba.r2.dev/package-manifest.json";

    // Replace these values with your public GitHub repository.
    private const string GitHubOwner = "nopwrbk";
    private const string GitHubRepository = "Iron-Resolve-Installer";


    private readonly HttpClient httpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    private readonly string addonsFolder;
    private readonly string packageStateFile;
    private readonly PackageUpdateService packageUpdateService;
    private readonly LauncherUpdateService launcherUpdateService;

    private readonly System.Windows.Forms.Timer fadeTimer = new();
    private readonly System.Windows.Forms.Timer progressTimer = new();

    // Created in code so Form1.Designer.cs does not need to be edited.
    private readonly Button btnPlay = new();
    private readonly Label lblLauncherVersion = new();
    private readonly Label lblDownloadSpeed = new();
    private readonly GlassTextPanel glassChangelog = new();
    private readonly GlassTextPanel glassLog = new();

    private PackageManifest? onlineManifest;
    private List<PackageEntry> packagesToDownload = new();
    private string serverVersion = "Unknown";
    private int targetProgress;
    private bool isBusy;
    private bool isClosing;

    public Form1()
    {
        InitializeComponent();
        ConfigureDarkBackground();
        ConfigureGlassInterface();
        ConfigurePlayButton();
        ConfigureLauncherVersion();
        ConfigureDownloadSpeedLabel();

        addonsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "My Games",
            "ArmaReforger",
            "addons");

        packageStateFile = Path.Combine(
            addonsFolder,
            "IronResolve.packages.json");

        packageUpdateService = new PackageUpdateService(
            httpClient,
            addonsFolder,
            packageStateFile);

        launcherUpdateService = new LauncherUpdateService(
            httpClient,
            GitHubOwner,
            GitHubRepository,
            "IronResolveLauncherSetup.exe",
            "IronResolveLauncherSetup.exe.sha256");

        Opacity = 0;

        fadeTimer.Interval = 15;
        fadeTimer.Tick += FadeTimer_Tick;

        progressTimer.Interval = 15;
        progressTimer.Tick += ProgressTimer_Tick;

        Shown += Form1_Shown;
        FormClosed += Form1_FormClosed;
    }
    private void ConfigureLauncherVersion()
    {
        lblLauncherVersion.Name = "lblLauncherVersion";

        Version launcherVersion =
            LauncherUpdateService.GetCurrentLauncherVersion();

        lblLauncherVersion.Text =
            $"v{launcherVersion.Major}." +
            $"{launcherVersion.Minor}." +
            $"{Math.Max(0, launcherVersion.Build)}" +
            "   © 2026 Iron Resolve";

        lblLauncherVersion.AutoSize = false;
        lblLauncherVersion.Size = new Size(260, 18);

        lblLauncherVersion.Font =
            new Font("Segoe UI", 8F, FontStyle.Regular);

        lblLauncherVersion.ForeColor =
            Color.FromArgb(155, 155, 155);

        lblLauncherVersion.BackColor = Color.Transparent;

        // Attach to the wallpaper so transparency works
        lblLauncherVersion.Parent = picBackground;

        lblLauncherVersion.TextAlign = ContentAlignment.MiddleRight;

        lblLauncherVersion.Anchor =
            AnchorStyles.Bottom | AnchorStyles.Right;

        lblLauncherVersion.Location = new Point(
            ClientSize.Width - lblLauncherVersion.Width - 22,
            ClientSize.Height - lblLauncherVersion.Height - 6);

        lblLauncherVersion.BringToFront();
    }
    private void ConfigureDownloadSpeedLabel()
    {
        lblDownloadSpeed.Name = "lblDownloadSpeed";
        lblDownloadSpeed.Text = "";
        lblDownloadSpeed.AutoSize = true;
        lblDownloadSpeed.Font =
            new Font("Segoe UI", 9F, FontStyle.Regular);
        lblDownloadSpeed.ForeColor = Color.LightSkyBlue;
        lblDownloadSpeed.BackColor = Color.Transparent;
        lblDownloadSpeed.Parent = picBackground;

        lblDownloadSpeed.Location = new Point(
            lblProgress.Right + 11,
            lblProgress.Top);

        lblDownloadSpeed.BringToFront();
    }

    private void ConfigureDarkBackground()
    {
        picBackground.Dock = DockStyle.Fill;
        picBackground.SizeMode = PictureBoxSizeMode.StretchImage;
        picBackground.SendToBack();

        if (picBackground.Image is null)
        {
            return;
        }

        Image original = picBackground.Image;

        var darkened = new Bitmap(
            original.Width,
            original.Height,
            PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(darkened))
        using (var attributes = new ImageAttributes())
        {
            // 0.40 keeps about 40% of the original brightness.
            const float brightness = 0.40f;

            var matrix = new ColorMatrix(new[]
            {
                new[] { brightness, 0f,         0f,         0f, 0f },
                new[] { 0f,         brightness, 0f,         0f, 0f },
                new[] { 0f,         0f,         brightness, 0f, 0f },
                new[] { 0f,         0f,         0f,         1f, 0f },
                new[] { 0f,         0f,         0f,         0f, 1f }
            });

            attributes.SetColorMatrix(matrix);

            graphics.DrawImage(
                original,
                new Rectangle(0, 0, darkened.Width, darkened.Height),
                0,
                0,
                original.Width,
                original.Height,
                GraphicsUnit.Pixel,
                attributes);
        }

        picBackground.Image = darkened;

        foreach (Control control in Controls)
        {
            if (!ReferenceEquals(control, picBackground))
            {
                control.BringToFront();
            }
        }
    }

    private void ConfigureGlassInterface()
    {
        // Move labels onto the wallpaper so Color.Transparent works correctly.
        MoveWallpaperLabelsToBackground(this);

        // Make the title header transparent as well.
        Control? title = FindControlByText(this, "IRON RESOLVE LAUNCHER");

        if (title is not null)
        {
            Control? titleContainer = title.Parent;

            if (titleContainer is not null &&
                !ReferenceEquals(titleContainer, this) &&
                !ReferenceEquals(titleContainer, picBackground))
            {
                Point containerScreen =
                    titleContainer.Parent!.PointToScreen(titleContainer.Location);

                Point containerLocation =
                    picBackground.PointToClient(containerScreen);

                titleContainer.Parent = picBackground;
                titleContainer.Location = containerLocation;
                titleContainer.BackColor = Color.Transparent;
                titleContainer.BringToFront();
            }
            else
            {
                MoveControlToBackground(title);
            }

            title.BackColor = Color.Transparent;
            title.ForeColor = Color.White;
            title.BringToFront();
        }

        ConfigureGlassPanel(
            glassChangelog,
            rtbChangelog,
            showLastLines: false);

        ConfigureGlassPanel(
            glassLog,
            rtbLog,
            showLastLines: true);

        // Keep the original RichTextBox controls as hidden text stores.
        rtbChangelog.Visible = false;
        rtbLog.Visible = false;

        RefreshGlassText();
    }

    private void ConfigureGlassPanel(
        GlassTextPanel glassPanel,
        RichTextBox source,
        bool showLastLines)
    {
        Point screenPoint =
            source.Parent!.PointToScreen(source.Location);

        Point newLocation =
            picBackground.PointToClient(screenPoint);

        glassPanel.Location = newLocation;
        glassPanel.Size = source.Size;
        glassPanel.Anchor = source.Anchor;
        glassPanel.Font = source.Font;
        glassPanel.ForeColor = Color.WhiteSmoke;
        glassPanel.SetShowLastLines(showLastLines);
        glassPanel.SetGlassAlpha(155);
        glassPanel.SetCornerRadius(12);
        glassPanel.Padding = new Padding(12, 10, 12, 10);

        picBackground.Controls.Add(glassPanel);
        glassPanel.BringToFront();
    }

    private void RefreshGlassText()
    {
        if (isClosing || IsDisposed || Disposing)
        {
            return;
        }

        if (!rtbChangelog.IsDisposed)
        {
            glassChangelog.Text = rtbChangelog.Text;
        }

        if (!rtbLog.IsDisposed)
        {
            glassLog.Text = rtbLog.Text;
        }

        if (!glassChangelog.IsDisposed)
        {
            glassChangelog.Invalidate();
        }

        if (!glassLog.IsDisposed)
        {
            glassLog.Invalidate();
        }
    }

    private void MoveWallpaperLabelsToBackground(Control container)
    {
        Control[] controls = container.Controls
            .Cast<Control>()
            .ToArray();

        foreach (Control control in controls)
        {
            bool isLabel =
                control is Label ||
                control.GetType().Name == "Guna2HtmlLabel";

            if (isLabel)
            {
                Point formPoint = PointToClient(
                    control.Parent!.PointToScreen(control.Location));

                // Everything below the Windows title bar belongs over the wallpaper.
                if (formPoint.Y >= 35)
                {
                    MoveControlToBackground(control);
                    control.BackColor = Color.Transparent;
                    control.ForeColor = Color.White;
                    control.BringToFront();
                    continue;
                }
            }

            if (control.HasChildren &&
                !ReferenceEquals(control, picBackground))
            {
                MoveWallpaperLabelsToBackground(control);
            }
        }

        // Force all changing values and progress text.
        MoveControlToBackground(lblServerVersion);
        MoveControlToBackground(lblInstalledVersion);
        MoveControlToBackground(lblStatus);
        MoveControlToBackground(lblProgress);

        lblServerVersion.BackColor = Color.Transparent;
        lblInstalledVersion.BackColor = Color.Transparent;
        lblStatus.BackColor = Color.Transparent;
        lblProgress.BackColor = Color.Transparent;
    }

    private void MoveControlToBackground(Control control)
    {
        if (ReferenceEquals(control.Parent, picBackground))
        {
            return;
        }

        Point screenPoint =
            control.Parent!.PointToScreen(control.Location);

        Point newLocation =
            picBackground.PointToClient(screenPoint);

        control.Parent = picBackground;
        control.Location = newLocation;
    }

    private static Control? FindControlByText(
        Control parent,
        string text)
    {
        foreach (Control control in parent.Controls)
        {
            if (string.Equals(
                    control.Text?.Trim(),
                    text,
                    StringComparison.OrdinalIgnoreCase))
            {
                return control;
            }

            Control? nested =
                FindControlByText(control, text);

            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }


    private void ConfigurePlayButton()
    {
        btnPlay.Name = "btnPlay";
        btnPlay.Text = "PLAY";
        btnPlay.Size = new Size(210, 48);
        btnPlay.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnPlay.Location = new Point(
            ClientSize.Width - btnPlay.Width - 28,
            ClientSize.Height - btnPlay.Height - 28);

        btnPlay.BackColor = Color.FromArgb(46, 160, 90);
        btnPlay.ForeColor = Color.White;
        btnPlay.FlatStyle = FlatStyle.Flat;
        btnPlay.FlatAppearance.BorderSize = 0;
        btnPlay.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnPlay.Cursor = Cursors.Hand;
        btnPlay.Enabled = false;
        btnPlay.Click += BtnPlay_Click;

        Controls.Add(btnPlay);
        btnPlay.BringToFront();
    }

    private void BtnPlay_Click(object? sender, EventArgs e)
    {
        if (isBusy)
        {
            return;
        }

        if (onlineManifest is null || packagesToDownload.Count > 0)
        {
            MessageBox.Show(
                "Install or repair the modpack before launching the game.",
                "Update required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        try
        {
            // Official Steam application ID for Arma Reforger: 1874880.
            Process.Start(new ProcessStartInfo
            {
                FileName = "steam://run/1874880",
                UseShellExecute = true
            });

            WriteLog("Starting Arma Reforger through Steam...");
        }
        catch (Exception ex)
        {
            WriteLog($"Could not start Arma Reforger: {ex.Message}");

            MessageBox.Show(
                "The launcher could not open Arma Reforger through Steam.\n\n" +
                "Make sure Steam and Arma Reforger are installed.",
                "Could not launch game",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void Form1_Shown(
        object? sender,
        EventArgs e)
    {
        fadeTimer.Start();

        bool updaterStarted =
            await TryInstallLauncherUpdateAsync();

        if (updaterStarted)
        {
            return;
        }

        await CheckInstallationAsync();
    }

    private async Task<bool> TryInstallLauncherUpdateAsync()
    {
        if (GitHubOwner == "CHANGE_ME" ||
            GitHubRepository == "CHANGE_ME")
        {
            WriteLog(
                "Launcher update check skipped: configure GitHubOwner " +
                "and GitHubRepository in Form1.cs.");

            return false;
        }

        try
        {
            SetStatus(
                "Checking launcher update...",
                Color.LightSkyBlue);

            LauncherReleaseInfo? release =
                await launcherUpdateService.CheckForUpdateAsync(
                    CancellationToken.None);

            if (release is null)
            {
                WriteLog("Launcher is up to date.");
                return false;
            }

            DialogResult answer = MessageBox.Show(
                $"Iron Resolve Launcher {release.Version} is available." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "Install the launcher update now?" +
                (string.IsNullOrWhiteSpace(release.ReleaseNotes)
                    ? ""
                    : $"{Environment.NewLine}{Environment.NewLine}" +
                      release.ReleaseNotes),
                "Launcher update available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (answer != DialogResult.Yes)
            {
                WriteLog(
                    $"Launcher update {release.Version} postponed.");

                return false;
            }

            SetBusy(true);
            SetProgress(0);
            lblDownloadSpeed.Text = "";

            WriteLog(
                $"Downloading launcher update {release.Version}...");

            await launcherUpdateService.DownloadAndLaunchInstallerAsync(
                release,
                progress =>
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(() =>
                        {
                            SetProgress(progress.Percentage);
                            lblDownloadSpeed.Text = progress.StatusText;
                        });
                    }
                    else
                    {
                        SetProgress(progress.Percentage);
                        lblDownloadSpeed.Text = progress.StatusText;
                    }
                },
                CancellationToken.None);

            WriteLog(
                "Launcher installer started. Closing this version...");

            isClosing = true;
            BeginInvoke(Close);
            return true;
        }
        catch (Exception ex)
        {
            WriteLog(
                $"Launcher update check failed: {ex.Message}");

            SetStatus(
                "Launcher update check failed",
                Color.Orange);

            return false;
        }
        finally
        {
            if (!isClosing)
            {
                SetBusy(false);
                lblDownloadSpeed.Text = "";
            }
        }
    }

    private void FadeTimer_Tick(
        object? sender,
        EventArgs e)
    {
        Opacity = Math.Min(
            1.0,
            Opacity + 0.06);

        if (Opacity >= 1.0)
        {
            Opacity = 1.0;
            fadeTimer.Stop();
        }
    }

    private void ProgressTimer_Tick(
        object? sender,
        EventArgs e)
    {
        int current =
            guna2ProgressBar1.Value;

        if (current == targetProgress)
        {
            progressTimer.Stop();
            return;
        }

        int difference =
            Math.Abs(
                targetProgress - current);

        int step =
            Math.Max(1, difference / 8);

        current = current < targetProgress
            ? Math.Min(
                current + step,
                targetProgress)
            : Math.Max(
                current - step,
                targetProgress);

        guna2ProgressBar1.Value = current;
        lblProgress.Text = $"{current}%";

        lblDownloadSpeed.Location = new Point(
            lblProgress.Right + 10,
            lblProgress.Top);
    }

    private async Task CheckInstallationAsync()
    {
        SetBusy(true);
        SetProgress(0);
        lblDownloadSpeed.Text = "";

        WriteLog(
            "Downloading online package manifest...");

        SetStatus(
            "Checking manifest...",
            Color.LightSkyBlue);

        try
        {
            onlineManifest =
                await packageUpdateService
                    .DownloadManifestAsync(
                        ManifestUrl,
                        CancellationToken.None);

            serverVersion =
                onlineManifest.Version;

            lblServerVersion.Text =
                serverVersion;

            lblInstalledVersion.Text =
                GetInstalledVersion();

            rtbChangelog.Text =
                $"IRON RESOLVE {onlineManifest.Version}" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Packages: {onlineManifest.PackageCount:N0}" +
                $"{Environment.NewLine}" +
                $"Full install size: " +
                $"{FormatBytes(onlineManifest.TotalSize)}";

            RefreshGlassText();

            SetStatus(
                "Checking installed packages...",
                Color.LightSkyBlue);

            WriteLog(
                $"Manifest version: " +
                $"{onlineManifest.Version}");

            packagesToDownload =
                await packageUpdateService
                    .FindRequiredPackagesAsync(
                        onlineManifest,
                        forceRepair: false,
                        WriteLog,
                        CancellationToken.None);

            long downloadSize =
                packagesToDownload
                    .Sum(package =>
                        package.Size);

            if (packagesToDownload.Count == 0)
            {
                SetStatus(
                    "Up To Date",
                    Color.LimeGreen);

                btnInstall.Text = "INSTALLED";
                btnInstall.Enabled = false;
                btnRepair.Enabled = true;
                btnPlay.Enabled = true;

                SetProgress(100);

                WriteLog(
                    "All required packages are installed.");
            }
            else
            {
                bool firstInstall =
                    GetInstalledVersion() ==
                    "Not Installed";

                SetStatus(
                    firstInstall
                        ? "Installation Required"
                        : "Update Available",
                    Color.Orange);

                btnInstall.Text = firstInstall
                    ? "INSTALL MODPACK"
                    : "INSTALL UPDATE";

                btnInstall.Enabled = true;
                btnRepair.Enabled = true;
                btnPlay.Enabled = false;

                SetProgress(0);

                WriteLog(
                    $"{packagesToDownload.Count:N0} " +
                    $"package(s) need downloading " +
                    $"({FormatBytes(downloadSize)}).");

                rtbChangelog.AppendText(
                    $"{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                    $"Required download: " +
                    $"{FormatBytes(downloadSize)}" +
                    $"{Environment.NewLine}" +
                    $"Packages needed: " +
                    $"{packagesToDownload.Count:N0}");

                RefreshGlassText();
            }
        }
        catch (Exception ex)
        {
            serverVersion = "Unknown";
            onlineManifest = null;
            packagesToDownload.Clear();

            lblServerVersion.Text =
                "Unavailable";

            lblInstalledVersion.Text =
                GetInstalledVersion();

            SetStatus(
                "Connection Error",
                Color.OrangeRed);

            btnInstall.Enabled = false;
            btnRepair.Enabled = false;
            btnPlay.Enabled = false;

            WriteLog(
                $"Could not check installation: " +
                $"{ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void btnInstall_Click(
        object sender,
        EventArgs e)
    {
        await InstallRequiredPackagesAsync(
            forceRepair: false);
    }

    private async void btnRepair_Click(
        object sender,
        EventArgs e)
    {
        DialogResult answer =
            MessageBox.Show(
                "Repair will redownload and reinstall " +
                "every managed mod package. Continue?",
                "Repair installation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        WriteLog(
            "Full package repair requested.");

        await InstallRequiredPackagesAsync(
            forceRepair: true);
    }

    private async Task InstallRequiredPackagesAsync(
        bool forceRepair)
    {
        if (onlineManifest is null)
        {
            await CheckInstallationAsync();

            if (onlineManifest is null)
            {
                return;
            }
        }

        SetBusy(true);

        try
        {
            Directory.CreateDirectory(
                addonsFolder);

            packagesToDownload =
                await packageUpdateService
                    .FindRequiredPackagesAsync(
                        onlineManifest,
                        forceRepair,
                        WriteLog,
                        CancellationToken.None);

            if (packagesToDownload.Count == 0)
            {
                SetStatus(
                    "Up To Date",
                    Color.LimeGreen);

                SetProgress(100);
                lblDownloadSpeed.Text = "";

                WriteLog(
                    "No package repair was required.");

                return;
            }

            WriteLog(
                $"Downloading " +
                $"{packagesToDownload.Count:N0} " +
                $"package(s), " +
                $"{FormatBytes(packagesToDownload.Sum(x => x.Size))} " +
                $"total. Parallel package downloads are enabled.");

            await packageUpdateService
                .InstallPackagesAsync(
                    onlineManifest,
                    packagesToDownload,
                    progress =>
                    {
                        if (InvokeRequired)
                        {
                            BeginInvoke(() =>
                                ApplyDownloadProgress(
                                    progress));
                        }
                        else
                        {
                            ApplyDownloadProgress(
                                progress);
                        }
                    },
                    WriteLog,
                    CancellationToken.None);

            packagesToDownload.Clear();

            lblInstalledVersion.Text =
                onlineManifest.Version;

            SetStatus(
                "Up To Date",
                Color.LimeGreen);

            btnInstall.Text = "INSTALLED";
            btnInstall.Enabled = false;
            btnRepair.Enabled = true;
            btnPlay.Enabled = true;

            SetProgress(100);
            lblDownloadSpeed.Text = "";

            WriteLog(
                "Package installation completed successfully.");

            MessageBox.Show(
                "The Iron Resolve modpack is " +
                "installed and verified.",
                "Installation complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus(
                "Installation Failed",
                Color.Red);

            btnInstall.Text = "TRY AGAIN";
            btnInstall.Enabled = true;
            btnRepair.Enabled = true;
            btnPlay.Enabled = false;

            lblDownloadSpeed.Text = "";

            WriteLog(
                $"Installation failed: " +
                $"{ex.Message}");

            MessageBox.Show(
                ex.Message,
                "Installation failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ApplyDownloadProgress(
        DownloadUiProgress progress)
    {
        SetProgress(progress.Percentage);

        lblDownloadSpeed.Text =
            progress.SpeedText;

        lblDownloadSpeed.Location =
            new Point(
                lblProgress.Right + 10,
                lblProgress.Top);

        lblStatus.Text =
            progress.StatusText;

        lblStatus.ForeColor =
            Color.DeepSkyBlue;
    }

    private string GetInstalledVersion()
    {
        InstalledPackageState state =
            packageUpdateService.LoadState();

        return string.IsNullOrWhiteSpace(
                state.Version)
            ? "Not Installed"
            : state.Version;
    }

    private void SetStatus(
        string text,
        Color color)
    {
        lblStatus.Text = text;
        lblStatus.ForeColor = color;
    }

    private void SetBusy(bool busy)
    {
        isBusy = busy;
        UseWaitCursor = busy;

        if (busy)
        {
            btnInstall.Enabled = false;
            btnRepair.Enabled = false;
            btnPlay.Enabled = false;
        }
        else
        {
            btnRepair.Enabled =
                onlineManifest is not null;

            btnPlay.Enabled =
                onlineManifest is not null &&
                packagesToDownload.Count == 0;

            if (onlineManifest is not null &&
                packagesToDownload.Count > 0)
            {
                btnInstall.Enabled = true;
            }
        }
    }

    private void SetProgress(int value)
    {
        targetProgress =
            Math.Clamp(value, 0, 100);

        if (!progressTimer.Enabled)
        {
            progressTimer.Start();
        }
    }

    private void WriteLog(string message)
    {
        if (isClosing ||
            IsDisposed ||
            Disposing ||
            rtbLog.IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(() =>
                    WriteLog(message));
            }
            catch
            {
                // The form is closing.
            }

            return;
        }

        string line =
            $"[{DateTime.Now:HH:mm:ss}] " +
            $"{message}";

        rtbLog.AppendText(
            line + Environment.NewLine);

        rtbLog.SelectionStart =
            rtbLog.TextLength;

        rtbLog.ScrollToCaret();

        RefreshGlassText();
    }

    private static string FormatBytes(long bytes)
    {
        string[] units =
            { "B", "KB", "MB", "GB", "TB" };

        double value =
            Math.Max(0, bytes);

        int unit = 0;

        while (value >= 1024 &&
               unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }

    private void Form1_FormClosed(
        object? sender,
        FormClosedEventArgs e)
    {
        isClosing = true;

        fadeTimer.Stop();
        progressTimer.Stop();

        fadeTimer.Dispose();
        progressTimer.Dispose();
        httpClient.Dispose();
    }

    private void guna2ProgressBar1_ValueChanged(
        object sender,
        EventArgs e)
    {
    }

    private void guna2HtmlLabel2_Click(
        object sender,
        EventArgs e)
    {
    }

    private void guna2HtmlLabel2_Click_1(
        object sender,
        EventArgs e)
    {
    }

    private void rtbLog_TextChanged(
        object sender,
        EventArgs e)
    {
    }

    private void guna2PictureBox1_Click(
        object sender,
        EventArgs e)
    {
    }

    private sealed class GlassTextPanel : Control
    {
        private int glassAlpha = 155;
        private int cornerRadius = 12;
        private bool showLastLines;

        public GlassTextPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Font = new Font("Consolas", 9F);
        }

        public void SetGlassAlpha(int value)
        {
            glassAlpha = Math.Clamp(value, 0, 255);
            Invalidate();
        }

        public void SetCornerRadius(int value)
        {
            cornerRadius = Math.Max(0, value);
            Invalidate();
        }

        public void SetShowLastLines(bool value)
        {
            showLastLines = value;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode =
                System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle area = new(
                0,
                0,
                Math.Max(1, Width - 1),
                Math.Max(1, Height - 1));

            using var path =
                CreateRoundedRectangle(area, cornerRadius);

            using var backgroundBrush =
                new SolidBrush(Color.FromArgb(
                    glassAlpha,
                    8,
                    8,
                    10));

            using var borderPen =
                new Pen(Color.FromArgb(105, 180, 180, 185), 1F);

            e.Graphics.FillPath(backgroundBrush, path);
            e.Graphics.DrawPath(borderPen, path);

            Rectangle textArea = new(
                Padding.Left,
                Padding.Top,
                Math.Max(1, Width - Padding.Horizontal),
                Math.Max(1, Height - Padding.Vertical));

            string displayText = Text ?? string.Empty;

            if (showLastLines)
            {
                displayText = GetLastVisibleLines(
                    e.Graphics,
                    displayText,
                    textArea.Height);
            }

            TextRenderer.DrawText(
                e.Graphics,
                displayText,
                Font,
                textArea,
                ForeColor,
                TextFormatFlags.Left |
                TextFormatFlags.Top |
                TextFormatFlags.NoPadding |
                TextFormatFlags.WordBreak);
        }

        private string GetLastVisibleLines(
            Graphics graphics,
            string text,
            int availableHeight)
        {
            string[] lines = text
                .Replace("\r\n", "\n")
                .Split('\n');

            int lineHeight =
                TextRenderer.MeasureText(
                    graphics,
                    "Ag",
                    Font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding).Height;

            int lineCount =
                Math.Max(1, availableHeight / Math.Max(1, lineHeight));

            return string.Join(
                Environment.NewLine,
                lines.TakeLast(lineCount));
        }

        private static System.Drawing.Drawing2D.GraphicsPath
            CreateRoundedRectangle(
                Rectangle bounds,
                int radius)
        {
            int diameter =
                Math.Max(1, Math.Min(radius * 2, Math.Min(
                    bounds.Width,
                    bounds.Height)));

            var path =
                new System.Drawing.Drawing2D.GraphicsPath();

            var arc = new Rectangle(
                bounds.Location,
                new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);

            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }



}