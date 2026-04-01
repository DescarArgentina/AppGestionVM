using AppGestionDeVM.Models;
using Microsoft.Data.SqlClient;
using System.Configuration;

namespace AppGestionDeVM.Services
{
    public class VmService
    {
        private readonly string _connectionString;

        public VmService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ControlVMConnection"]?.ConnectionString
                ?? throw new Exception("No se encontró la cadena de conexión ControlVMConnection en App.config.");
        }

        public Dictionary<int, string> ObtenerEstadosVMs()
        {
            var estados = new Dictionary<int, string>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = "SELECT IdVM, EstadoActual FROM dbo.MaquinasVirtuales WHERE Activa = 1";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        estados[Convert.ToInt32(reader["IdVM"])] = reader["EstadoActual"]?.ToString() ?? string.Empty;
                    }
                }
            }

            return estados;
        }

        public List<MaquinaVirtual> ObtenerMaquinasActivas()
        {
            List<MaquinaVirtual> lista = new List<MaquinaVirtual>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"
                    SELECT
                        IdVM,
                        NombreVM,
                        HostFisico,
                        TipoHypervisor,
                        NombreTecnicoVM,
                        Activa,
                        EstadoActual,
                        OrdenBoton,
                        RutaVMX,
                        UsuarioEncendio
                    FROM dbo.MaquinasVirtuales
                    WHERE Activa = 1
                    ORDER BY OrdenBoton, NombreVM";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new MaquinaVirtual
                        {
                            IdVM = Convert.ToInt32(reader["IdVM"]),
                            NombreVM = reader["NombreVM"]?.ToString() ?? string.Empty,
                            HostFisico = reader["HostFisico"]?.ToString() ?? string.Empty,
                            TipoHypervisor = reader["TipoHypervisor"]?.ToString() ?? string.Empty,
                            NombreTecnicoVM = reader["NombreTecnicoVM"]?.ToString() ?? string.Empty,
                            Activa = Convert.ToBoolean(reader["Activa"]),
                            EstadoActual = reader["EstadoActual"]?.ToString() ?? string.Empty,
                            OrdenBoton = Convert.ToInt32(reader["OrdenBoton"]),
                            RutaVMX = reader["RutaVMX"]?.ToString() ?? string.Empty,
                            UsuarioEncendio = reader["UsuarioEncendio"]?.ToString() ?? string.Empty
                        });
                    }
                }
            }

            return lista;
        }

        public void ActualizarEstadoVM(int idVM, string estado)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "UPDATE [dbo].[MaquinasVirtuales] SET [EstadoActual] = @Estado WHERE [IdVM] = @IdVM";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdVM", idVM);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<MaquinaVirtual> ObtenerTodasLasMaquinas()
        {
            var lista = new List<MaquinaVirtual>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            const string query = @"
                SELECT IdVM, NombreVM, HostFisico, TipoHypervisor, NombreTecnicoVM,
                       Activa, EstadoActual, OrdenBoton, RutaVMX, UsuarioEncendio
                FROM dbo.MaquinasVirtuales
                ORDER BY OrdenBoton, NombreVM";
            using var cmd = new SqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new MaquinaVirtual
                {
                    IdVM = Convert.ToInt32(reader["IdVM"]),
                    NombreVM = reader["NombreVM"]?.ToString() ?? string.Empty,
                    HostFisico = reader["HostFisico"]?.ToString() ?? string.Empty,
                    TipoHypervisor = reader["TipoHypervisor"]?.ToString() ?? string.Empty,
                    NombreTecnicoVM = reader["NombreTecnicoVM"]?.ToString() ?? string.Empty,
                    Activa = Convert.ToBoolean(reader["Activa"]),
                    EstadoActual = reader["EstadoActual"]?.ToString() ?? string.Empty,
                    OrdenBoton = Convert.ToInt32(reader["OrdenBoton"]),
                    RutaVMX = reader["RutaVMX"]?.ToString() ?? string.Empty,
                    UsuarioEncendio = reader["UsuarioEncendio"]?.ToString() ?? string.Empty
                });
            }
            return lista;
        }

        public void ToggleActivaVM(int idVM, bool activa)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE dbo.MaquinasVirtuales SET Activa = @Activa WHERE IdVM = @IdVM", conn);
            cmd.Parameters.AddWithValue("@Activa", activa);
            cmd.Parameters.AddWithValue("@IdVM", idVM);
            cmd.ExecuteNonQuery();
        }

        public List<UsuarioAdmin> ObtenerTodosLosUsuarios()
        {
            var lista = new List<UsuarioAdmin>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "SELECT IdUsuario, Usuario, Rol, Activo FROM dbo.Usuarios ORDER BY Usuario", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new UsuarioAdmin
                {
                    IdUsuario = Convert.ToInt32(reader["IdUsuario"]),
                    Usuario = reader["Usuario"]?.ToString() ?? string.Empty,
                    Rol = reader["Rol"]?.ToString() ?? string.Empty,
                    Activo = Convert.ToBoolean(reader["Activo"])
                });
            }
            return lista;
        }

        public void EliminarUsuario(int idUsuario)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "DELETE FROM dbo.Usuarios WHERE IdUsuario = @IdUsuario", conn);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            cmd.ExecuteNonQuery();
        }

        public void ActualizarUsuario(int idUsuario, string nuevoNombre, string nuevoRol)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using (var cmdExiste = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.Usuarios WHERE Usuario = @Usuario AND IdUsuario <> @IdUsuario", conn))
            {
                cmdExiste.Parameters.AddWithValue("@Usuario", nuevoNombre);
                cmdExiste.Parameters.AddWithValue("@IdUsuario", idUsuario);
                if (Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0)
                    throw new Exception("Ya existe otro usuario con ese nombre.");
            }

            using var cmd = new SqlCommand(
                "UPDATE dbo.Usuarios SET Usuario = @Usuario, Rol = @Rol WHERE IdUsuario = @IdUsuario", conn);
            cmd.Parameters.AddWithValue("@Usuario", nuevoNombre);
            cmd.Parameters.AddWithValue("@Rol", nuevoRol);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            cmd.ExecuteNonQuery();
        }

        public void ActualizarUsuarioEncendio(int idVM, string usuario)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[BD] Iniciando actualización: IdVM={idVM}, Usuario={usuario}");

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    System.Diagnostics.Debug.WriteLine($"[BD] Conexión abierta");

                    // Verificar que la VM existe
                    string checkQuery = "SELECT COUNT(*) FROM [dbo].[MaquinasVirtuales] WHERE [IdVM] = @IdVM";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@IdVM", idVM);
                        int count = (int)checkCmd.ExecuteScalar();
                        System.Diagnostics.Debug.WriteLine($"[BD] VMs encontradas con IdVM={idVM}: {count}");

                        if (count == 0)
                        {
                            throw new Exception($"No existe VM con IdVM={idVM}");
                        }
                    }

                    // Hacer el UPDATE
                    string updateQuery = "UPDATE [dbo].[MaquinasVirtuales] SET [UsuarioEncendio] = @Usuario WHERE [IdVM] = @IdVM";
                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@IdVM", idVM);

                        // Usar el usuario tal como viene
                        if (string.IsNullOrWhiteSpace(usuario))
                        {
                            updateCmd.Parameters.AddWithValue("@Usuario", DBNull.Value);
                        }
                        else
                        {
                            updateCmd.Parameters.AddWithValue("@Usuario", usuario.Trim());
                        }

                        int rowsAffected = updateCmd.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine($"[BD] UPDATE ejecutado - Filas afectadas: {rowsAffected}");

                        if (rowsAffected > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[BD] ✓ Actualización exitosa: IdVM={idVM}, Usuario={usuario}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[BD] ⚠ UPDATE no actualizó filas");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BD] ✗ Error: {ex.Message}");
                throw;
            }
        }
    }
}