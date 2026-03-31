using System.IO;
using System.Text.Json;

namespace AppGestionDeVM.Services
{
    public class LoginPreferencesService
    {
        private readonly string _folderPath;
        private readonly string _filePath;

        public LoginPreferencesService()
        {
            _folderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GestionVM");

            _filePath = Path.Combine(_folderPath, "login-preferences.json");
        }

        public LoginPreferences Cargar()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new LoginPreferences();

                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<LoginPreferences>(json) ?? new LoginPreferences();
            }
            catch
            {
                return new LoginPreferences();
            }
        }

        public void Guardar(LoginPreferences datos)
        {
            Directory.CreateDirectory(_folderPath);

            string json = JsonSerializer.Serialize(datos, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_filePath, json);
        }

        public void Limpiar()
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
    }

    public class LoginPreferences
    {
        public bool RecordarDatos { get; set; }
        public string Usuario { get; set; } = "";
        public string Password { get; set; } = "";
    }
}