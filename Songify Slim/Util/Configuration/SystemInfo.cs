using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Runtime.InteropServices;

namespace Songify_Slim.Util.Configuration
{
    internal static class SystemInfo
    {
        public static string GetWindowsVersion()
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber FROM WIN32_OperatingSystem");

            foreach (ManagementBaseObject obj in searcher.Get())
            {
                return $"{obj["Caption"]} " +
                       $"({obj["Version"]}, Build {obj["BuildNumber"]})";
            }

            return RuntimeInformation.OSDescription;
        }

        public static string GetCpuInfo()
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM WIN32_Processor");

            foreach (ManagementBaseObject obj in searcher.Get())
            {
                return obj["Name"]?.ToString() ?? "Unknown CPU";
            }

            return "Unknown CPU";
        }

        public static List<string> GetGpus()
        {
            var result = new List<string>();

            using var searcher = new ManagementObjectSearcher("SELECT Name FROM win32_VideoController");

            foreach (var obj in searcher.Get())
            {
                if (obj["Name"] is string name)
                {
                    result.Add(name.Trim());
                }
            }

            return result;
        }

        public static ulong GetRamBytes()
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");

            foreach (ManagementBaseObject obj in searcher.Get())
            {
                if (obj["TotalPhysicalMemory"] is ulong bytes)
                {
                    return bytes;
                }
            }

            return 0;
        }
    }
}