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
                        RutaVMX
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
                            RutaVMX = reader["RutaVMX"]?.ToString() ?? string.Empty
                        });
                    }
                }
            }

            return lista;
        }
    }
}