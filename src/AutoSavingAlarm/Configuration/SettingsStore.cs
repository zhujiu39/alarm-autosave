using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoSavingAlarm.Configuration;

internal enum SettingsLoadSource
{
    Defaults,
    Primary,
    Backup
}

internal enum SettingsReadStatus
{
    Success,
    Corrupt,
    Unavailable
}

internal sealed record SettingsStorageInfo(
    string SettingsPath,
    string BackupPath,
    bool PrimaryExists,
    bool BackupExists,
    DateTimeOffset? PrimaryLastWriteUtc,
    DateTimeOffset? BackupLastWriteUtc);

internal sealed record SettingsLoadResult(
    AppSettings Settings,
    bool Exists,
    SettingsLoadSource Source,
    SettingsStorageInfo StorageInfo,
    string? Notice);

internal sealed class SettingsStore : IDisposable
{
    private const string AppFolderName = "AutoSavingAlarm";
    private const string SettingsFileName = "settings.json";
    private const string BackupFileName = "settings.backup.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)
        }
    };

    private readonly string _settingsPath;
    private readonly string _backupPath;
    private bool _disposed;

    public SettingsStore()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string settingsDirectory = Path.Combine(appDataPath, AppFolderName);
        _settingsPath = Path.Combine(settingsDirectory, SettingsFileName);
        _backupPath = Path.Combine(settingsDirectory, BackupFileName);
    }

    public SettingsLoadResult Load()
    {
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        SettingsStorageInfo initialInfo = GetStorageInfo();
        bool hasAnySavedConfig = initialInfo.PrimaryExists || initialInfo.BackupExists;
        SettingsReadStatus primaryReadStatus = SettingsReadStatus.Unavailable;
        SettingsReadStatus backupReadStatus = SettingsReadStatus.Unavailable;

        if (initialInfo.PrimaryExists &&
            TryReadSettings(_settingsPath, nowUtc, out AppSettings? primarySettings, out primaryReadStatus))
        {
            return new SettingsLoadResult(
                primarySettings,
                Exists: true,
                SettingsLoadSource.Primary,
                initialInfo,
                Notice: null);
        }

        bool primaryArchived =
            initialInfo.PrimaryExists &&
            primaryReadStatus == SettingsReadStatus.Corrupt &&
            ArchiveCorruptFile(_settingsPath, "primary");

        if (initialInfo.BackupExists &&
            TryReadSettings(_backupPath, nowUtc, out AppSettings? backupSettings, out backupReadStatus))
        {
            string notice = BuildBackupRecoveryNotice(initialInfo.PrimaryExists, primaryReadStatus, primaryArchived);

            return new SettingsLoadResult(
                backupSettings,
                Exists: true,
                SettingsLoadSource.Backup,
                GetStorageInfo(),
                notice);
        }

        bool backupArchived =
            initialInfo.BackupExists &&
            backupReadStatus == SettingsReadStatus.Corrupt &&
            ArchiveCorruptFile(_backupPath, "backup");
        string? fallbackNotice = BuildFallbackNotice(
            hasAnySavedConfig,
            primaryReadStatus,
            backupReadStatus,
            primaryArchived,
            backupArchived);

        return new SettingsLoadResult(
            AppSettings.CreateDefault(nowUtc),
            hasAnySavedConfig,
            SettingsLoadSource.Defaults,
            GetStorageInfo(),
            fallbackNotice);
    }

    public SettingsStorageInfo GetStorageInfo()
    {
        return new SettingsStorageInfo(
            _settingsPath,
            _backupPath,
            File.Exists(_settingsPath),
            File.Exists(_backupPath),
            GetLastWriteUtc(_settingsPath),
            GetLastWriteUtc(_backupPath));
    }

    public bool TryLoadBackup(out AppSettings settings, out string? errorMessage)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!File.Exists(_backupPath))
        {
            settings = AppSettings.CreateDefault(DateTimeOffset.UtcNow);
            errorMessage = "当前没有可用的备份配置。";
            return false;
        }

        if (TryReadSettings(_backupPath, DateTimeOffset.UtcNow, out AppSettings? backupSettings, out SettingsReadStatus backupReadStatus))
        {
            settings = backupSettings;
            errorMessage = null;
            return true;
        }

        settings = AppSettings.CreateDefault(DateTimeOffset.UtcNow);

        if (backupReadStatus == SettingsReadStatus.Corrupt)
        {
            bool archived = ArchiveCorruptFile(_backupPath, "manual-backup");
            errorMessage = archived
                ? "最近备份已损坏，已归档损坏备份文件。"
                : "最近备份已损坏，但归档失败。";
            return false;
        }

        errorMessage = "最近备份当前无法读取，请稍后重试。";
        return false;
    }

    public void Save(AppSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        string? directory = Path.GetDirectoryName(_settingsPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("配置目录无效。");
        }

        Directory.CreateDirectory(directory);

        AppSettings sanitized = settings.Sanitize(DateTimeOffset.UtcNow);
        string json = JsonSerializer.Serialize(sanitized, SerializerOptions);
        string tempPath = _settingsPath + ".tmp";

        try
        {
            File.WriteAllText(tempPath, json, new UTF8Encoding(false));
            VerifySerializedSettings(tempPath);
            File.Move(tempPath, _settingsPath, overwrite: true);
            File.Copy(_settingsPath, _backupPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private static DateTimeOffset? GetLastWriteUtc(string path)
    {
        return File.Exists(path)
            ? File.GetLastWriteTimeUtc(path)
            : null;
    }

    private static void VerifySerializedSettings(string path)
    {
        string json = File.ReadAllText(path, Encoding.UTF8);
        AppSettings? verified = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);

        if (verified is null)
        {
            throw new InvalidOperationException("写入后的配置校验失败。");
        }
    }

    private static bool TryReadSettings(
        string path,
        DateTimeOffset nowUtc,
        out AppSettings settings,
        out SettingsReadStatus readStatus)
    {
        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            AppSettings? deserialized = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);

            if (deserialized is null)
            {
                settings = AppSettings.CreateDefault(nowUtc);
                readStatus = SettingsReadStatus.Corrupt;
                return false;
            }

            settings = deserialized.Sanitize(nowUtc);
            readStatus = SettingsReadStatus.Success;
            return true;
        }
        catch (JsonException)
        {
            settings = AppSettings.CreateDefault(nowUtc);
            readStatus = SettingsReadStatus.Corrupt;
            return false;
        }
        catch (IOException)
        {
            settings = AppSettings.CreateDefault(nowUtc);
            readStatus = SettingsReadStatus.Unavailable;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            settings = AppSettings.CreateDefault(nowUtc);
            readStatus = SettingsReadStatus.Unavailable;
            return false;
        }
    }

    private static string BuildBackupRecoveryNotice(
        bool primaryExists,
        SettingsReadStatus primaryReadStatus,
        bool primaryArchived)
    {
        if (!primaryExists)
        {
            return "未找到主配置，已从最近备份恢复。";
        }

        if (primaryArchived)
        {
            return "主配置已损坏并归档，已从最近备份恢复。";
        }

        return primaryReadStatus == SettingsReadStatus.Unavailable
            ? "主配置当前无法读取，已暂时使用最近备份。"
            : "主配置无法读取，已从最近备份恢复。";
    }

    private static string? BuildFallbackNotice(
        bool hasAnySavedConfig,
        SettingsReadStatus primaryReadStatus,
        SettingsReadStatus backupReadStatus,
        bool primaryArchived,
        bool backupArchived)
    {
        if (!hasAnySavedConfig)
        {
            return null;
        }

        if (primaryArchived || backupArchived)
        {
            return "配置文件已损坏并归档，当前已回退到默认设置。";
        }

        if (primaryReadStatus == SettingsReadStatus.Unavailable ||
            backupReadStatus == SettingsReadStatus.Unavailable)
        {
            return "配置文件当前无法读取，已临时回退到默认设置。";
        }

        return "配置文件无法读取，已回退到默认设置。";
    }

    private static bool ArchiveCorruptFile(string path, string suffix)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            string directory = Path.GetDirectoryName(path) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string archivedPath = Path.Combine(
                directory,
                $"{fileNameWithoutExtension}.corrupt-{suffix}-{timestamp}{extension}");

            int collisionIndex = 1;
            while (File.Exists(archivedPath))
            {
                archivedPath = Path.Combine(
                    directory,
                    $"{fileNameWithoutExtension}.corrupt-{suffix}-{timestamp}-{collisionIndex}{extension}");
                collisionIndex++;
            }

            File.Move(path, archivedPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
