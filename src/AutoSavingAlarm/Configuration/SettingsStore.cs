using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoSavingAlarm.Configuration;

internal sealed class SettingsStore : IDisposable
{
    private const string AppFolderName = "AutoSavingAlarm";
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)
        }
    };

    private readonly string _settingsPath;
    private bool _disposed;

    public SettingsStore()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = Path.Combine(appDataPath, AppFolderName, SettingsFileName);
    }

    public (AppSettings Settings, bool Exists) Load()
    {
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;

        if (!File.Exists(_settingsPath))
        {
            return (AppSettings.CreateDefault(nowUtc), false);
        }

        try
        {
            string json = File.ReadAllText(_settingsPath, Encoding.UTF8);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);

            if (settings is null)
            {
                return (AppSettings.CreateDefault(nowUtc), false);
            }

            return (settings.Sanitize(nowUtc), true);
        }
        catch (JsonException)
        {
            return (AppSettings.CreateDefault(nowUtc), false);
        }
        catch (IOException)
        {
            return (AppSettings.CreateDefault(nowUtc), false);
        }
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

        string json = JsonSerializer.Serialize(settings.Sanitize(DateTimeOffset.UtcNow), SerializerOptions);
        File.WriteAllText(_settingsPath, json, new UTF8Encoding(false));
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
