using AppGestionDeVM.Models;
using Microsoft.Data.SqlClient;
using System.Configuration;

namespace AppGestionDeVM.Services
{
    public class AuthService
    {
        private readonly string _connectionString;

        public AuthService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ControlVMConnection"]?.ConnectionString
                ?? throw new Exception("No se encontró la cadena de conexión ControlVMConnection en App.config.");
        }

        public UsuarioSesion? ValidarLogin(string usuarioIngresado, string passwordIngresada)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"
                    SELECT TOP 1
                        IdUsuario,
                        Usuario,
                        PasswordHash,
                        Activo,
                        Rol
                    FROM dbo.Usuarios
                    WHERE Usuario = @Usuario";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Usuario", usuarioIngresado);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        bool activo = Convert.ToBoolean(reader["Activo"]);
                        if (!activo)
                            throw new Exception("El usuario está inactivo.");

                        string passwordGuardada = reader["PasswordHash"]?.ToString() ?? string.Empty;

                        if (!string.Equals(passwordGuardada, passwordIngresada, StringComparison.Ordinal))
                            return null;

                        return new UsuarioSesion
                        {
                            IdUsuario = Convert.ToInt32(reader["IdUsuario"]),
                            Usuario = reader["Usuario"]?.ToString() ?? string.Empty,
                            Rol = reader["Rol"]?.ToString() ?? string.Empty
                        };
                    }
                }
            }
        }
    }
}