using AppGestionDeVM.Models;
using AppGestionDeVM.Services;
using System.Windows;
using System.Windows.Input;

namespace AppGestionDeVM.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginPreferencesService _prefs = new();

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var datos = _prefs.Cargar();
            if (datos.RecordarDatos)
            {
                txtUsuario.Text = datos.Usuario;
                txtPassword.Password = datos.Password;
                chkRecordar.IsChecked = true;
            }

            txtUsuario.Focus();
        }

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnIngresar_Click(object sender, RoutedEventArgs e)
        {
            RealizarLogin();
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                RealizarLogin();
        }

        private void RealizarLogin()
        {
            string usuario = txtUsuario.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(usuario))
            {
                MessageBox.Show("Ingresá el usuario.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsuario.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Ingresá la contraseña.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }

            try
            {
                AuthService authService = new AuthService();
                UsuarioSesion? usuarioLogueado = authService.ValidarLogin(usuario, password);

                if (usuarioLogueado == null)
                {
                    MessageBox.Show("Usuario o contraseña incorrectos.", "Login", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }
                if (chkRecordar.IsChecked == true)
                    _prefs.Guardar(new LoginPreferences { RecordarDatos = true, Usuario = usuario, Password = password });
                else
                    _prefs.Limpiar();

                AppGestionDeVM.MainWindow mainWindow = new AppGestionDeVM.MainWindow(usuarioLogueado);
                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al iniciar sesión:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}