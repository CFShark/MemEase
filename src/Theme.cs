using System;
using System.Drawing;
using System.Globalization;
using Microsoft.Win32;

namespace MemEase
{
    internal static class Theme
    {
        public static bool IsDark;
        public static Color MenuBack;
        public static Color Text;
        public static Color Muted;
        public static Color Hover;
        public static Color Border;
        public static Color Separator;
        public static Color Accent;

        static Theme()
        {
            Refresh();
        }

        public static void Refresh()
        {
            bool dark = false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("AppsUseLightTheme");
                        if (value != null)
                        {
                            dark = Convert.ToInt32(value, CultureInfo.InvariantCulture) == 0;
                        }
                    }
                }
            }
            catch
            {
                dark = false;
            }

            IsDark = dark;
            if (dark)
            {
                MenuBack = Color.FromArgb(32, 32, 32);
                Text = Color.FromArgb(242, 242, 242);
                Muted = Color.FromArgb(158, 158, 158);
                Hover = Color.FromArgb(56, 56, 56);
                Border = Color.FromArgb(64, 64, 64);
                Separator = Color.FromArgb(58, 58, 58);
                Accent = Color.FromArgb(80, 200, 170);
            }
            else
            {
                MenuBack = Color.FromArgb(250, 250, 250);
                Text = Color.FromArgb(26, 26, 26);
                Muted = Color.FromArgb(112, 112, 112);
                Hover = Color.FromArgb(232, 232, 232);
                Border = Color.FromArgb(218, 218, 218);
                Separator = Color.FromArgb(225, 225, 225);
                Accent = Color.FromArgb(13, 124, 102);
            }
        }
    }
}
