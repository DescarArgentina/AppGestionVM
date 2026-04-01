using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppGestionDeVM.Models
{
    public class UsuarioAdmin : INotifyPropertyChanged
    {
        public int IdUsuario { get; set; }

        private string _usuario = string.Empty;
        public string Usuario
        {
            get => _usuario;
            set { _usuario = value; OnPropertyChanged(); }
        }

        private string _rol = string.Empty;
        public string Rol
        {
            get => _rol;
            set { _rol = value; OnPropertyChanged(); }
        }

        private bool _activo;
        public bool Activo
        {
            get => _activo;
            set { _activo = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
