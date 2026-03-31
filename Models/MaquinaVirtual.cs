using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppGestionDeVM.Models
{
    public class MaquinaVirtual : INotifyPropertyChanged
    {
        public int IdVM { get; set; }
        public string NombreVM { get; set; } = string.Empty;
        public string HostFisico { get; set; } = string.Empty;
        public string TipoHypervisor { get; set; } = string.Empty;
        public string NombreTecnicoVM { get; set; } = string.Empty;
        public bool Activa { get; set; }
        public int OrdenBoton { get; set; }
        public string RutaVMX { get; set; } = string.Empty;

        private string _estadoActual = string.Empty;
        public string EstadoActual
        {
            get => _estadoActual;
            set
            {
                if (_estadoActual != value)
                {
                    _estadoActual = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}