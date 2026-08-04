using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace IronResolve_Installer_V0._9._6;

public sealed class PackageUpdateService
{
    // Two parallel downloads are more reliable on slower Wi-Fi,
    // mobile connections, and routes that reset multiple large streams.
    private const int MaxParallelDownloads = 2;
    private const int MaximumDownloadAttempts = 4;

    private readonly HttpClient httpClient;
    private readonly string addonsFolder;
    private readonly string statePath;

    public PackageUpdateService(
        HttpClient httpClient,
        string addonsFolder,
        string statePath)
    {
        this.httpClient = httpClient;
        this.addonsFolder = addonsFolder;
        this.statePath = statePath;
    }

    public async Task<PackageManifest> DownloadManifestAsync(
        string manifestUrl,
        CancellationToken cancellationToken)
    {
        string json = await httpClient.GetStringAsync(
            manifestUrl,
            cancellationToken);

        PackageManifest? manifest =
            JsonSerializer.Deserialize<PackageManifest>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (manifest is null ||
            manifest.Packages.Count == 0)
        {
            throw new InvalidDataException(
                "The online package manifest is empty or invalid.");
        }

        return manifest;
    }

    public InstalledPackageState LoadState()
    {
        try
        {
            if (!File.Exists(statePath))
            {
                return new InstalledPackageState();
            }

            string json = File.ReadAllText(statePath);

            return JsonSerializer.Deserialize<InstalledPackageState>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new InstalledPackageState();
        }
        catch
        {
            return new InstalledPackageState();
        }
    }

    public async Task<List<PackageEntry>> FindRequiredPackagesAsync(
        PackageManifest manifest,
        bool forceRepair,
        Action<string>? writeLog,
        CancellationToken cancellationToken)
    {
        if (forceRepair)
        {
            return manifest.Packages.ToList();
        }

        InstalledPackageState state = LoadState();
        var required = new List<PackageEntry>();
        bool stateChanged = false;

        string[] localFolders = Directory.Exists(addonsFolder)
            ? Directory.GetDirectories(
                addonsFolder,
                "*",
                SearchOption.TopDirectoryOnly)
            : Array.Empty<string>();

        foreach (PackageEntry package in manifest.Packages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(package.InstallPath))
            {
                bool rootCurrent =
                    state.Packages.TryGetValue(
                        package.Name,
                        out InstalledPackage? rootInstalled) &&
                    string.Equals(
                        rootInstalled.ContentHash,
                        package.ContentHash,
                        StringComparison.OrdinalIgnoreCase);

                if (!rootCurrent)
                {
                    required.Add(package);
                }

                continue;
            }

            state.Packages.TryGetValue(
                package.Name,
                out InstalledPackage? installed);

            string? localFolder = FindMatchingLocalFolder(
                package,
                installed,
                localFolders);

            if (localFolder is null)
            {
                required.Add(package);
                continue;
            }

            bool stateAlreadyCurrent =
                installed is not null &&
                string.Equals(
                    installed.ContentHash,
                    package.ContentHash,
                    StringComparison.OrdinalIgnoreCase) &&
                Directory.Exists(localFolder);

            if (stateAlreadyCurrent)
            {
                continue;
            }

            writeLog?.Invoke(
                $"Checking existing mod: {Path.GetFileName(localFolder)}");

            string localContentHash =
                await CalculateFolderContentHashAsync(
                    localFolder,
                    cancellationToken);

            if (string.Equals(
                    localContentHash,
                    package.ContentHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                string relativeFolder =
                    Path.GetRelativePath(
                        addonsFolder,
                        localFolder)
                    .Replace('\\', '/');

                state.Packages[package.Name] =
                    new InstalledPackage
                    {
                        ContentHash = package.ContentHash,
                        InstallPath = relativeFolder
                    };

                stateChanged = true;

                writeLog?.Invoke(
                    $"Existing mod recognized: {Path.GetFileName(localFolder)}");
            }
            else
            {
                required.Add(package);
            }
        }

        if (stateChanged)
        {
            state.Version = manifest.Version;
            await SaveStateAsync(state, cancellationToken);
        }

        return required;
    }

    private string? FindMatchingLocalFolder(
        PackageEntry package,
        InstalledPackage? installed,
        IReadOnlyList<string> localFolders)
    {
        // First use the actual folder previously recorded by the launcher.
        if (installed is not null &&
            !string.IsNullOrWhiteSpace(installed.InstallPath))
        {
            string recordedFolder = Path.Combine(
                addonsFolder,
                installed.InstallPath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));

            if (Directory.Exists(recordedFolder))
            {
                return recordedFolder;
            }
        }

        // Then try the exact folder name from the current manifest.
        string exactFolder = Path.Combine(
            addonsFolder,
            package.InstallPath.Replace(
                '/',
                Path.DirectorySeparatorChar));

        if (Directory.Exists(exactFolder))
        {
            return exactFolder;
        }

        // Arma Reforger mod folders normally end with a stable 16-character
        // hexadecimal workshop/mod ID. The readable name before it can change.
        string? stableId = ExtractStableModId(package.InstallPath)
            ?? ExtractStableModId(package.Name);

        if (stableId is null)
        {
            return null;
        }

        return localFolders.FirstOrDefault(folder =>
        {
            string folderName = Path.GetFileName(folder);
            string? folderId = ExtractStableModId(folderName);

            return string.Equals(
                folderId,
                stableId,
                StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string? ExtractStableModId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string name = Path.GetFileName(
            value.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar));

        int separator = Math.Max(
            name.LastIndexOf('_'),
            name.LastIndexOf('-'));

        string candidate = separator >= 0
            ? name[(separator + 1)..]
            : name;

        if (candidate.Length != 16 ||
            !candidate.All(Uri.IsHexDigit))
        {
            return null;
        }

        return candidate.ToUpperInvariant();
    }

    private static async Task<string> CalculateFolderContentHashAsync(
        string folder,
        CancellationToken cancellationToken)
    {
        using IncrementalHash combined =
            IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        string[] files = Directory.GetFiles(
                folder,
                "*",
                SearchOption.AllDirectories)
            .OrderBy(
                path => Path.GetRelativePath(folder, path),
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string relative = Path.GetRelativePath(folder, file)
                .Replace('\\', '/');

            combined.AppendData(
                System.Text.Encoding.UTF8.GetBytes(relative));

            combined.AppendData(
                BitConverter.GetBytes(
                    new FileInfo(file).Length));

            string fileHash =
                await CalculateSha256Async(
                    file,
                    cancellationToken);

            combined.AppendData(
                Convert.FromHexString(fileHash));
        }

        return Convert.ToHexString(
            combined.GetHashAndReset())
            .ToLowerInvariant();
    }


    public async Task InstallPackagesAsync(
        PackageManifest manifest,
        IReadOnlyList<PackageEntry> packages,
        Action<DownloadUiProgress> reportProgress,
        Action<string> writeLog,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(addonsFolder);

        string tempRoot = Path.Combine(
            Path.GetTempPath(),
            "IronResolveLauncher",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempRoot);

        long totalBytes = packages.Sum(x => x.Size);
        long totalDownloaded = 0;
        long bytesSinceSample = 0;
        long lastSampleTicks = Environment.TickCount64;
        long lastUiReportTicks = 0;
        double bytesPerSecond = 0;

        int completedDownloads = 0;
        int activeDownloads = 0;

        object progressLock = new();

        var downloadedArchives =
            new ConcurrentDictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        using var semaphore =
            new SemaphoreSlim(MaxParallelDownloads);

        try
        {
            Task[] downloadTasks = packages
                .Select(async package =>
                {
                    await semaphore.WaitAsync(
                        cancellationToken).ConfigureAwait(false);

                    try
                    {
                        Interlocked.Increment(
                            ref activeDownloads);

                        string zipPath = Path.Combine(
                            tempRoot,
                            package.ArchiveName);

                        bool downloadedSuccessfully = false;
                        Exception? lastDownloadError = null;

                        for (int attempt = 1;
                             attempt <= MaximumDownloadAttempts;
                             attempt++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            long packageDownloadedThisAttempt = 0;

                            try
                            {
                                if (File.Exists(zipPath))
                                {
                                    File.Delete(zipPath);
                                }

                                writeLog(
                                    attempt == 1
                                        ? $"Downloading package: {package.Name}"
                                        : $"Retrying package {package.Name} " +
                                          $"({attempt}/{MaximumDownloadAttempts})...");

                                using HttpResponseMessage response =
                                    await httpClient.GetAsync(
                                        package.Url,
                                        HttpCompletionOption.ResponseHeadersRead,
                                        cancellationToken).ConfigureAwait(false);

                                response.EnsureSuccessStatusCode();

                                await using Stream input =
                                    await response.Content.ReadAsStreamAsync(
                                        cancellationToken).ConfigureAwait(false);

                                await using FileStream output = new(
                                    zipPath,
                                    FileMode.Create,
                                    FileAccess.Write,
                                    FileShare.None,
                                    1024 * 1024,
                                    useAsync: true);

                                byte[] buffer = new byte[1024 * 1024];

                                while (true)
                                {
                                    int read = await input.ReadAsync(
                                        buffer,
                                        cancellationToken).ConfigureAwait(false);

                                    if (read == 0)
                                    {
                                        break;
                                    }

                                    await output.WriteAsync(
                                        buffer.AsMemory(0, read),
                                        cancellationToken).ConfigureAwait(false);

                                    packageDownloadedThisAttempt += read;

                                    DownloadUiProgress? uiUpdate = null;

                                    lock (progressLock)
                                    {
                                        totalDownloaded += read;
                                        bytesSinceSample += read;

                                        long now = Environment.TickCount64;

                                        double elapsedSeconds = Math.Max(
                                            (now - lastSampleTicks) / 1000.0,
                                            0.001);

                                        if (elapsedSeconds >= 1.0)
                                        {
                                            bytesPerSecond =
                                                bytesSinceSample / elapsedSeconds;

                                            bytesSinceSample = 0;
                                            lastSampleTicks = now;
                                        }

                                        if (now - lastUiReportTicks >= 250)
                                        {
                                            lastUiReportTicks = now;

                                            int percentage = totalBytes <= 0
                                                ? 100
                                                : (int)Math.Clamp(
                                                    totalDownloaded * 100L / totalBytes,
                                                    0,
                                                    100);

                                            string speedText = bytesPerSecond > 0
                                                ? $"{FormatBytes((long)bytesPerSecond)}/s"
                                                : "Calculating...";

                                            uiUpdate = new DownloadUiProgress(
                                                percentage,
                                                speedText,
                                                $"Downloading {completedDownloads:N0}/" +
                                                $"{packages.Count:N0} complete — " +
                                                $"{activeDownloads:N0} active");
                                        }
                                    }

                                    if (uiUpdate is not null)
                                    {
                                        reportProgress(uiUpdate);
                                    }
                                }

                                await output.FlushAsync(
                                    cancellationToken).ConfigureAwait(false);

                                output.Close();

                                string actualHash =
                                    await CalculateSha256Async(
                                        zipPath,
                                        cancellationToken).ConfigureAwait(false);

                                if (!string.Equals(
                                        actualHash,
                                        package.Sha256,
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new InvalidDataException(
                                        $"Package verification failed: {package.Name}");
                                }

                                downloadedSuccessfully = true;
                                break;
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex) when (
                                ex is HttpRequestException ||
                                ex is IOException ||
                                ex is InvalidDataException)
                            {
                                lastDownloadError = ex;

                                // Remove bytes from this failed attempt so the
                                // total progress remains accurate after retry.
                                lock (progressLock)
                                {
                                    totalDownloaded = Math.Max(
                                        0,
                                        totalDownloaded -
                                        packageDownloadedThisAttempt);

                                    bytesSinceSample = 0;
                                    bytesPerSecond = 0;
                                    lastSampleTicks = Environment.TickCount64;
                                }

                                try
                                {
                                    if (File.Exists(zipPath))
                                    {
                                        File.Delete(zipPath);
                                    }
                                }
                                catch
                                {
                                    // The retry will recreate the file.
                                }

                                if (attempt >= MaximumDownloadAttempts)
                                {
                                    break;
                                }

                                int delaySeconds = attempt * 3;

                                writeLog(
                                    $"Temporary download error for {package.Name}: " +
                                    $"{ex.Message} Retrying in {delaySeconds}s...");

                                reportProgress(
                                    new DownloadUiProgress(
                                        totalBytes <= 0
                                            ? 0
                                            : (int)Math.Clamp(
                                                totalDownloaded * 100L / totalBytes,
                                                0,
                                                100),
                                        "Reconnecting...",
                                        $"Retrying {package.Name} " +
                                        $"({attempt + 1}/{MaximumDownloadAttempts})"));

                                await Task.Delay(
                                    TimeSpan.FromSeconds(delaySeconds),
                                    cancellationToken).ConfigureAwait(false);
                            }
                        }

                        if (!downloadedSuccessfully)
                        {
                            throw new IOException(
                                $"Could not download {package.Name} after " +
                                $"{MaximumDownloadAttempts} attempts.",
                                lastDownloadError);
                        }

                        downloadedArchives[package.Name] =
                            zipPath;

                        int completed = Interlocked.Increment(
                            ref completedDownloads);

                        int currentActive = Volatile.Read(
                            ref activeDownloads);

                        long downloadedNow;
                        double currentSpeed;

                        lock (progressLock)
                        {
                            downloadedNow = totalDownloaded;
                            currentSpeed = bytesPerSecond;
                        }

                        int completedPercent = totalBytes <= 0
                            ? 100
                            : (int)Math.Clamp(
                                downloadedNow * 100L / totalBytes,
                                0,
                                100);

                        reportProgress(new DownloadUiProgress(
                            completedPercent,
                            currentSpeed > 0
                                ? $"{FormatBytes((long)currentSpeed)}/s"
                                : "Calculating...",
                            $"Downloading {completed:N0}/" +
                            $"{packages.Count:N0} complete — " +
                            $"{Math.Max(0, currentActive - 1):N0} active"));

                        writeLog(
                            $"Downloaded package: {package.Name}");
                    }
                    finally
                    {
                        Interlocked.Decrement(
                            ref activeDownloads);

                        semaphore.Release();
                    }
                })
                .ToArray();

            await Task.WhenAll(downloadTasks).ConfigureAwait(false);

            // Installation is sequential to avoid file-system conflicts.
            for (int index = 0;
                 index < packages.Count;
                 index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                PackageEntry package = packages[index];

                if (!downloadedArchives.TryGetValue(
                        package.Name,
                        out string? zipPath))
                {
                    throw new FileNotFoundException(
                        $"Downloaded archive is missing: " +
                        $"{package.Name}");
                }

                reportProgress(
                    new DownloadUiProgress(
                        100,
                        "Installing",
                        $"Installing {index + 1:N0}/" +
                        $"{packages.Count:N0}: {package.Name}"));

                await ExtractAndInstallAsync(
                    zipPath,
                    package,
                    tempRoot,
                    cancellationToken).ConfigureAwait(false);

                writeLog(
                    $"Installed package: {package.Name}");
            }

            InstalledPackageState state = LoadState();

            foreach (PackageEntry package in packages)
            {
                state.Packages[package.Name] =
                    new InstalledPackage
                    {
                        ContentHash =
                            package.ContentHash,
                        InstallPath =
                            package.InstallPath
                    };
            }

            RemovePackagesNoLongerInManifest(
                manifest,
                state,
                writeLog);

            state.Version = manifest.Version;

            await SaveStateAsync(
                state,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(
                        tempRoot,
                        recursive: true);
                }
            }
            catch
            {
                // Temporary cleanup failure is not fatal.
            }
        }
    }

    private async Task ExtractAndInstallAsync(
        string zipPath,
        PackageEntry package,
        string tempRoot,
        CancellationToken cancellationToken)
    {
        string staging = Path.Combine(
            tempRoot,
            "extract-" +
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(staging);

        await Task.Run(() =>
        {
            using ZipArchive archive =
                ZipFile.OpenRead(zipPath);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string destination = Path.GetFullPath(
                    Path.Combine(
                        staging,
                        entry.FullName.Replace(
                            '/',
                            Path.DirectorySeparatorChar)));

                string safeRoot = Path.GetFullPath(staging)
                    .TrimEnd(Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                if (!destination.StartsWith(
                        safeRoot,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Unsafe ZIP path in package " +
                        $"{package.Name}.");
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destination);
                    continue;
                }

                Directory.CreateDirectory(
                    Path.GetDirectoryName(destination)!);

                entry.ExtractToFile(
                    destination,
                    overwrite: true);
            }
        }, cancellationToken);

        if (string.IsNullOrWhiteSpace(
                package.InstallPath))
        {
            CopyDirectoryContents(
                staging,
                addonsFolder);

            Directory.Delete(
                staging,
                recursive: true);

            return;
        }

        string destinationFolder = Path.GetFullPath(
            Path.Combine(
                addonsFolder,
                package.InstallPath.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));

        string safeAddonsRoot =
            Path.GetFullPath(addonsFolder)
                .TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        if (!destinationFolder.StartsWith(
                safeAddonsRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Unsafe install path: " +
                $"{package.InstallPath}");
        }

        if (Directory.Exists(destinationFolder))
        {
            Directory.Delete(
                destinationFolder,
                recursive: true);
        }

        Directory.CreateDirectory(
            Path.GetDirectoryName(
                destinationFolder)!);

        Directory.Move(
            staging,
            destinationFolder);
    }

    private void RemovePackagesNoLongerInManifest(
        PackageManifest manifest,
        InstalledPackageState state,
        Action<string> writeLog)
    {
        HashSet<string> onlineNames =
            manifest.Packages
                .Select(x => x.Name)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        string[] removed =
            state.Packages.Keys
                .Where(name =>
                    !onlineNames.Contains(name))
                .ToArray();

        foreach (string name in removed)
        {
            InstalledPackage old =
                state.Packages[name];

            if (!string.IsNullOrWhiteSpace(
                    old.InstallPath))
            {
                string folder = Path.GetFullPath(
                    Path.Combine(
                        addonsFolder,
                        old.InstallPath.Replace(
                            '/',
                            Path.DirectorySeparatorChar)));

                string safeRoot =
                    Path.GetFullPath(addonsFolder)
                        .TrimEnd(
                            Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                if (folder.StartsWith(
                        safeRoot,
                        StringComparison.OrdinalIgnoreCase) &&
                    Directory.Exists(folder))
                {
                    Directory.Delete(
                        folder,
                        recursive: true);

                    writeLog(
                        $"Removed old package: {name}");
                }
            }

            state.Packages.Remove(name);
        }
    }

    private async Task SaveStateAsync(
        InstalledPackageState state,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(
            state,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(
            statePath,
            json,
            cancellationToken);
    }

    private static void CopyDirectoryContents(
        string source,
        string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (string directory in
                 Directory.GetDirectories(
                     source,
                     "*",
                     SearchOption.AllDirectories))
        {
            string relative =
                Path.GetRelativePath(
                    source,
                    directory);

            Directory.CreateDirectory(
                Path.Combine(
                    destination,
                    relative));
        }

        foreach (string file in
                 Directory.GetFiles(
                     source,
                     "*",
                     SearchOption.AllDirectories))
        {
            string relative =
                Path.GetRelativePath(
                    source,
                    file);

            string target =
                Path.Combine(
                    destination,
                    relative);

            Directory.CreateDirectory(
                Path.GetDirectoryName(target)!);

            File.Copy(
                file,
                target,
                overwrite: true);
        }
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

        byte[] hash = await SHA256.HashDataAsync(
            stream,
            cancellationToken);

        return Convert.ToHexString(
            hash).ToLowerInvariant();
    }

    private static string FormatBytes(long bytes)
    {
        string[] units =
            { "B", "KB", "MB", "GB", "TB" };

        double value = Math.Max(0, bytes);
        int unit = 0;

        while (value >= 1024 &&
               unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}