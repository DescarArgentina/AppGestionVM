using System.IO;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;

namespace AppGestionDeVM.Services
{
    public class VmwareService
    {
        private const string Host = "PC-26";
        private const string Usuario = "PC-26";
        private const string Clave = "PC-26";
        private const string RutaVmrun = @"C:\VMware\VMware Workstation\vmrun.exe";
        private const string RutaVmware = @"C:\VMware\VMware Workstation\vmplayer.exe";

        public string ObtenerUsuarioLogueado()
        {
            var options = new ConnectionOptions
            {
                Username = Usuario,
                Password = Clave,
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy
            };
            var scope = new ManagementScope($"\\\\{Host}\\root\\cimv2", options);
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT UserName FROM Win32_ComputerSystem"));
            foreach (ManagementObject obj in searcher.Get())
                return obj["UserName"]?.ToString() ?? "(desconocido)";
            return "(sin sesión activa)";
        }

        public List<string> ObtenerCommandLinesVmx()
        {
            var lista = new List<string>();
            var options = new ConnectionOptions
            {
                Username = Usuario,
                Password = Clave,
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy
            };
            var scope = new ManagementScope($"\\\\{Host}\\root\\cimv2", options);
            scope.Connect();
            var query = new ObjectQuery("SELECT CommandLine FROM Win32_Process WHERE Name = 'vmware-vmx.exe'");
            using var searcher = new ManagementObjectSearcher(scope, query);
            foreach (ManagementObject proc in searcher.Get())
            {
                using (proc)
                {
                    string? cmdLine = proc["CommandLine"]?.ToString();
                    lista.Add(cmdLine ?? "(null)");
                }
            }
            return lista;
        }

        /// <summary>
        /// Devuelve las rutas completas .vmx de las VMs que tienen
        /// un proceso vmware-vmx.exe activo en PC-26.
        /// </summary>
        public HashSet<string> ObtenerRutasVmsEncendidas()
        {
            var encendidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var options = new ConnectionOptions
            {
                Username = Usuario,
                Password = Clave,
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy
            };

            var scope = new ManagementScope($"\\\\{Host}\\root\\cimv2", options);
            scope.Connect();

            var query = new ObjectQuery(
                "SELECT CommandLine FROM Win32_Process WHERE Name = 'vmware-vmx.exe'");

            using var searcher = new ManagementObjectSearcher(scope, query);

            foreach (ManagementObject proc in searcher.Get())
            {
                using (proc)
                {
                    string? cmdLine = proc["CommandLine"]?.ToString();
                    if (cmdLine == null) continue;

                    // Busca rutas Windows (.vmx) con o sin comillas
                    var match = Regex.Match(cmdLine,
                        @"""([A-Za-z]:\\[^""]+\.vmx)""|([A-Za-z]:\\[^\s""]+\.vmx)",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (match.Success)
                    {
                        string ruta = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                        encendidas.Add(ruta.Trim());
                    }
                }
            }

            return encendidas;
        }

        public void EncenderVM(string rutaVmx)
        {
            var (_, proc) = ConectarWmi();
            using (proc)
            {
                string taskName = "VmCtrl_" + Guid.NewGuid().ToString("N")[..8];

                // Register-ScheduledTask con LogonType=Interactive corre en la sesión
                // del usuario logueado sin necesitar su contraseña.
                string ps = $@"$u = (Get-WmiObject Win32_ComputerSystem).UserName
$a = New-ScheduledTaskAction -Execute '{RutaVmware}' -Argument '""{rutaVmx}""'
$p = New-ScheduledTaskPrincipal -UserId $u -LogonType Interactive -RunLevel Highest
Register-ScheduledTask -TaskName '{taskName}' -Action $a -Principal $p -Force | Out-Null
Start-ScheduledTask -TaskName '{taskName}'
Start-Sleep -Seconds 5
Unregister-ScheduledTask -TaskName '{taskName}' -Confirm:$false";

                string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(ps));
                Lanzar(proc, $"powershell.exe -NonInteractive -EncodedCommand {encoded}");
            }
        }

        public void ApagarVM(string rutaVmx)
        {
            var options = new ConnectionOptions
            {
                Username = Usuario,
                Password = Clave,
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy
            };
            var scope = new ManagementScope($"\\\\{Host}\\root\\cimv2", options);
            scope.Connect();

            var query = new ObjectQuery(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'vmware-vmx.exe'");
            using var searcher = new ManagementObjectSearcher(scope, query);

            bool terminado = false;
            foreach (ManagementObject proc in searcher.Get())
            {
                using (proc)
                {
                    string? cmdLine = proc["CommandLine"]?.ToString();
                    if (cmdLine == null) continue;

                    if (cmdLine.IndexOf(rutaVmx, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        uint pid = Convert.ToUInt32(proc["ProcessId"]);
                        using var instancia = new ManagementObject(
                            scope,
                            new ManagementPath($"Win32_Process.Handle='{pid}'"),
                            null);
                        instancia.Get();
                        instancia.InvokeMethod("Terminate", new object[] { (uint)0 });
                        terminado = true;
                    }
                }
            }

            if (!terminado)
                throw new Exception("No se encontró la VM en ejecución.");
        }

        private ManagementScope ConectarScope()
        {
            var options = new ConnectionOptions
            {
                Username = Usuario,
                Password = Clave,
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy
            };
            var scope = new ManagementScope($"\\\\{Host}\\root\\cimv2", options);
            scope.Connect();
            return scope;
        }

        private (ManagementScope, ManagementClass) ConectarWmi()
        {
            var scope = ConectarScope();
            return (scope, new ManagementClass(scope, new ManagementPath("Win32_Process"), null));
        }

        /// <summary>
        /// Lanza un script PowerShell en PC-26 que obtiene la IP guest de la VM
        /// y verifica si el puerto 3389 está abierto. Escribe el resultado en el registro.
        /// </summary>
        public void LanzarDeteccionRDP(string rutaVmx)
        {
            string valorNombre = "RDP_" + SanitizarNombreVmx(rutaVmx);
            string ps = $@"
$ErrorActionPreference = 'SilentlyContinue'
if (-not (Test-Path 'HKLM:\SOFTWARE\VmCtrl')) {{ New-Item -Path 'HKLM:\SOFTWARE\VmCtrl' -Force | Out-Null }}
$raw = & '{RutaVmrun}' getGuestIPAddress '{rutaVmx}' 2>&1
$ip  = ($raw -split '[\r\n]' | Where-Object {{ $_ -match '^\d+\.\d+\.\d+\.\d+$' }} | Select-Object -First 1)
if ($ip) {{
    try {{
        $t = New-Object System.Net.Sockets.TcpClient
        $t.Connect($ip, 3389)
        $t.Close()
        Set-ItemProperty -Path 'HKLM:\SOFTWARE\VmCtrl' -Name '{valorNombre}' -Value $ip -Force
    }} catch {{
        Set-ItemProperty -Path 'HKLM:\SOFTWARE\VmCtrl' -Name '{valorNombre}' -Value 'NOT_READY' -Force
    }}
}} else {{
    Set-ItemProperty -Path 'HKLM:\SOFTWARE\VmCtrl' -Name '{valorNombre}' -Value 'NO_IP' -Force
}}";
            var (_, proc) = ConectarWmi();
            using (proc)
            {
                string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(ps));
                Lanzar(proc, $"powershell.exe -NonInteractive -EncodedCommand {encoded}");
            }
        }

        /// <summary>
        /// Lee el resultado de la última detección RDP desde el registro remoto (PC-26).
        /// Devuelve la IP si RDP está listo, "NOT_READY", "NO_IP", o null si aún no hay dato.
        /// </summary>
        public string? LeerEstadoRDP(string rutaVmx)
        {
            string valorNombre = "RDP_" + SanitizarNombreVmx(rutaVmx);
            var scope = ConectarScope();
            using var regClass = new ManagementClass(scope, new ManagementPath("StdRegProv"), null);
            var inParams = regClass.GetMethodParameters("GetStringValue");
            inParams["hDefKey"] = 0x80000002u; // HKLM
            inParams["sSubKeyName"] = @"SOFTWARE\VmCtrl";
            inParams["sValueName"] = valorNombre;
            using var outParams = regClass.InvokeMethod("GetStringValue", inParams, null);
            if (outParams == null) return null;
            if ((uint)(outParams["ReturnValue"] ?? 1u) != 0) return null;
            return outParams["sValue"]?.ToString()?.Trim();
        }

        private static string SanitizarNombreVmx(string rutaVmx)
            => Regex.Replace(Path.GetFileNameWithoutExtension(rutaVmx), @"[^\w]", "_");

        private static void Lanzar(ManagementClass proc, string cmd)
        {
            var p = proc.GetMethodParameters("Create");
            p["CommandLine"] = cmd;
            proc.InvokeMethod("Create", p, null);
        }
    }
}
