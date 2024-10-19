using System.Text.Json;

namespace DbMigration.Common.Legacy.Model.General;

public class JsonDictionaryManager
{
    private Dictionary<string, object> _data;
    private readonly string _filePath;
    private DateTime _jsonLoadTime;
    private readonly object _lockObject = new object();

    public JsonDictionaryManager(string filePath)
    {
        _filePath = filePath;
        LoadData();
    }

    private void LoadData()
    {
        if (File.Exists(_filePath))
        {
            string json = File.ReadAllText(_filePath);
            _data = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        else
        {
            _data = new Dictionary<string, object>();
        }
    }

    public void AddOrUpdate(string key, object value)
    {
        lock (_lockObject)
        {
            if (HasFileChangedSinceLastLoad())
            {
                LoadData();
            }
            _data[key] = value;
            SaveData();
        }

    }

    public bool TryGetValue(string key, out object value)
    {
        lock (_lockObject)
        {
            if (HasFileChangedSinceLastLoad())
            {
                LoadData();
            }

            return _data.TryGetValue(key, out value);
        }
    }

    public bool Remove(string key)
    {
        lock (_lockObject)
        {
            if (HasFileChangedSinceLastLoad())
            {
                LoadData();
            }
            bool removed = _data.Remove(key);
            if (removed) SaveData();
            return removed;
        }


    }

    //Get all elements
    public Dictionary<string, object> GetAll()
    {
        lock (_lockObject)
        {
            return _data;
        }
    }

    private bool HasFileChangedSinceLastLoad()
    {
        DateTime fileLastModifiedTimestamp = File.GetLastWriteTime(_filePath);

        return _jsonLoadTime < fileLastModifiedTimestamp;

        //Check:
        //JsonDictionaryManager dictionaryManager = new JsonDictionaryManager("path/to/file.json");
        //bool hasChanged = dictionaryManager.HasFileChangedSinceLastLoad();
    }

    private void SaveData()
    {
        string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
        _jsonLoadTime = File.GetLastWriteTime(_filePath);

    }
}