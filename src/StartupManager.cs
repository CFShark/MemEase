using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MemEase
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "MemEase";

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    string command = key.GetValue(ValueName, null) as string;
                    if (string.IsNullOrEmpty(command)) return false;
                    return string.Equals(
                        ExtractExecutablePath(command),
                        Application.ExecutablePath,
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("无法打开开机启动注册表项。");
                }

                if (enabled)
                {
                    string command = "\"" + Application.ExecutablePath + "\" --autostart";
                    key.SetValue(ValueName, command, RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(ValueName, false);
                }
            }
        }

        private static string ExtractExecutablePath(string command)
        {
            if (string.IsNullOrEmpty(command)) return string.Empty;
            command = command.Trim();
            if (command.Length == 0) return string.Empty;

            if (command[0] == '"')
            {
                int end = command.IndexOf('"', 1);
                if (end > 1) return command.Substring(1, end - 1);
            }

            int space = command.IndexOf(' ');
            return space > 0 ? command.Substring(0, space) : command;
        }
    }
}
