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
                    OnPropertyChanged(nameof(TextoBotonAccion));
                    OnPropertyChanged(nameof(BotonAccionHabilitado));
                }
            }
        }

        private string _usuarioEncendio = string.Empty;
        public string UsuarioEncendio
        {
            get => _usuarioEncendio;
            set
            {
                if (_usuarioEncendio != value)
                {
                    _usuarioEncendio = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _rdpListo;
        public bool RdpListo
        {
            get => _rdpListo;
            set
            {
                if (_rdpListo != value)
                {
                    _rdpListo = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TextoBotonAccion));
                    OnPropertyChanged(nameof(BotonAccionHabilitado));
                }
            }
        }

        public string TextoBotonAccion
        {
            get
            {
                if (EstadoActual.Equals("Apagada", StringComparison.OrdinalIgnoreCase))
                    return "Encender";

                if (EstadoActual.Equals("Encendida", StringComparison.OrdinalIgnoreCase))
                    return RdpListo ? "Ejecutar" : "Conectar";

                return "...";
            }
        }

        public bool BotonAccionHabilitado
            => !string.IsNullOrEmpty(_estadoActual);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
