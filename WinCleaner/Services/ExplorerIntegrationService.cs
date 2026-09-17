using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface IExplorerIntegrationService
    {
        Task<bool> OpenInExplorerAsync(string path);
        Task<bool> OpenFileInExplorerAsync(string filePath);
        Task<bool> OpenDirectoryInExplorerAsync(string directoryPath);
        Task<bool> ShowFilePropertiesAsync(string path);
        Task<bool> SelectInExplorerAsync(string path);
        Task<bool> OpenContainingFolderAsync(string filePath);
    }

    [SupportedOSPlatform("windows")]
    public class ExplorerIntegrationService : IExplorerIntegrationService
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr[] apidl, int dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHParseDisplayName(string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

        [DllImport("shell32.dll")]
        private static extern void ILFree(IntPtr pidl);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            public string lpVerb;
            public string lpFile;
            public string lpParameters;
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        private const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;
        private const uint SEE_MASK_FLAG_NO_UI = 0x00000400;
        private const uint SEE_MASK_NOASYNC = 0x00000100;
        private const int SW_SHOW = 5;

        public async Task<bool> OpenInExplorerAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                if (File.Exists(path))
                {
                    return await OpenFileInExplorerAsync(path);
                }
                else if (Directory.Exists(path))
                {
                    return await OpenDirectoryInExplorerAsync(path);
                }
                return false;
            }
            catch { return false; }
        }

        public async Task<bool> OpenFileInExplorerAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                // Use explorer.exe with /select to highlight the file
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                };

                var proc = Process.Start(psi);
                return proc != null;
            }
            catch { return false; }
        }

        public async Task<bool> OpenDirectoryInExplorerAsync(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                return false;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{directoryPath}\"",
                    UseShellExecute = true
                };

                var proc = Process.Start(psi);
                return proc != null;
            }
            catch { return false; }
        }

        public async Task<bool> ShowFilePropertiesAsync(string path)
        {
            if (string.IsNullOrEmpty(path) || (!File.Exists(path) && !Directory.Exists(path)))
                return false;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/properties,\"{path}\"",
                    UseShellExecute = true
                };

                var proc = Process.Start(psi);
                return proc != null;
            }
            catch { return false; }
        }

        public async Task<bool> SelectInExplorerAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                if (File.Exists(path))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{path}\"",
                        UseShellExecute = true
                    };
                    var proc = Process.Start(psi);
                    return proc != null;
                }
                else if (Directory.Exists(path))
                {
                    return await OpenDirectoryInExplorerAsync(path);
                }
                return false;
            }
            catch { return false; }
        }

        public async Task<bool> OpenContainingFolderAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory))
                return false;

            return await OpenDirectoryInExplorerAsync(directory);
        }
    }
}