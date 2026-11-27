using System.Text.Json;

namespace AgriConnect.Shared.Storage;

public class JsonFileStore<T> where T : class
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _filePath;
    private readonly object _sync = new();

    public JsonFileStore(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    public T LoadOrCreate(Func<T> seedFactory)
    {
        lock (_sync)
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    var loaded = JsonSerializer.Deserialize<T>(File.ReadAllText(_filePath));
                    if (loaded is not null) return loaded;
                }
                catch { }
            }

            var seeded = seedFactory();
            Save(seeded);
            return seeded;
        }
    }

    public void Save(T data)
    {
        lock (_sync)
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(data, Options));
        }
    }
}
