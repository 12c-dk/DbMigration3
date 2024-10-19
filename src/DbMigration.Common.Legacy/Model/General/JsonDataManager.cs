using System.Text.Json;

namespace DbMigration.Common.Legacy.Model.General;

public class JsonDataManager<T> where T : new()
{
    private T _data;
    private readonly string _filePath;
    private readonly object _lockObject = new object();
    private DateTime _lastLoadTime;
    //Only used for verification before saving
    private string _loadedJsonText;
    private readonly bool _manualLoading;

    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { WriteIndented = true };

    public JsonDataManager(string filePath, bool manualLoading = false, JsonSerializerOptions serializerOptions = null)
    {
        _manualLoading = manualLoading;
        _filePath = filePath;
        if (serializerOptions != null)
            _serializerOptions = serializerOptions;
        LoadData();
    }

    public JsonDataManager(string filePath, T data, bool manualLoading = false, JsonSerializerOptions serializerOptions = null)
    {
        _data = data;
        _manualLoading = manualLoading;
        _filePath = filePath;
        if (serializerOptions != null)
            _serializerOptions = serializerOptions;
        LoadData();//Creates file if it doesn't exist
        SaveData();
    }
    public T Data
    {
        get
        {
            //This gets called every time object is accessed or modified. 
            if (!_manualLoading)
                LoadData();
            return _data;
        }
        set
        {
            _data = value;
            SaveData();
        }
    }

    public void LoadData()
    {
        lock (_lockObject)
        {
            if (File.Exists(_filePath) && HasFileChangedSinceLastLoad())
            {
                string json = File.ReadAllText(_filePath);
                _loadedJsonText = json;
                _data = JsonSerializer.Deserialize<T>(json, _serializerOptions);
                _lastLoadTime = File.GetLastWriteTime(_filePath);
            }
            else if (_data == null)
            {
                _data = new T();
                File.WriteAllText(_filePath, JsonSerializer.Serialize(_data, _serializerOptions));
                _lastLoadTime = File.GetLastWriteTime(_filePath);
            }
        }
    }

    private bool HasFileChangedSinceLastLoad()
    {
        DateTime fileLastModifiedTimestamp = File.GetLastWriteTime(_filePath);
        return _lastLoadTime < fileLastModifiedTimestamp;
    }

    public void SaveData()
    {
        lock (_lockObject)
        {
            if (HasFileChangedSinceLastLoad())
            {
                throw new Exception("File has changed since last load. Save aborted.");
            }

            string json = JsonSerializer.Serialize(_data, _serializerOptions);

            if (json == _loadedJsonText)
            {
                //No changes made to object. No need to write to file. 
                return;
            }

            File.WriteAllText(_filePath, json);
            _loadedJsonText = json;
            _lastLoadTime = File.GetLastWriteTime(_filePath);

        }

    }
}