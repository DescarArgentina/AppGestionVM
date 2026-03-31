namespace AppGestionDeVM.Models
{
    public class UsuarioSesion
    {
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}