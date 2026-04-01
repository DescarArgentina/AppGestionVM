using AppGestionDeVM.Models;
using AppGestionDeVM.Services;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace AppGestionDeVM.Views
{
    public partial class AdminWindow : Window
    {
        private readonly string _connectionString;
        private readonly ObservableCollection<MaquinaVirtual> _listaVMs = new();
        private readonly ObservableCollection<UsuarioAdmin> _listaUsuarios = new();
        private UsuarioAdmin? _usuarioSeleccionado;

        public AdminWindow()
        {
            InitializeComponent();

            _connectionString = ObtenerConnectionString();

            cmbRolUsuario.SelectedIndex = 1;
            chkUsuarioActivo.IsChecked = true;
            chkVmActiva.IsChecked = true;
            txtOrdenBoton.Text = "0";

            dgVMs.ItemsSource = _listaVMs;
            dgUsuarios.ItemsSource = _listaUsuarios;
        }

        private static string ObtenerConnectionString()
        {
            var settings = ConfigurationManager.ConnectionStrings["ControlVMConnection"];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new InvalidOperationException("No se encontró la cadena de conexión ControlVMConnection.");
            return settings.ConnectionString;
        }

        // ─── Cambio de tab ───────────────────────────────────────────────────────

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ignorar eventos que burbujean desde DataGrid u otros controles internos
            if (!ReferenceEquals(e.OriginalSource, tabControl)) return;
            if (tabControl.SelectedItem is not TabItem tab) return;

            switch (tab.Header?.ToString())
            {
                case "Editar VM":
                    await CargarListaVMs();
                    break;
                case "Editar Usuario":
                    await CargarListaUsuarios();
                    break;
            }
        }

        // ─── Cargar listas ───────────────────────────────────────────────────────

        private async Task CargarListaVMs()
        {
            try
            {
                var lista = await Task.Run(() => new VmService().ObtenerTodasLasMaquinas());
                _listaVMs.Clear();
                foreach (var vm in lista)
                    _listaVMs.Add(vm);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar VMs:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CargarListaUsuarios()
        {
            try
            {
                var lista = await Task.Run(() => new VmService().ObtenerTodosLosUsuarios());
                _listaUsuarios.Clear();
                foreach (var u in lista)
                    _listaUsuarios.Add(u);

                pnlEditarUsuario.Visibility = Visibility.Collapsed;
                _usuarioSeleccionado = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar usuarios:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Editar VM ───────────────────────────────────────────────────────────

        private async void BtnActualizarListaVMs_Click(object sender, RoutedEventArgs e)
        {
            await CargarListaVMs();
        }

        private async void BtnToggleVmActiva_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not MaquinaVirtual vm)
                return;

            bool nuevoEstado = !vm.Activa;
            string accion = nuevoEstado ? "activar" : "desactivar";

            var confirm = MessageBox.Show(
                $"¿Querés {accion} la VM \"{vm.NombreVM}\"?",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await Task.Run(() => new VmService().ToggleActivaVM(vm.IdVM, nuevoEstado));
                vm.Activa = nuevoEstado;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo actualizar la VM:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Editar Usuario ──────────────────────────────────────────────────────

        private void DgUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUsuarios.SelectedItem is UsuarioAdmin usuario)
            {
                _usuarioSeleccionado = usuario;
                txtEditNombre.Text = usuario.Usuario;
                cmbEditRol.SelectedIndex = usuario.Rol == "Administrador" ? 0 : 1;
                pnlEditarUsuario.Visibility = Visibility.Visible;
            }
            else
            {
                pnlEditarUsuario.Visibility = Visibility.Collapsed;
                _usuarioSeleccionado = null;
            }
        }

        private async void BtnGuardarCambiosUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (_usuarioSeleccionado == null) return;

            string nuevoNombre = txtEditNombre.Text.Trim();
            if (string.IsNullOrWhiteSpace(nuevoNombre))
            {
                MessageBox.Show("Ingresá un nombre de usuario.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbEditRol.SelectedItem is not ComboBoxItem rolItem)
            {
                MessageBox.Show("Seleccioná un rol.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nuevoRol = rolItem.Content?.ToString() ?? string.Empty;

            try
            {
                int id = _usuarioSeleccionado.IdUsuario;
                await Task.Run(() => new VmService().ActualizarUsuario(id, nuevoNombre, nuevoRol));

                _usuarioSeleccionado.Usuario = nuevoNombre;
                _usuarioSeleccionado.Rol = nuevoRol;

                MessageBox.Show("Usuario actualizado correctamente.", "OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo actualizar el usuario:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEliminarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (_usuarioSeleccionado == null) return;

            var confirm = MessageBox.Show(
                $"¿Eliminar al usuario \"{_usuarioSeleccionado.Usuario}\"? Esta acción no se puede deshacer.",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                int id = _usuarioSeleccionado.IdUsuario;
                await Task.Run(() => new VmService().EliminarUsuario(id));
                await CargarListaUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo eliminar el usuario:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Cerrar ───────────────────────────────────────────────────────────────

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ─── Guardar VM ───────────────────────────────────────────────────────────

        private async void BtnGuardarVm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nombreVm = txtNombreVm.Text.Trim();
                string rutaVmx = txtRutaVmx.Text.Trim();

                if (string.IsNullOrWhiteSpace(nombreVm))
                {
                    MessageBox.Show("Ingresá el nombre de la VM.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(rutaVmx))
                {
                    MessageBox.Show("Ingresá la ruta VMX.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtOrdenBoton.Text.Trim(), out int ordenBoton))
                {
                    MessageBox.Show("El orden del botón debe ser numérico.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool activa = chkVmActiva.IsChecked == true;

                await Task.Run(() => GuardarVm(nombreVm, rutaVmx, ordenBoton, activa));

                MessageBox.Show("La VM se guardó correctamente.", "OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                txtNombreVm.Clear();
                txtRutaVmx.Clear();
                txtOrdenBoton.Text = "0";
                chkVmActiva.IsChecked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo guardar la VM:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Guardar Usuario ──────────────────────────────────────────────────────

        private async void BtnGuardarUsuario_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string usuario = txtUsuario.Text.Trim();
                string password = txtPassword.Password;

                if (cmbRolUsuario.SelectedItem is not ComboBoxItem item)
                {
                    MessageBox.Show("Seleccioná un rol.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string rol = item.Content?.ToString() ?? string.Empty;
                bool activo = chkUsuarioActivo.IsChecked == true;

                if (string.IsNullOrWhiteSpace(usuario))
                {
                    MessageBox.Show("Ingresá el nombre de usuario.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Ingresá la contraseña.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(rol))
                {
                    MessageBox.Show("Seleccioná un rol.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await Task.Run(() => GuardarUsuario(usuario, password, rol, activo));

                MessageBox.Show("El usuario se guardó correctamente.", "OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                txtUsuario.Clear();
                txtPassword.Clear();
                cmbRolUsuario.SelectedIndex = 1;
                chkUsuarioActivo.IsChecked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo guardar el usuario:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── DB helpers ───────────────────────────────────────────────────────────

        private void GuardarUsuario(string usuario, string passwordPlano, string rol, bool activo)
        {
            using SqlConnection cn = new SqlConnection(_connectionString);
            cn.Open();

            using (SqlCommand cmdExiste = new SqlCommand(
                "SELECT COUNT(1) FROM Usuarios WHERE Usuario = @Usuario", cn))
            {
                cmdExiste.Parameters.AddWithValue("@Usuario", usuario);
                if (Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0)
                    throw new Exception("Ya existe un usuario con ese nombre.");
            }

            string passwordHash = HashPassword(passwordPlano);

            using SqlCommand cmdInsert = new SqlCommand(
                @"INSERT INTO Usuarios (Usuario, PasswordHash, Activo, FechaAlta, Rol)
                  VALUES (@Usuario, @PasswordHash, @Activo, SYSDATETIME(), @Rol)", cn);

            cmdInsert.Parameters.AddWithValue("@Usuario", usuario);
            cmdInsert.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmdInsert.Parameters.AddWithValue("@Activo", activo);
            cmdInsert.Parameters.AddWithValue("@Rol", rol);
            cmdInsert.ExecuteNonQuery();
        }

        private void GuardarVm(string nombreVm, string rutaVmx, int ordenBoton, bool activa)
        {
            using SqlConnection cn = new SqlConnection(_connectionString);
            cn.Open();

            using (SqlCommand cmdExiste = new SqlCommand(
                "SELECT COUNT(1) FROM MaquinasVirtuales WHERE NombreVM = @NombreVM OR RutaVMX = @RutaVMX", cn))
            {
                cmdExiste.Parameters.AddWithValue("@NombreVM", nombreVm);
                cmdExiste.Parameters.AddWithValue("@RutaVMX", rutaVmx);
                if (Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0)
                    throw new Exception("Ya existe una VM con ese nombre o con esa ruta.");
            }

            using SqlCommand cmdInsert = new SqlCommand(
                @"INSERT INTO MaquinasVirtuales (NombreVM, RutaVMX, EstadoActual, UsuarioEncendio, OrdenBoton, Activa)
                  VALUES (@NombreVM, @RutaVMX, @EstadoActual, @UsuarioEncendio, @OrdenBoton, @Activa)", cn);

            cmdInsert.Parameters.AddWithValue("@NombreVM", nombreVm);
            cmdInsert.Parameters.AddWithValue("@RutaVMX", rutaVmx);
            cmdInsert.Parameters.AddWithValue("@EstadoActual", "Apagada");
            cmdInsert.Parameters.AddWithValue("@UsuarioEncendio", DBNull.Value);
            cmdInsert.Parameters.AddWithValue("@OrdenBoton", ordenBoton);
            cmdInsert.Parameters.AddWithValue("@Activa", activa);
            cmdInsert.ExecuteNonQuery();
        }

        private static string HashPassword(string password)
        {
            return global::BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
