using System;
using System.Globalization;
using Microsoft.Win32;

namespace MemEase
{
    internal sealed class AppSettings
    {
        private const string RegistryPath = @"Software\MemEase";

        public static readonly int[] Thresholds = new int[] { 65, 70, 75, 80, 85, 90 };

        public bool AutoRelease;
        public int ThresholdPercent;
        public bool NotifyOnRelease;
        public bool FirstRunDone;

        public AppSettings()
        {
            AutoRelease = true;
            ThresholdPercent = 80;
            NotifyOnRelease = true;
            FirstRunDone = false;
        }

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
                {
                    if (key != null)
                    {
                        settings.AutoRelease = ReadBool(key, "AutoRelease", true);
                        settings.NotifyOnRelease = ReadBool(key, "NotifyOnRelease", true);
                        settings.FirstRunDone = ReadBool(key, "FirstRunDone", false);
                        settings.ThresholdPercent = NormalizeThreshold(ReadInt(key, "ThresholdPercent", 80));
                    }
                }
            }
            catch
            {
                // Registry is optional; fall back to defaults.
            }
            return settings;
        }

        public void Save()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    if (key == null) return;
                    key.SetValue("AutoRelease", AutoRelease ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("ThresholdPercent", ThresholdPercent, RegistryValueKind.DWord);
                    key.SetValue("NotifyOnRelease", NotifyOnRelease ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("FirstRunDone", FirstRunDone ? 1 : 0, RegistryValueKind.DWord);
                }
            }
            catch
            {
                // A locked-down registry should never take the tray application down.
            }
        }

        public static int NormalizeThreshold(int value)
        {
            for (int i = 0; i < Thresholds.Length; i++)
            {
                if (Thresholds[i] == value) return value;
            }
            return 80;
        }

        private static bool ReadBool(RegistryKey key, string name, bool fallback)
        {
            object value = key.GetValue(name);
            if (value == null) return fallback;
            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;
            }
            catch
            {
                return fallback;
            }
        }

        private static int ReadInt(RegistryKey key, string name, int fallback)
        {
            object value = key.GetValue(name);
            if (value == null) return fallback;
            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }
    }
}
