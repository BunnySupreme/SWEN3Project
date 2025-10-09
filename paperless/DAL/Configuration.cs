using System.Text.Json;

namespace paperless.DAL
{
    public class Configuration
    {
        #region Configuration
        private const string _FILENAME = @"C:\Dev\configs\SWEN3Project\swen3project.config";
        #endregion

        #region Properties
        public string ConnectionString { get; set; } = String.Empty;
        #endregion

        #region Instance
        private static Configuration? _instance = null;
        public static Configuration Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (File.Exists(_FILENAME))
                    {
                        _instance = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(_FILENAME));
                    }
                    if (_instance == null)
                    { 
                        _instance = new Configuration();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Methods
        public void Save()
        {
            File.WriteAllText(_FILENAME, JsonSerializer.Serialize(this));
        }   
        #endregion
    }
}
