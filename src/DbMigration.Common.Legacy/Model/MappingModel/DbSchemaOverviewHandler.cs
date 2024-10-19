using System.Text.Json;

namespace DbMigration.Common.Legacy.Model.MappingModel
{
    public class DbSchemaOverviewHandler
    {
        private Dictionary<Guid, string> _data;
        private readonly string _filePath;
        private DateTime _jsonLoadTime;
        private readonly object _lockObject = new object();

        public DbSchemaOverviewHandler()
        {
            _filePath = "schemaOverview.json";
            LoadData();
        }

        private void LoadData()
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                _data = JsonSerializer.Deserialize<Dictionary<Guid, string>>(json) ?? new Dictionary<Guid, string>();
            }
            else
            {
                _data = new Dictionary<Guid, string>();
            }
        }

        public void AddOrUpdate(Guid key, string value)
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

        public bool TryGetValue(Guid key, out string value)
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

        public bool Remove(Guid key)
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
        public Dictionary<Guid, string> GetAll()
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

        }

        private void SaveData()
        {
            string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
            _jsonLoadTime = File.GetLastWriteTime(_filePath);

        }
    }
}
