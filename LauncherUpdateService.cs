using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace IronResolve_Installer_V0._9._6;

public sealed class LauncherUpdateService
{
    private const string GitHubApiVersion = "2022-11-28";

    private readonly HttpClient httpClient;
    private readonly string latestReleaseApiUrl;
    private readonly string installerAssetName;
    private readonly string checksumAssetName;

    public LauncherUpdateService(
        HttpClient httpClient,
        string owner,
        string repository,
        string installerAssetName,
        string checksumAssetName)
    {
        this.httpClient = httpClient;
        this.installerAssetName = installerAssetName;
        this.checksumAssetName = checksumAssetName;

        latestReleaseApiUrl =
            $"https://api.github.com/repos/" +
            $"{Uri.EscapeDataString(owner)}/" +
            $"{Uri.EscapeDataString(repository)}/releases/latest";
    }

    public static Version GetCurrentLauncherVersion()
    {
        Version? version =
            Assembly.GetExecutingAssembly()
                .GetName()
                .Version;

        return version ?? new Version(2, 0, 0, 0);
    }

    public async Task<LauncherReleaseInfo?> CheckForUpdateAsync(
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                latestReleaseApiUrl);

        request.Headers.UserAgent.ParseAdd(
            "IronResolveLauncher/2.0.0");

        request.Headers.Accept.ParseAdd(
            "application/vnd.github+json");

        request.Headers.TryAddWithoutValidation(
            "X-GitHub-Api-Version",
            GitHubApiVersion);

        using HttpResponseMessage response =
            await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using Stream jsonStream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken)
            .ConfigureAwait(false);

        GitHubReleaseResponse? release =
            await JsonSerializer.DeserializeAsync<GitHubReleaseResponse>(
                jsonStream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (release is null ||
            release.Draft ||
            release.Prerelease)
        {
            return null;
        }

        Version onlineVersion =
            ParseReleaseVersion(release.TagName);

        Version currentVersion =
            GetCurrentLauncherVersion();

        if (onlineVersion <= currentVersion)
        {
            return null;
        }

        GitHubReleaseAsset installer =
            release.Assets.FirstOrDefault(asset =>
                string.Equals(
                    asset.Name,
                    installerAssetName,
                    StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException(
                $"GitHub release {release.TagName} does not contain " +
                $"{installerAssetName}.");

        GitHubReleaseAsset checksum =
            release.Assets.FirstOrDefault(asset =>
                string.Equals(
                    asset.Name,
                    checksumAssetName,
                    StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException(
                $"GitHub release {release.TagName} does not contain " +
                $"{checksumAssetName}.");

        return new LauncherReleaseInfo(
            onlineVersion,
            release.TagName,
            release.Body ?? "",
            installer.BrowserDownloadUrl,
            installer.Size,
            checksum.BrowserDownloadUrl);
    }

    public async Task DownloadAndLaunchInstallerAsync(
        LauncherReleaseInfo release,
        Action<LauncherUpdateProgress>? reportProgress,
        CancellationToken cancellationToken)
    {
        string updateFolder = Path.Combine(
            Path.GetTempPath(),
            "IronResolveLauncherUpdate");

        Directory.CreateDirectory(updateFolder);

        string installerPath = Path.Combine(
            updateFolder,
            installerAssetName);

        string expectedHash =
            await DownloadExpectedHashAsync(
                release.ChecksumUrl,
                cancellationToken)
            .ConfigureAwait(false);

        await DownloadInstallerAsync(
            release.InstallerUrl,
            installerPath,
            release.InstallerSize,
            reportProgress,
            cancellationToken)
            .ConfigureAwait(false);

        reportProgress?.Invoke(
            new LauncherUpdateProgress(
                100,
                "Verifying update..."));

        string actualHash =
            await CalculateSha256Async(
                installerPath,
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.Equals(
                actualHash,
                expectedHash,
                StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                File.Delete(installerPath);
            }
            catch
            {
            }

            throw new InvalidDataException(
                "The downloaded launcher installer failed SHA-256 verification.");
        }

        reportProgress?.Invoke(
            new LauncherUpdateProgress(
                100,
                "Starting installer..."));

        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments =
                "/VERYSILENT " +
                "/SUPPRESSMSGBOXES " +
                "/NORESTART " +
                "/CLOSEAPPLICATIONS",
            UseShellExecute = true,
            WorkingDirectory = updateFolder
        });
    }

    private async Task DownloadInstallerAsync(
        string url,
        string destinationPath,
        long expectedSize,
        Action<LauncherUpdateProgress>? reportProgress,
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.UserAgent.ParseAdd(
            "IronResolveLauncher/2.0.0");

        using HttpResponseMessage response =
            await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        long totalBytes =
            response.Content.Headers.ContentLength
            ?? expectedSize;

        await using Stream input =
            await response.Content.ReadAsStreamAsync(
                cancellationToken)
            .ConfigureAwait(false);

        await using FileStream output = new(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            1024 * 1024,
            useAsync: true);

        byte[] buffer = new byte[1024 * 1024];
        long downloaded = 0;
        long lastReportTicks = 0;

        while (true)
        {
            int read = await input.ReadAsync(
                buffer,
                cancellationToken)
                .ConfigureAwait(false);

            if (read == 0)
            {
                break;
            }

            await output.WriteAsync(
                buffer.AsMemory(0, read),
                cancellationToken)
                .ConfigureAwait(false);

            downloaded += read;

            long now = Environment.TickCount64;

            if (now - lastReportTicks >= 200)
            {
                lastReportTicks = now;

                int percentage = totalBytes <= 0
                    ? 0
                    : (int)Math.Clamp(
                        downloaded * 100L / totalBytes,
                        0,
                        100);

                reportProgress?.Invoke(
                    new LauncherUpdateProgress(
                        percentage,
                        "Downloading launcher update..."));
            }
        }

        await output.FlushAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<string> DownloadExpectedHashAsync(
        string checksumUrl,
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                checksumUrl);

        request.Headers.UserAgent.ParseAdd(
            "IronResolveLauncher/2.0.0");

        using HttpResponseMessage response =
            await httpClient.SendAsync(
                request,
                cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        string checksumText =
            await response.Content.ReadAsStringAsync(
                cancellationToken)
            .ConfigureAwait(false);

        string hash = checksumText
            .Trim()
            .Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "";

        if (hash.Length != 64 ||
            !hash.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException(
                "The GitHub SHA-256 checksum file is invalid.");
        }

        return hash.ToLowerInvariant();
    }

    private static async Task<string> CalculateSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 1024,
            useAsync: true);

        byte[] hash =
            await SHA256.HashDataAsync(
                stream,
                cancellationToken);

        return Convert.ToHexString(hash)
            .ToLowerInvariant();
    }

    private static Version ParseReleaseVersion(
        string tagName)
    {
        string value = tagName.Trim();

        if (value.StartsWith(
                "v",
                StringComparison.OrdinalIgnoreCase))
        {
            value = value[1..];
        }

        int separator = value.IndexOfAny(
            new[] { '-', '+' });

        if (separator >= 0)
        {
            value = value[..separator];
        }

        if (!Version.TryParse(
                value,
                out Version? version))
        {
            throw new InvalidDataException(
                $"The GitHub release tag '{tagName}' is not a valid version.");
        }

        return version;
    }
}
