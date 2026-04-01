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

                // Eliminar VMs que ya no están activas
                var nuevosIds = lista.Select(v => v.IdVM).ToHashSet();
                for (int i = _maquinas.Count - 1; i >= 0; i--)
                    if (!nuevosIds.Contains(_maquinas[i].IdVM))
                        _maquinas.RemoveAt(i);

                // Actualizar existentes / agregar nuevas (preserva RdpListo e Iniciando)
                foreach (var vm in lista)
                {
                    var existente = _maquinas.FirstOrDefault(v => v.IdVM == vm.IdVM);
                    if (existente == null)
                    {
                        _maquinas.Add(vm);
                    }
                    else
                    {
                        existente.NombreVM = vm.NombreVM;
                        existente.RutaVMX = vm.RutaVMX;
                        existente.UsuarioEncendio = vm.UsuarioEncendio;
                        existente.OrdenBoton = vm.OrdenBoton;
                    }
                }

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

                var svcDb = new VmService();
                foreach (var vm in _maquinas)
                {
                    string nuevoEstado = encendidas.Contains(vm.RutaVMX) ? "Encendida" : "Apagada";
                    if (vm.EstadoActual != nuevoEstado)
                    {
                        vm.EstadoActual = nuevoEstado;
                        if (!string.Equals(nuevoEstado, "Encendida", StringComparison.OrdinalIgnoreCase))
                            vm.RdpListo = false;
                        await Task.Run(() => svcDb.ActualizarEstadoVM(vm.IdVM, nuevoEstado));
                    }
                }

                // ── Auto-iniciar monitoreo para VMs ya encendidas sin RDP listo ──
                foreach (var vm in _maquinas)
                {
                    if (vm.EstadoActual == "Encendida" && !vm.RdpListo && !_vmsMonitoreandoRdp.Contains(vm.RutaVMX))
                        _vmsMonitoreandoRdp.Add(vm.RutaVMX);
                }

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
                            vm.RdpListo = true;
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

            if (string.Equals(vm.EstadoActual, "Encendida", StringComparison.OrdinalIgnoreCase))
            {
                await EjecutarConexionRemota(vm);
                return;
            }

            btn.IsEnabled = false;
            vm.RdpListo = false;
            _vmsMonitoreandoRdp.Add(vm.RutaVMX);
            try
            {
                System.Diagnostics.Debug.WriteLine($"[INFO] Encendiendo VM: IdVM={vm.IdVM}, NombreVM={vm.NombreVM}");
                await Task.Run(() => new VmwareService().EncenderVM(vm.RutaVMX));

                // Actualizar el usuario que encendió la VM en la BD
                try
                {
                    string usuarioLogueado = _usuarioLogueado.Usuario ?? "(sin usuario)";
                    System.Diagnostics.Debug.WriteLine($"[INFO] Guardando usuario: IdVM={vm.IdVM}, Usuario={usuarioLogueado}");

                    await Task.Run(() => new VmService().ActualizarUsuarioEncendio(vm.IdVM, usuarioLogueado));
                    vm.UsuarioEncendio = usuarioLogueado;

                    System.Diagnostics.Debug.WriteLine($"[INFO] Usuario guardado exitosamente");
                }
                catch (Exception exDB)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Fallo al guardar usuario: {exDB.Message}");
                    MessageBox.Show($"Error al guardar usuario en BD:\n{exDB.Message}", "Error de base de datos",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }

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

        private async Task EjecutarConexionRemota(MaquinaVirtual vm)
        {
            try
            {
                var svc = new VmwareService();
                string? ip = await Task.Run(() => svc.LeerEstadoRDP(vm.RutaVMX));

                if (!Regex.IsMatch(ip ?? string.Empty, @"^\d+\.\d+\.\d+\.\d+$"))
                {
                    _vmsMonitoreandoRdp.Add(vm.RutaVMX);
                    _rdpDeteccionEnCurso.Remove(vm.RutaVMX);
                    _rdpMomento.Remove(vm.RutaVMX);

                    await Task.Run(() => svc.LanzarDeteccionRDP(vm.RutaVMX));
                    MessageBox.Show(
                        "La VM está encendida, pero todavía no tenemos la IP de Escritorio Remoto. " +
                        "Estamos validando la conexión y te va a aparecer el aviso cuando esté lista.",
                        "Conexión RDP en preparación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                _rdpIpActual = ip;
                Process.Start("mstsc.exe", $"/v:{ip}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el Escritorio Remoto:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
