using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var systemInfo = new Dictionary<string, string>();

        // Obtener información del procesador
        var processorInfo = new ManagementObjectSearcher("SELECT * FROM Win32_Processor").Get();
        foreach (var item in processorInfo)
        {
            systemInfo["ModeloProcesador"] = item["Name"].ToString();
            break; 
        }

        // Obtener información del sistema operativo
        var osInfo = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem").Get();
        foreach (var item in osInfo)
        {
            systemInfo["SistemaInstalado"] = item["Caption"].ToString();
            break; 
        }

        // Obtener información de memoria RAM
        var ramInfo = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem").Get();
        foreach (var item in ramInfo)
        {
            ulong ramBytes = ulong.Parse(item["TotalPhysicalMemory"].ToString());
            ulong ramGB = ramBytes / (1024 * 1024 * 1024);
            systemInfo["MemoriaRAM"] = ramGB + " GB";
            break; 
        }

        // Obtener información del disco en uso
        var diskInfo = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3").Get();
        foreach (var item in diskInfo)
        {
            ulong freeSpaceBytes = ulong.Parse(item["FreeSpace"].ToString());
            ulong freeSpaceGB = freeSpaceBytes / (1024 * 1024 * 1024);
            systemInfo["MemoriaDiscoDisponible"] = freeSpaceGB + " GB";
            break;
        }

        // Obtener información del nombre de dispositivo y usuario
        systemInfo["NombreDispositivo"] = Environment.MachineName;
        systemInfo["Usuario"] = Environment.UserName;

        // Obtener la lista de programas instalados
        var installedPrograms = GetInstalledPrograms();
        systemInfo["ProgramasInstalados"] = JsonConvert.SerializeObject(installedPrograms);

        // Convertir el diccionario en una cadena JSON
        string json = JsonConvert.SerializeObject(systemInfo, Formatting.Indented);

        // Mostrar el JSON en la consola
        Console.WriteLine(json);

        string baseUrl = "http://fer.freeddns.org:5050/post_device"; 
        HttpClient httpClient = new HttpClient();

        HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await httpClient.PostAsync(baseUrl, content);

            if (response.IsSuccessStatusCode)
            {
                // Solicitud exitosa
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Solicitud exitosa. Respuesta del servidor:");
                Console.WriteLine(responseBody);
            }
            else
            {
                // Solicitud no exitosa
                Console.WriteLine($"Error en la solicitud. Código de respuesta: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            // Manejo de errores de red u otros errores
            Console.WriteLine($"Error en la solicitud: {ex.Message}");
        }
        finally
        {
            // Liberar los recursos de HttpClient
            httpClient.Dispose();
        }

        Console.ReadLine();

    }

    // Función para obtener la lista de programas instalados
    static List<string> GetInstalledPrograms()
    {
        var programs = new List<string>();

        // Consultar el Registro para obtener la lista de programas instalados
        using (var uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
        {
            if (uninstallKey != null)
            {
                foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                {
                    using (var subKey = uninstallKey.OpenSubKey(subKeyName))
                    {
                        var displayName = subKey.GetValue("DisplayName") as string;
                        if (!string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = displayName.Replace("/", "");
                            programs.Add(displayName);
                        }
                    }
                }
            }
        }

        return programs.Distinct().OrderBy(p => p).ToList();
    }
}


