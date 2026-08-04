using System.Text.Json.Serialization;

namespace IronResolve_Installer_V0._9._6;

public sealed class PackageManifest
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("generatedUtc")]
    public DateTime GeneratedUtc { get; set; }

    [JsonPropertyName("totalSize")]
    public long TotalSize { get; set; }

    [JsonPropertyName("packageCount")]
    public int PackageCount { get; set; }

    [JsonPropertyName("packages")]
    public List<PackageEntry> Packages { get; set; } = new();
}

public sealed class PackageEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("installPath")]
    public string InstallPath { get; set; } = "";

    [JsonPropertyName("archiveName")]
    public string ArchiveName { get; set; } = "";

    [JsonPropertyName("contentHash")]
    public string ContentHash { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

public sealed class InstalledPackageState
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("packages")]
    public Dictionary<string, InstalledPackage> Packages { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class InstalledPackage
{
    [JsonPropertyName("contentHash")]
    public string ContentHash { get; set; } = "";

    [JsonPropertyName("installPath")]
    public string InstallPath { get; set; } = "";
}

public sealed record DownloadUiProgress(
    int Percentage,
    string SpeedText,
    string StatusText);
