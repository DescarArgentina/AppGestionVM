using AppGestionDeVM.Models;
using AppGestionDeVM.Services;
using AppGestionDeVM.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AppGestionDeVM
{
    public partial class MainWindow : Window
    {
        private readonly UsuarioSesion _usuarioLogueado;
        private readonly ObservableCollection<MaquinaVirtual> _maquinas = new();
        private readonly DispatcherTimer _timer = new();

        // RDP monitoring
        private readonly HashSet<string> _vmsMonitoreandoRdp = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _rdpDeteccionEnCurso = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _rdpMomento = new(StringComparer.OrdinalIgnoreCase);
        private string _rdpIpActual = string.Empty;

        public MainWindow(UsuarioSesion usuarioLogueado)
        {
            InitializeComponent();
            _usuarioLogueado = usuarioLogueado;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            lblUsuario.Text = _usuarioLogueado.Usuario;
            lblRol.Text = _usuarioLogueado.Rol;
            lblInicial.Text = _usuarioLogueado.Usuario?.Length > 0
                ? _usuarioLogueado.Usuario[0].ToString().ToUpper()
                : "?";

            icVMs.ItemsSource = _maquinas;

            await CargarMaquinas();

            _timer.Interval = TimeSpan.FromSeconds(15);
            _timer.Tick += async (s, ev) => await CargarMaquinas();
            _timer.Start();
        }

        private async Task CargarMaquinas()
        {
            try
            {
                var lista = await Task.Run(() => new VmService().ObtenerMaquinasActivas());
                _maquinas.Clear();
                foreach (var vm in lista)
                    _maquinas.Add(vm);

                await ActualizarEstados();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar las VMs:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ActualizarEstados()
        {
            try
            {
                var svc = new VmwareService();
                var encendidas = await Task.Run(() => svc.ObtenerRutasVmsEncendidas());

                foreach (var vm in _maquinas)
                    vm.EstadoActual = encendidas.Contains(vm.RutaVMX) ? "Encendida" : "Apagada";

                // ── Monitoreo RDP ──
                foreach (var vm in _maquinas)
                {
                    if (!_vmsMonitoreandoRdp.Contains(vm.RutaVMX)) continue;
                    if (vm.EstadoActual != "Encendida") continue;

                    if (!_rdpDeteccionEnCurso.Contains(vm.RutaVMX))
                    {
                        // Disparar detección en PC-26
                        _rdpDeteccionEnCurso.Add(vm.RutaVMX);
                        _rdpMomento[vm.RutaVMX] = DateTime.Now;
                        var ruta = vm.RutaVMX;
                        _ = Task.Run(() => svc.LanzarDeteccionRDP(ruta));
                    }
                    else if ((DateTime.Now - _rdpMomento[vm.RutaVMX]).TotalSeconds >= 18)
                    {
                        // Leer resultado
                        var ruta = vm.RutaVMX;
                        var ip = await Task.Run(() => svc.LeerEstadoRDP(ruta));

                        if (ip != null && Regex.IsMatch(ip, @"^\d+\.\d+\.\d+\.\d+$"))
                        {
                            _vmsMonitoreandoRdp.Remove(ruta);
                            _rdpDeteccionEnCurso.Remove(ruta);
                            _rdpMomento.Remove(ruta);
                            MostrarNotificacionRdp(vm.NombreVM, ip);
                        }
                        else
                        {
                            // No está listo aún, reintentar en el próximo ciclo
                            _rdpDeteccionEnCurso.Remove(ruta);
                            _rdpMomento.Remove(ruta);
                        }
                    }
                }

                lblUltimaActualizacion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                lblUltimaActualizacion.Text = $"Error WMI: {ex.Message}";
            }
        }

        private async void MostrarNotificacionRdp(string nombreVm, string ip)
        {
            _rdpIpActual = ip;
            toastRdpNombre.Text = nombreVm;
            toastRdpIp.Text = $"Hacé clic para conectarte  ·  {ip}";
            toastRdp.Visibility = Visibility.Visible;
            await Task.Delay(10000);
            toastRdp.Visibility = Visibility.Collapsed;
        }

        private void ToastRdp_Click(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(_rdpIpActual)) return;
            Process.Start("mstsc.exe", $"/v:{_rdpIpActual}");
            toastRdp.Visibility = Visibility.Collapsed;
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await CargarMaquinas();
        }

        private async void BtnEncender_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn ||
                btn.DataContext is not MaquinaVirtual vm)
                return;

            if (string.IsNullOrWhiteSpace(vm.RutaVMX))
            {
                MessageBox.Show("Esta VM no tiene configurada la ruta VMX.", "Sin ruta VMX",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btn.IsEnabled = false;
            _vmsMonitoreandoRdp.Add(vm.RutaVMX);
            try
            {
                await Task.Run(() => new VmwareService().EncenderVM(vm.RutaVMX));
                await Task.Delay(5000);
                await ActualizarEstados();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al encender la VM:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                DragMove();
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            new LoginWindow().Show();
            Close();
        }

        private void BtnMinimizar_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
            => Close();

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}
